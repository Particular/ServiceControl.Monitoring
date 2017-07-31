namespace ServiceControl.Monitoring
{
    using System.Threading.Tasks;
    using Infrastructure;
    using Messaging;
    using NServiceBus;

    public class OccurrenceHandler : IHandleMessages<Occurrences>
    {
        readonly RetriesStore store;
        const string RetriesMessageType = "Retries";

        public OccurrenceHandler(RetriesStore store)
        {
            this.store = store;
        }

        public Task Handle(Occurrences message, IMessageHandlerContext context)
        {
            try
            {
                var endpointInstanceId = EndpointInstanceId.From(context.MessageHeaders);
                var messageType = context.MessageHeaders[MetricHeaders.MetricType];

                switch (messageType)
                {
                    case RetriesMessageType:
                        store.Store(endpointInstanceId, message.Entries);
                        break;
                    default:
                        throw new UnknownOccurrenceMessageType(messageType);
                }
            }
            finally
            {
                RawMessage.Pool<Occurrences>.Default.Release(message);
            }

            return TaskEx.Completed;
        }
    }
}