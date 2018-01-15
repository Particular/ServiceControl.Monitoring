namespace ServiceControl.Monitoring.SmokeTests.SQS
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Support;

    public class ConfigureEndpointSqsTransport : IConfigureEndpointTestExecution
    {
        public Task Configure(string endpointName, EndpointConfiguration configuration, RunSettings settings, PublisherMetadata publisherMetadata)
        {
            var transport = configuration
                .UseTransport<SqsTransport>();

            transport.Region(Environment.GetEnvironmentVariable("AWS_REGION", EnvironmentVariableTarget.User));

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