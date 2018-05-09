namespace ServiceControl.Monitoring.QueueLength
{
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using Infrastructure;
    using NServiceBus;
    using NServiceBus.Logging;
    using NServiceBus.Metrics;

    class LegacyQueueLengthReportHandler : IHandleMessages<MetricReport>
    {
        LegacyQueueLengthEndpoints legacyEndpoints;

        public LegacyQueueLengthReportHandler(LegacyQueueLengthEndpoints legacyEndpoints)
        {
            this.legacyEndpoints = legacyEndpoints;
        }

        public Task Handle(MetricReport message, IMessageHandlerContext context)
        {
            var endpointInstanceId = EndpointInstanceId.From(context.MessageHeaders);

            if (legacyEndpoints.TryAdd(endpointInstanceId.InstanceId))
            {
                Logger.Warn($"Legacy queue length report received from {endpointInstanceId.InstanceName} instance of {endpointInstanceId.EndpointName}");
            }

            return TaskEx.Completed;
        }

        public class LegacyQueueLengthEndpoints
        {
            ConcurrentDictionary<string, string> regsteredInstances = new ConcurrentDictionary<string, string>();

            public bool TryAdd(string id)
            {
                return regsteredInstances.TryAdd(id, id);
            }

        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(LegacyQueueLengthReportHandler));
    }
}