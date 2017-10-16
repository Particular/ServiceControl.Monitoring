namespace ServiceControl.Monitoring.SmokeTests.RabbitMQ
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Support;
    using ScenarioDescriptors;

    public class ConfigureEndpointRabbitMQTransport : IConfigureEndpointTestExecution
    {
        public static string ConnectionString => EnvironmentHelper.GetEnvironmentVariable($"{nameof(RabbitMQTransport)}.ConnectionString") ?? "host=localhost";

        public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
        {
            var connectionString = ConnectionString;

            var transportConfig = configuration
                .UseTransport<RabbitMQTransport>()
                .ConnectionString(connectionString);

            transportConfig.DelayedDelivery().DisableTimeoutManager();

            configuration.UseSerialization<JsonSerializer>();

            return Task.FromResult(0);
        }

        public Task Cleanup()
        {
            return Task.FromResult(0);
        }
    }
}