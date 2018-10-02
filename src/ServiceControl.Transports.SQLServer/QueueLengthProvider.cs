namespace ServiceControl.Transports.SQLServer
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Monitoring;
    using Monitoring.Infrastructure;
    using Monitoring.Messaging;
    using Monitoring.QueueLength;
    using NServiceBus.Logging;
    using NServiceBus.Metrics;

    public class QueueLengthProvider : IProvideQueueLength
    {
        ConcurrentDictionary<EndpointInputQueue, SqlTable> tableNames = new ConcurrentDictionary<EndpointInputQueue, SqlTable>();
        ConcurrentDictionary<SqlTable, int> tableSizes = new ConcurrentDictionary<SqlTable, int>();

        string connectionString;
        string defaultSchema;
        QueueLengthStore store;

        CancellationTokenSource stop = new CancellationTokenSource();
        Task poller;

        public void Initialize(string connectionString, QueueLengthStore store)
        {
            this.connectionString = connectionString.RemoveCustomSchemaPart(out var customSchema);

            defaultSchema = customSchema ?? "dbo";

            this.store = store;
        }

        public void Process(EndpointInstanceId endpointInstanceId, EndpointMetadataReport metadataReport)
        {
            var endpointInputQueue = new EndpointInputQueue(endpointInstanceId.EndpointName, metadataReport.LocalAddress);
            var localAddress = metadataReport.LocalAddress;

            var sqlTable = SqlTable.Parse(localAddress, defaultSchema);
            
            tableNames.AddOrUpdate(endpointInputQueue, _ => sqlTable, (_, currentSqlTable) =>
            {
                if (currentSqlTable.Equals(sqlTable) == false)
                {
                    tableSizes.TryRemove(currentSqlTable, out var _);
                }

                return sqlTable;
            });

            tableSizes.TryAdd(sqlTable, 0);
        }

        public void Process(EndpointInstanceId endpointInstanceId, TaggedLongValueOccurrence metricsReport)
        {
            //HINT: Sql server endpoints do not support endpoint level queue length monitoring
        }
       
        public Task Start()
        {
            stop = new CancellationTokenSource();

            poller = Task.Run(async () =>
            {
                while (!stop.Token.IsCancellationRequested)
                {
                    try
                    {
                        await QueryTableSizes();

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

            return poller;
        }

        void UpdateQueueLengthStore()
        {
            var nowTicks = DateTime.UtcNow.Ticks;

            foreach (var tableNamePair in tableNames)
            {
                store.Store(
                    new []{ new RawMessage.Entry
                    {
                        DateTicks = nowTicks,
                        Value = tableSizes.TryGetValue(tableNamePair.Value, out var size) ? size : 0
                    }}, 
                    tableNamePair.Key);
            }
        }

        async Task QueryTableSizes()
        {
            var chunks = tableSizes
                .Select((i, index) => new
                {
                    i,
                    index
                })
                .GroupBy(p => p.index / QueryChunkSize)
                .Select(grp => grp.Select(g => g.i).ToArray())
                .ToList();

            foreach (var chunk in chunks)
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    await UpdateChunk(connection, chunk, stop.Token);
                }
            }
        }

        async Task UpdateChunk(SqlConnection connection, KeyValuePair<SqlTable, int>[] chunk, CancellationToken token)
        {
            var query = string.Join(Environment.NewLine, chunk.Select(c => BuildQueueLengthQuery(c.Key)).ToArray());

            using (var command = new SqlCommand(query, connection))
            {
                var reader = await command.ExecuteReaderAsync(token);

                foreach (var chunkPair in chunk)
                {
                    await reader.ReadAsync(token);

                    var queueLength = reader.GetInt32(0);

                    if (queueLength == -1)
                    {
                        Logger.Warn($"Table {chunkPair.Key} does not exist.");
                    }
                    else
                    {
                        tableSizes.TryUpdate(chunkPair.Key, queueLength, chunkPair.Value);
                    }

                    await reader.NextResultAsync(token);
                }
            }
        }

        static string BuildQueueLengthQuery(SqlTable t)
        {
            if (t.QuotedCatalog == null)
            {
                return $@"IF (EXISTS (SELECT *  FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{t.UnquotedSchema}' AND  TABLE_NAME = '{t.UnquotedName}'))
                            SELECT count(*) FROM {t.QuotedSchema}.{t.QuotedName} WITH (nolock)
                          ELSE
                            SELECT -1;";
            }

            return $@"IF (EXISTS (SELECT *  FROM {t.QuotedCatalog}.INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{t.UnquotedSchema}' AND  TABLE_NAME = '{t.UnquotedName}'))
                        SELECT count(*) FROM {t.QuotedCatalog}.{t.QuotedSchema}.{t.QuotedName} WITH (nolock)
                      ELSE
                        SELECT -1;";
        }

        static TimeSpan QueryDelayInterval = TimeSpan.FromMilliseconds(200);

        const int QueryChunkSize = 10;

        static ILog Logger = LogManager.GetLogger<QueueLengthProvider>();
    }
}