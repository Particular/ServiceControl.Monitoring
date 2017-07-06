namespace ServiceControl.Monitoring.Processing.Snapshot
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Metrics;
    using NServiceBus;

    public class MetricsHandler : IHandleMessages<MetricReport>
    {
        readonly IEnumerable<ISnapshotDataConsumer> providers;
        static readonly Task<int> CompletedTask = Task.FromResult(0);

        public MetricsHandler(IEnumerable<ISnapshotDataConsumer> providers)
        {
            this.providers = providers;
        }

        public Task Handle(MetricReport message, IMessageHandlerContext context)
        {
            foreach (var provider in providers)
            {
                provider.Consume(context.MessageHeaders, message.Data);
            }
            
            return CompletedTask;
        }
    }
}