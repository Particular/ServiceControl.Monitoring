namespace ServiceControl.Monitoring.QueueLength
{
    using System.Threading.Tasks;
    using Infrastructure;
    using Messaging;
    using NServiceBus.Metrics;

    public class DefaultQueueLengthProvider : IProvideQueueLength
    {
        protected string ConnectionString { get; private set; }
        protected QueueLengthStore QueueLengthStore { get; private set; }

        public virtual void Initialize(string connectionString, QueueLengthStore store)
        {
            ConnectionString = connectionString;
            QueueLengthStore = store;
        }

        public virtual void Process(EndpointInstanceId endpointInstanceId, EndpointMetadataReport metadataReport)
        {
            // HINT: Not every queue length provider requires metadata reports
        }

        public void Process(EndpointInstanceId endpointInstanceId, TaggedLongValueOccurrence metricsReport)
        {
            var endpointInputQueue = new EndpointInputQueue(endpointInstanceId.EndpointName, metricsReport.TagValue);
            QueueLengthStore.Store(metricsReport.Entries, endpointInputQueue);
        }

        public virtual Task Start()
        {
            return TaskEx.Completed;
        }

        public virtual Task Stop()
        {
            return TaskEx.Completed;
        }
    }
}