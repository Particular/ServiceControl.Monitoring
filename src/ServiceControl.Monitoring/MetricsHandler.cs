namespace ServiceControl.Monitoring.ServiceControl.Monitoring
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Metrics;

    public class MetricsHandler : IHandleMessages<MetricReport>
    {
        static readonly Task<int> CompletedTask = Task.FromResult(0);
        readonly PublisherConsumer<MetricReport> publisherConsumer;

        public MetricsHandler(PublisherConsumer<MetricReport> publisherConsumer)
        {
            this.publisherConsumer = publisherConsumer;
        }

        public Task Handle(MetricReport message, IMessageHandlerContext context)
        {
            publisherConsumer.Publish(message);
            return CompletedTask;
        }
    }
}