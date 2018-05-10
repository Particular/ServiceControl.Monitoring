namespace ServiceControl.Transports.AzureServiceBus
{
    using System.Threading.Tasks;
    using Monitoring;
    using Monitoring.Infrastructure;
    using Monitoring.Messaging;
    using Monitoring.QueueLength;
    using NServiceBus.Metrics;

    public class QueueLengthProvider : IProvideQueueLength
    {
        public void Initialize(string connectionString, QueueLengthStore store)
        {
            // Save this stuff
        }

        public void Process(EndpointInstanceId endpointInstanceId, EndpointMetadataReport metadataReport)
        {
            // HINT: The endpoint is reporting which queues it reads from
        }

        public void Process(EndpointInstanceId endpointInstanceId, TaggedLongValueOccurrence metricsReport)
        {
            // HINT: The endpoint is reporting queue length
        }

        // Start updating the queue length store in the background
        public Task Start() => TaskEx.Completed;

        public Task Stop() => TaskEx.Completed;
    }
}