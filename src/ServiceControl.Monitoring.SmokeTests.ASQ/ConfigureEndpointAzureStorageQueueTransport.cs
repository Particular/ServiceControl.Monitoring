namespace ServiceControl.Monitoring.SmokeTests.ASQ
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Support;
    using ScenarioDescriptors;

    public class ConfigureEndpointAzureStorageQueueTransport : IConfigureEndpointTestExecution
    {
        public static string ConnectionString => EnvironmentHelper.GetEnvironmentVariable($"{nameof(AzureStorageQueueTransport)}.ConnectionString") ?? "UseDevelopmentStorage=true";

        public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
        {
            var connectionString = ConnectionString;

            var transportConfig = configuration
                .UseTransport<AzureStorageQueueTransport>()
                .ConnectionString(connectionString)
                .MessageInvisibleTime(TimeSpan.FromSeconds(30));

            transportConfig.DelayedDelivery().DisableTimeoutManager();

            var routingConfig = transportConfig.Routing();

            foreach (var publisher in publisherMetadata.Publishers)
            {
                foreach (var eventType in publisher.Events)
                {
                    routingConfig.RegisterPublisher(eventType, publisher.PublisherName);
                }
            }

            configuration.UseSerialization<JsonSerializer>();

            return Task.FromResult(0);
        }

        public Task Cleanup()
        {
            return Task.FromResult(0);
        }
    }
}