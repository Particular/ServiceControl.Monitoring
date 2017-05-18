namespace ServiceControl.Monitoring.Raw
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Metrics;

    public class MetricsHandler : IHandleMessages<MetricReport>
    {
        readonly IEnumerable<IRawDataConsumer> providers;
        static readonly Task<int> CompletedTask = Task.FromResult(0);

        public MetricsHandler(IEnumerable<IRawDataConsumer> providers)
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