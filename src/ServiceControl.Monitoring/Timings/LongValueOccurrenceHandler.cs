namespace ServiceControl.Monitoring.Timings
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using Processing.RawData.NServiceBus.Metrics;
    using Processing.Snapshot;

    class TimingsReportHandler : IHandleMessages<LongValueOccurrences>
    {
        readonly TimingsStore processingTimeStore;
        readonly CriticalTimeStore criticalTimeStore;

        static string ProcessingTimeMessageType = "NServiceBus.Metrics.ProcessingTime";
        static string CriticalTimeMessageType = "NServiceBus.Metrics.CriticalTime";

        public TimingsReportHandler(ProcessingTimeStore processingTimeStore, CriticalTimeStore criticalTimeStore)
        {
            this.processingTimeStore = processingTimeStore;
            this.criticalTimeStore = criticalTimeStore;
        }

        public Task Handle(LongValueOccurrences message, IMessageHandlerContext context)
        {
            var messageType = context.MessageHeaders[Headers.EnclosedMessageTypes];
            var endpointName = context.MessageHeaders.GetOriginatingEndpoint();

            if (messageType == ProcessingTimeMessageType)
            {
                processingTimeStore.Store(endpointName, message, DateTime.UtcNow);
            }
            else if (messageType == CriticalTimeMessageType)
            {
                criticalTimeStore.Store(endpointName, message, DateTime.UtcNow);
            }

            return TaskEx.Completed;
        }
    }
}