namespace ServiceControl.Monitoring.Timings
{
    using System;
    using System.Threading.Tasks;
    using Metrics.Raw;
    using Metrics.Snapshot;
    using NServiceBus;

    class TimingsReportHandler : IHandleMessages<LongValueOccurrences>
    {
        readonly TimingsStore processingTimeStore;
        readonly CriticalTimeStore criticalTimeStore;

        const string ProcessingTimeMessageType = "NServiceBus.Metrics.ProcessingTime";
        const string CriticalTimeMessageType = "NServiceBus.Metrics.CriticalTime";

        public TimingsReportHandler(ProcessingTimeStore processingTimeStore, CriticalTimeStore criticalTimeStore)
        {
            this.processingTimeStore = processingTimeStore;
            this.criticalTimeStore = criticalTimeStore;
        }

        public Task Handle(LongValueOccurrences message, IMessageHandlerContext context)
        {
            var messageType = context.MessageHeaders[Headers.EnclosedMessageTypes];
            var endpointName = context.MessageHeaders.GetOriginatingEndpoint();

            switch (messageType)
            {
                case ProcessingTimeMessageType:
                    processingTimeStore.Store(endpointName, message, DateTime.UtcNow);
                    break;
                case CriticalTimeMessageType:
                    criticalTimeStore.Store(endpointName, message, DateTime.UtcNow);
                    break;
                default:
                    throw new UnknownLongValueOccurrenceMessageType(messageType);

            }

            return TaskEx.Completed;
        }
    }
}