namespace ServiceControl.Monitoring.SmokeTests.Learning
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Support;

    public class ConfigureEndpointLearningTransport : IConfigureEndpointTestExecution
    {
        public static string ConnectionString = @"C:\Temp\Learning";

        public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
        {
            configuration.UseTransport<ServiceControlLearningTransport>().ConnectionString(ConnectionString);

            return Task.FromResult(0);
        }

        public Task Cleanup()
        {
            return Task.FromResult(0);
        }
    }
}