namespace ServiceControl.Monitoring.Infrastructure
{
    using System;
    using System.Threading.Tasks;
    using Messaging;
    using NServiceBus;
    using NServiceBus.Metrics;

    public class EndpointTracker : IHandleMessages<MetricReport>, IHandleMessages<LongValueOccurrences>, IHandleMessages<Occurrences>
    {
        public EndpointTracker(EndpointRegistry endpointRegistry, EndpointInstanceActivityTracker activityTracker)
        {
            this.endpointRegistry = endpointRegistry;
            this.activityTracker = activityTracker;
        }

        public Task Handle(LongValueOccurrences message, IMessageHandlerContext context)
        {
            return RecordEndpointInstanceId(context);
        }

        public Task Handle(MetricReport message, IMessageHandlerContext context)
        {
            return RecordEndpointInstanceId(context);
        }

        public Task Handle(Occurrences message, IMessageHandlerContext context)
        {
            return RecordEndpointInstanceId(context);
        }

        Task RecordEndpointInstanceId(IMessageHandlerContext context)
        {
            var instanceId = EndpointInstanceId.From(context.MessageHeaders);

            endpointRegistry.Record(instanceId);
            activityTracker.Record(instanceId, DateTime.UtcNow);

            return TaskEx.Completed;
        }

        EndpointRegistry endpointRegistry;
        readonly EndpointInstanceActivityTracker activityTracker;
    }
}