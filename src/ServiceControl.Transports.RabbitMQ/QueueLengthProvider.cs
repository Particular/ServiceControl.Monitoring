﻿namespace ServiceControl.Transports.RabbitMQ
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;
    using System.Threading.Tasks;
    using global::RabbitMQ.Client;
    using Monitoring;
    using Monitoring.Infrastructure;
    using Monitoring.Messaging;
    using Monitoring.QueueLength;
    using NServiceBus.Logging;
    using NServiceBus.Metrics;

    public class QueueLengthProvider : IProvideQueueLength
    {
        public void Initialize(string connectionString, QueueLengthStore store)
        {
            this.connectionString = connectionString;
            this.store = store;
        }

        public void Process(EndpointInstanceId endpointInstanceId, EndpointMetadataReport metadataReport)
        {
            var endpointInstanceQueue = new EndpointInputQueue(endpointInstanceId.EndpointName, metadataReport.LocalAddress);
            var queueName = metadataReport.LocalAddress;

            endpointQueues.AddOrUpdate(endpointInstanceQueue, _ => queueName, (_, currentValue) =>
            {
                if (currentValue != queueName) sizes.TryRemove(currentValue, out var _);

                return queueName;
            });
        }

        public void Process(EndpointInstanceId endpointInstanceId, TaggedLongValueOccurrence metricsReport)
        {
            //RabbitMQ does not support endpoint level queue length reports
        }

        public Task Start()
        {
            var connectionConfiguration = ConnectionConfiguration.Create(connectionString, "ServiceControl.Monitoring");
            var factory = new ConnectionFactory(connectionConfiguration, null, false, false);

            IConnection connection = null;
            IModel model = null;

            queryTask = Task.Run(async () =>
            {
                while (!stoppedTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        if (connection == null)
                        {
                            connection = factory.CreateConnection(string.Empty);
                        }

                        if (model == null)
                        {
                            model = connection.CreateModel();
                        }

                        FetchQueueLengths(model);

                        UpdateQueueLengths();

                        await Task.Delay(QueryDelayInterval).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Logger.Warn("Error querying queue sizes.", e);
                    }
                }
            });

            return TaskEx.Completed;
        }

        public Task Stop()
        {
            stoppedTokenSource.Cancel();

            return queryTask;
        }

        void UpdateQueueLengths()
        {
            var nowTicks = DateTime.UtcNow.Ticks;

            foreach (var endpointQueuePair in endpointQueues)
                store.Store(
                    new[]
                    {
                        new RawMessage.Entry
                        {
                            DateTicks = nowTicks,
                            Value = sizes.TryGetValue(endpointQueuePair.Value, out var size) ? size : 0
                        }
                    },
                    endpointQueuePair.Key);
        }

        void FetchQueueLengths(IModel model)
        {
            //TODO: check what happens when non existing queue is querried
            foreach (var endpointQueuePair in endpointQueues)
            {
                try
                {
                    var size = (int) model.MessageCount(endpointQueuePair.Value);

                    sizes.AddOrUpdate(endpointQueuePair.Value, _ => size, (_, __) => size);
                }
                catch (Exception e)
                {
                    Logger.Warn($"Error fetching size for queue {endpointQueuePair.Value}", e);
                }
            }
        }

        ConcurrentDictionary<EndpointInputQueue, string> endpointQueues = new ConcurrentDictionary<EndpointInputQueue, string>();
        ConcurrentDictionary<string, int> sizes = new ConcurrentDictionary<string, int>();

        CancellationTokenSource stoppedTokenSource = new CancellationTokenSource();
        Task queryTask;

        string connectionString;
        QueueLengthStore store;

        static TimeSpan QueryDelayInterval = TimeSpan.FromMilliseconds(200);

        static ILog Logger = LogManager.GetLogger<QueueLengthProvider>();
    }
}