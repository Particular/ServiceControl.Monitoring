namespace ServiceControl.Monitoring.SmokeTests.SQS.EndpointTemplates
{
    using NServiceBus.AcceptanceTesting.Support;

    public interface ITestSuiteConstraints
    {
        bool SupportsDtc { get; }

        bool SupportsCrossQueueTransactions { get; }

        bool SupportsNativePubSub { get; }

        bool SupportsNativeDeferral { get; }

        bool SupportsOutbox { get; }

        IConfigureEndpointTestExecution TransportConfiguration { get; }

        IConfigureEndpointTestExecution PersistenceConfiguration { get; }
    }

    // ReSharper disable once PartialTypeWithSinglePart
    public partial class TestSuiteConstraints
    {
        public bool SupportsCrossQueueTransactions => false;
        public bool SupportsDtc => false;
        public bool SupportsNativeDeferral => true;
        public bool SupportsNativePubSub => false;
        public bool SupportsOutbox => false;
        public IConfigureEndpointTestExecution CreateTransportConfiguration() => new ConfigureEndpointSqsTransport();
        public IConfigureEndpointTestExecution CreatePersistenceConfiguration() => new ConfigureEndpointInMemoryPersistence();
    }
}