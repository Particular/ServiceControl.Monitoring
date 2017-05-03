namespace ServiceControl.Monitoring
{
    using System;
    using System.Threading.Tasks;
    using Http;
    using NServiceBus;
    using NServiceBus.Features;
    using NServiceBus.Logging;

    public class EndpointFactory
    {
        internal static Task<IEndpointInstance> StartEndpoint(Settings settings)
        {
            var endpointConfiguration = PrepareConfiguration(settings);
            if (settings.EnableInstallers)
            {
                endpointConfiguration.EnableInstallers();
            }
            return Endpoint.Start(endpointConfiguration);
        }

        static EndpointConfiguration PrepareConfiguration(Settings settings)
        {
            var config = new EndpointConfiguration(settings.EndpointName);
            MakeMetricsReceiver(config, settings);
            return config;
        }

        public static void MakeMetricsReceiver(EndpointConfiguration config, Settings settings)
        {
            var selectedTransportType = DetermineTransportType(settings);
            var transport = config.UseTransport(selectedTransportType);

            if (settings.TransportConnectionString != null)
            {
                transport.ConnectionString(settings.TransportConnectionString);
            }

            config.UseSerialization<NewtonsoftSerializer>();
            config.UsePersistence<InMemoryPersistence>();
            config.SendFailedMessagesTo("error");
            config.LimitMessageProcessingConcurrencyTo(1);
            config.DisableFeature<AutoSubscribe>();
            config.EnableFeature<MetricsReceiver>();
            config.EnableFeature<HttpEndpoint>();
        }

        static Type DetermineTransportType(Settings settings)
        {
            var transportType = Type.GetType(settings.TransportType);
            if (transportType != null)
            {
                return transportType;
            }

            var errorMsg = $"Configuration of transport failed. Could not resolve type `{settings.TransportType}`";
            Logger.Error(errorMsg);
            throw new Exception(errorMsg);
        }

        static ILog Logger = LogManager.GetLogger<EndpointFactory>();
    }
}