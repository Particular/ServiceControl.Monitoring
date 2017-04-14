namespace ServiceControl.Monitoring.ServiceControl.Monitoring
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Metrics;

    public class MetricsHandler : IHandleMessages<MetricReport>
    {
        static readonly Task<int> CompletedTask = Task.FromResult(0);
        readonly PublisherConsumer<MetricReportWithHeaders> publisherConsumer;

        public MetricsHandler(PublisherConsumer<MetricReportWithHeaders> publisherConsumer)
        {
            this.publisherConsumer = publisherConsumer;
        }

        public Task Handle(MetricReport message, IMessageHandlerContext context)
        {
            publisherConsumer.Publish(new MetricReportWithHeaders(message.Data, context.MessageHeaders));
            return CompletedTask;
        }
    }
}