﻿namespace ServiceControl.Transports.AmazonSQS
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Amazon.SQS;
    using Monitoring;
    using Monitoring.Infrastructure;
    using Monitoring.Messaging;
    using Monitoring.QueueLength;
    using NServiceBus.Logging;
    using NServiceBus.Metrics;

    public class QueueLengthProvider : IProvideQueueLength
    {
        ConcurrentDictionary<EndpointInputQueue, string> queues = new ConcurrentDictionary<EndpointInputQueue, string>();
        ConcurrentDictionary<string, int> sizes = new ConcurrentDictionary<string, int>();

        QueueLengthStore store;

        CancellationTokenSource stop = new CancellationTokenSource();
        Task pooler;

        public void Initialize(string connectionString, QueueLengthStore store)
        {
            this.store = store;
        }

        public void Process(EndpointInstanceId endpointInstanceId, EndpointMetadataReport metadataReport)
        {
            var endpointInputQueue = new EndpointInputQueue(endpointInstanceId.EndpointName, metadataReport.LocalAddress);
            var queue = QueueNameHelper.GetSqsQueueName(metadataReport.LocalAddress);

            queues.AddOrUpdate(endpointInputQueue, _ => queue, (_, currentQueue) =>
            {
                if (currentQueue != queue)
                {
                    sizes.TryRemove(currentQueue, out var _);
                }

                return queue;
            });

            sizes.TryAdd(queue, 0);
        }

        public void Process(EndpointInstanceId endpointInstanceId, TaggedLongValueOccurrence metricsReport)
        {
            //HINT: ASQ  server endpoints do not support endpoint level queue length monitoring
        }

        public Task Start()
        {
            stop = new CancellationTokenSource();

            pooler = Task.Run(async () =>
            {
                using (var client = new AmazonSQSClient())
                {
                    var cache = new QueueAttributesRequestCache(client);

                    while (!stop.Token.IsCancellationRequested)
                    {
                        try
                        {
                            await FetchQueueSizes(cache, client).ConfigureAwait(false);

                            UpdateQueueLengthStore();

                            await Task.Delay(QueryDelayInterval).ConfigureAwait(false);
                        }
                        catch (Exception e)
                        {
                            Logger.Error("Error querying SQS queue sizes.", e);
                        }
                    }
                }
            });

            return TaskEx.Completed;
        }

        public Task Stop()
        {
            stop.Cancel();

            return pooler;
        }

        void UpdateQueueLengthStore()
        {
            var nowTicks = DateTime.UtcNow.Ticks;

            foreach (var tableNamePair in queues)
            {
                store.Store(
                    new[]{ new RawMessage.Entry
                    {
                        DateTicks = nowTicks,
                        Value = sizes.TryGetValue(tableNamePair.Value, out var size) ? size : 0
                    }},
                    tableNamePair.Key);
            }
        }

        Task FetchQueueSizes(QueueAttributesRequestCache cache, IAmazonSQS client) => Task.WhenAll(sizes.Select(kvp => FetchLength(kvp.Key, client, cache)));

        async Task FetchLength(string queue, IAmazonSQS client, QueueAttributesRequestCache cache)
        {
            try
            {
                var attReq = await cache.GetQueueAttributesRequest(queue).ConfigureAwait(false);
                var response = await client.GetQueueAttributesAsync(attReq).ConfigureAwait(false);
                sizes[queue] = response.ApproximateNumberOfMessages;
            }
            catch (Exception ex)
            {
                Logger.Error($"Obtaining an approximate number of messages failed for '{queue}'", ex);
            }
        }

        static TimeSpan QueryDelayInterval = TimeSpan.FromMilliseconds(200);
        static ILog Logger = LogManager.GetLogger<QueueLengthProvider>();
    }
}