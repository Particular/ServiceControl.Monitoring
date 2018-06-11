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
            queryExecutor = new QueryExecutor(connectionString);

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
            queryExecutor.Initialize();

            queryLoop = Task.Run(async () =>
            {
                while (!stoppedTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        await FetchQueueLengths();

                        UpdateQueueLengths();

                        await Task.Delay(QueryDelayInterval).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Queue length query loop failure.", e);
                    }
                }
            });

            return TaskEx.Completed;
        }

        public async Task Stop()
        {
            stoppedTokenSource.Cancel();

            await queryLoop;

            queryExecutor.Dispose();
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

        async Task FetchQueueLengths()
        {
            foreach (var endpointQueuePair in endpointQueues)
            {
                await queryExecutor.Execute(m =>
                {
                    var queueName = endpointQueuePair.Value;

                    try
                    {
                        var size = (int) m.MessageCount(queueName);

                        sizes.AddOrUpdate(queueName, _ => size, (_, __) => size);
                    }
                    catch (Exception e)
                    {
                        Logger.Warn($"Error querying queue length for {queueName}", e);
                    }
                });
            }
        }


        class QueryExecutor : IDisposable
        {
            string connectionString;
            IConnection connection;
            IModel model;
            ConnectionFactory connectionFactory;

            public QueryExecutor(string connectionString)
            {
                this.connectionString = connectionString;
            }

            public void Initialize()
            {
                var connectionConfiguration = ConnectionConfiguration.Create(connectionString, "ServiceControl.Monitoring");
                connectionFactory = new ConnectionFactory(connectionConfiguration, null, false, false);
            }

            public async Task Execute(Action<IModel> action)
            {
                try
                {
                    if (connection == null)
                    {
                        connection = connectionFactory.CreateConnection("queue length monitor");
                    }

                    //Connection implements reconnection logic
                    while (connection.IsOpen == false)
                    {
                        await Task.Delay(ReconnectionDelay);
                    }

                    if (model == null || model.IsClosed)
                    {
                        model?.Dispose();

                        model = connection.CreateModel();
                    }

                    action(model);
                }
                catch (Exception e)
                {
                    Logger.Warn("Error querying queue length.", e);
                }
            }

            TimeSpan ReconnectionDelay = TimeSpan.FromSeconds(5);

            public void Dispose()
            {
                connection?.Dispose();
            }
        }

        ConcurrentDictionary<EndpointInputQueue, string> endpointQueues = new ConcurrentDictionary<EndpointInputQueue, string>();
        ConcurrentDictionary<string, int> sizes = new ConcurrentDictionary<string, int>();

        CancellationTokenSource stoppedTokenSource = new CancellationTokenSource();
        Task queryLoop;

        QueueLengthStore store;
        QueryExecutor queryExecutor;

        static TimeSpan QueryDelayInterval = TimeSpan.FromMilliseconds(200);

        static ILog Logger = LogManager.GetLogger<QueueLengthProvider>();
    }
}