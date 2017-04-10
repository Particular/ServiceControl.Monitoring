namespace ServiceControl.Monitoring
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Features;

    public class EndpointFactory
    {
        internal static async Task<IEndpointInstance> StartEndpoint(bool enableInstallers = false)
        {
            var endpointConfiguration = PrepareConfiguration();
            if (enableInstallers)
            {
                endpointConfiguration.EnableInstallers();
            }
            return await Endpoint.Start(endpointConfiguration).ConfigureAwait(false);
        }

        static EndpointConfiguration PrepareConfiguration()
        {
            var config = new EndpointConfiguration("scmonitoring");
            MakeMetricsReceiver(config);
            return config;
        }

        public static void MakeMetricsReceiver(EndpointConfiguration config)
        {
            config.UseTransport<MsmqTransport>();
            config.UseSerialization<NewtonsoftSerializer>();
            config.UsePersistence<InMemoryPersistence>();
            config.SendFailedMessagesTo("error");
            config.LimitMessageProcessingConcurrencyTo(1);
            config.DisableFeature<AutoSubscribe>();
            config.EnableFeature<MetricsReceiver>();
        }
    }
}