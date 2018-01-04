namespace ServiceControl.Monitoring.QueueLength
{
    using System.Threading.Tasks;
    using Infrastructure;
    using NServiceBus;
    using NServiceBus.Metrics;

    class QueueLengthReportHandler : IHandleMessages<MetricReport>
    {
        QueueLengthStore queueLengthStore;

        public QueueLengthReportHandler(QueueLengthStore queueLengthStore)
        {
            this.queueLengthStore = queueLengthStore;
        }

        public Task Handle(MetricReport message, IMessageHandlerContext context)
        {
            var endpointInstanceId = EndpointInstanceId.From(context.MessageHeaders);

            queueLengthStore.Store(endpointInstanceId, message.Data);

            return TaskEx.Completed;
        }
    }
}