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
        ConcurrentDictionary<string, string> loggedInstances = new ConcurrentDictionary<string, string>();

        public Task Handle(MetricReport message, IMessageHandlerContext context)
        {
            var endpointInstanceId = EndpointInstanceId.From(context.MessageHeaders);

            if (loggedInstances.TryAdd(endpointInstanceId.InstanceId, endpointInstanceId.InstanceId))
            {
                Logger.Warn($"Legacy queue length report received from {endpointInstanceId.InstanceName} instance of {endpointInstanceId.EndpointName}");
            }

            return TaskEx.Completed;
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(LegacyQueueLengthReportHandler));
    }
}