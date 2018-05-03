namespace ServiceControl.Monitoring.QueueLength
{
    using System.Threading.Tasks;
    using Infrastructure;
    using NServiceBus;
    using NServiceBus.Logging;
    using NServiceBus.Metrics;

    class LegacyQueueLengthReportHandler : IHandleMessages<MetricReport>
    {
        public Task Handle(MetricReport message, IMessageHandlerContext context)
        {
            var endpointInstanceId = EndpointInstanceId.From(context.MessageHeaders);

            Logger.Warn($"Legacy queue length report received from {endpointInstanceId.InstanceName} instance of {endpointInstanceId.EndpointName}");

            return TaskEx.Completed;
        }

        static readonly ILog Logger = LogManager.GetLogger(typeof(LegacyQueueLengthReportHandler));
    }
}