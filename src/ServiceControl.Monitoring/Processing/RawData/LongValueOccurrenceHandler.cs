namespace ServiceControl.Monitoring.Processing.RawData
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using global::NServiceBus;
    using NServiceBus.Metrics;

    public class LongValueOccurrenceTimeReportHandler : IHandleMessages<LongValueOccurrences>
    {
        readonly IEnumerable<IConsumeLongValueOccurrences> consumers;
        static readonly Task<int> CompletedTask = Task.FromResult(0);

        public LongValueOccurrenceTimeReportHandler(IEnumerable<IConsumeLongValueOccurrences> consumers)
        {
            this.consumers = consumers;
        }

        public Task Handle(LongValueOccurrences message, IMessageHandlerContext context)
        {
            foreach (var consumer in consumers)
            {
                consumer.Consume(context.MessageHeaders, message);
            }

            return CompletedTask;
        }
    }
}