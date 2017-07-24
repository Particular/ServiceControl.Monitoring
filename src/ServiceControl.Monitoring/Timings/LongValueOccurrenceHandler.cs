namespace ServiceControl.Monitoring.Timings
{
    using System;
    using System.Threading.Tasks;
    using Metrics.Raw;
    using NServiceBus;

    class TimingsReportHandler : IHandleMessages<LongValueOccurrences>
    {
        readonly TimingsStore processingTimeStore;
        readonly CriticalTimeStore criticalTimeStore;

        const string ProcessingTimeMessageType = "ProcessingTime";
        const string CriticalTimeMessageType = "CriticalTime";

        public TimingsReportHandler(ProcessingTimeStore processingTimeStore, CriticalTimeStore criticalTimeStore)
        {
            this.processingTimeStore = processingTimeStore;
            this.criticalTimeStore = criticalTimeStore;
        }

        public Task Handle(LongValueOccurrences message, IMessageHandlerContext context)
        {
            try
            {
                var endpointInstanceId = EndpointInstanceId.From(context.MessageHeaders);

                var messageType = context.MessageHeaders[MetricHeaders.MetricType];

                switch (messageType)
                {
                    case ProcessingTimeMessageType:
                        processingTimeStore.Store(endpointInstanceId, message, DateTime.UtcNow);
                        break;
                    case CriticalTimeMessageType:
                        criticalTimeStore.Store(endpointInstanceId, message, DateTime.UtcNow);
                        break;
                    default:
                        throw new UnknownLongValueOccurrenceMessageType(messageType);

                }
            }
            finally
            {
                LongValueOccurrences.Pool.Default.Release(message);
            }

            return TaskEx.Completed;
        }
    }
}