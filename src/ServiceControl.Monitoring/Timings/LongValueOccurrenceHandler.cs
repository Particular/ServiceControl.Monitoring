namespace ServiceControl.Monitoring.Timings
{
    using System.Threading.Tasks;
    using Infrastructure;
    using Messaging;
    using NServiceBus;

    class TimingsReportHandler : IHandleMessages<LongValueOccurrences>
    {
        readonly ProcessingTimeStore processingTimeStore;
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
                        processingTimeStore.Store(endpointInstanceId, message.Entries);
                        break;
                    case CriticalTimeMessageType:
                        criticalTimeStore.Store(endpointInstanceId, message.Entries);
                        break;
                    default:
                        throw new UnknownLongValueOccurrenceMessageType(messageType);

                }
            }
            finally
            {
                RawMessage.Pool<LongValueOccurrences>.Default.Release(message);
            }

            return TaskEx.Completed;
        }
    }
}