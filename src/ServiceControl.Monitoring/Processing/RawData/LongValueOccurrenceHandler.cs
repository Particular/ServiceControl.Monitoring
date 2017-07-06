namespace ServiceControl.Monitoring.Processing.RawData
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::NServiceBus;
    using NServiceBus.Metrics;
    using Snapshot;

    public class LongValueOccurrenceTimeReportHandler : IHandleMessages<LongValueOccurrences>
    {
        readonly DurationsDataStore store;
        static readonly Task<int> CompletedTask = Task.FromResult(0);
        static string ProcessingTimeMessageType = "NServiceBus.Metrics.ProcessingTime";
        static string CriticalTimeMessageType = "NServiceBus.Metrics.CriticalTime";

        public LongValueOccurrenceTimeReportHandler(DurationsDataStore store)
        {
            this.store = store;
        }

        public Task Handle(LongValueOccurrences message, IMessageHandlerContext context)
        {
            var messageType = context.MessageHeaders[Headers.EnclosedMessageTypes];
            var endpointName = context.MessageHeaders.GetOriginatingEndpoint();

            if (messageType == ProcessingTimeMessageType)
            {
                store.RecordProcessingTime(endpointName, message);
            }
            else if (messageType == CriticalTimeMessageType)
            {
                //store.RecordCriticalTime(endpointName, message);
            }

            return CompletedTask;
        }
    }
}