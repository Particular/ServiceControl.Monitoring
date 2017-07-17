﻿namespace ServiceControl.Monitoring.QueueLength
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Metrics;

    class QueueLengthReportHandler : IHandleMessages<MetricReport>
    {
        QueueLengthDataStore queueLengthDataStore;

        public QueueLengthReportHandler(QueueLengthDataStore queueLengthDataStore)
        {
            this.queueLengthDataStore = queueLengthDataStore;
        }

        public Task Handle(MetricReport message, IMessageHandlerContext context)
        {
            queueLengthDataStore.Store(message.Data);

            return TaskEx.Completed;
        }
    }
}