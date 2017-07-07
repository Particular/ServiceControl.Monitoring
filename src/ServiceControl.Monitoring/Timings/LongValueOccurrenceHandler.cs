namespace ServiceControl.Monitoring.Timings
{
    using System.Threading.Tasks;
    using NServiceBus;
    using Processing.RawData.NServiceBus.Metrics;
    using Processing.Snapshot;

    class TimingsReportHandler : IHandleMessages<LongValueOccurrences>
    {
        readonly TimingsDataStore store;
        
        static string ProcessingTimeMessageType = "NServiceBus.Metrics.ProcessingTime";
        static string CriticalTimeMessageType = "NServiceBus.Metrics.CriticalTime";

        public TimingsReportHandler(TimingsDataStore store)
        {
            this.store = store;
        }

        public Task Handle(LongValueOccurrences message, IMessageHandlerContext context)
        {
            var messageType = context.MessageHeaders[Headers.EnclosedMessageTypes];
            var endpointName = context.MessageHeaders.GetOriginatingEndpoint();

            if (messageType == ProcessingTimeMessageType)
            {
                store.StoreProcessingTime(endpointName, message);
            }
            else if (messageType == CriticalTimeMessageType)
            {
                store.StoreCriticalTime(endpointName, message);
            }

            return TaskEx.Completed;
        }
    }
}