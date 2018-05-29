namespace ServiceControl.Transports.AzureStorageQueues
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Monitoring;
    using Monitoring.Infrastructure;
    using Monitoring.Messaging;
    using Monitoring.QueueLength;
    using NServiceBus.Logging;
    using NServiceBus.Metrics;

    public class QueueLengthProvider : IProvideQueueLength
    {
        ConcurrentDictionary<EndpointInputQueue, CloudQueue> queues = new ConcurrentDictionary<EndpointInputQueue, CloudQueue>();
        ConcurrentDictionary<CloudQueue, int> sizes = new ConcurrentDictionary<CloudQueue, int>();
        ConcurrentDictionary<CloudQueue, CloudQueue> problematicQueues = new ConcurrentDictionary<CloudQueue, CloudQueue>();

        string connectionString;
        QueueLengthStore store;

        CancellationTokenSource stop = new CancellationTokenSource();
        Task pooler;

        public void Initialize(string connectionString, QueueLengthStore store)
        {
            this.connectionString = connectionString;
            this.store = store;
        }

        public void Process(EndpointInstanceId endpointInstanceId, EndpointMetadataReport metadataReport)
        {
            var endpointInputQueue = new EndpointInputQueue(endpointInstanceId.EndpointName, metadataReport.LocalAddress);
            var queueName = metadataReport.LocalAddress.ToLower().Replace(".", "-");

            var queueClient = CloudStorageAccount.Parse(connectionString).CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(queueName);

            queues.AddOrUpdate(endpointInputQueue, _ => queue, (_, currentQueue) =>
            {
                if (currentQueue.Name.Equals(queue.Name) == false)
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
                while (!stop.Token.IsCancellationRequested)
                {
                    try
                    {
                        await FetchQueueSizes().ConfigureAwait(false);

                        UpdateQueueLengthStore();

                        await Task.Delay(QueryDelayInterval);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Error querying sql queue sizes.", e);
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

        Task FetchQueueSizes() => Task.WhenAll(sizes.Select(kvp => FetchLength(kvp.Key)));

        async Task FetchLength(CloudQueue queue)
        {
            try
            {
                await queue.FetchAttributesAsync(stop.Token).ConfigureAwait(false);
                sizes[queue] = queue.ApproximateMessageCount.GetValueOrDefault();

                problematicQueues.TryRemove(queue, out _);
            }
            catch (Exception ex)
            {
                // simple "log once" approach to do not flood logs
                if (problematicQueues.TryAdd(queue, queue))
                {
                    Logger.Error($"Obtaining Azure Storage Queue count failed for '{queue.Name}'", ex);
                }
            }
        }

        static TimeSpan QueryDelayInterval = TimeSpan.FromMilliseconds(200);
        static ILog Logger = LogManager.GetLogger<QueueLengthProvider>();
    }
}