namespace ServiceControl.Monitoring.SmokeTests.SQS
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Support;
    using Transports.AmazonSQS;

    public class ConfigureEndpointSqsTransport : IConfigureEndpointTestExecution
    {
        public static string ConnectionString => string.Join(";",
            Build("AccessKeyId", "AWS_ACCESS_KEY_ID"),
            Build("SecretAccessKey", "AWS_SECRET_ACCESS_KEY"),
            Build("Region", "AWS_REGION"));

        static string Build(string name, string envName) => $"{name}={Environment.GetEnvironmentVariable(envName)}";

        public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
        {
            var transport = configuration
                .UseTransport<ServiceControlSqsTransport>()
                .ConnectionString(ConnectionString);

            var routingConfig = transport.Routing();

            foreach (var publisher in publisherMetadata.Publishers)
            {
                foreach (var eventType in publisher.Events)
                {
                    routingConfig.RegisterPublisher(eventType, publisher.PublisherName);
                }
            }

            return Task.FromResult(0);
        }

        public Task Cleanup() => Task.CompletedTask;
    }
}