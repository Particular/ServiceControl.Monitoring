﻿namespace ServiceControl.Monitoring
{
    using System;
    using System.Threading.Tasks;
    using Http;
    using NServiceBus;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Features;
    using NServiceBus.Logging;
    using Raw;

    public class EndpointFactory
    {
        internal static Task<IEndpointInstance> StartEndpoint(Settings settings)
        {
            var endpointConfiguration = PrepareConfiguration(settings);
            return Endpoint.Start(endpointConfiguration);
        }

        public static EndpointConfiguration PrepareConfiguration(Settings settings)
        {
            var config = new EndpointConfiguration(settings.EndpointName);
            MakeMetricsReceiver(config, settings);
            return config;
        }

        public static void MakeMetricsReceiver(EndpointConfiguration config, Settings settings)
        {
            var selectedTransportType = DetermineTransportType(settings);
            var transport = config.UseTransport(selectedTransportType);

            transport.ConnectionStringName("NServiceBus/Transport");

            if (settings.EnableInstallers)
            {
                config.EnableInstallers(settings.Username);
            }

            config.GetSettings().Set<Settings>(settings);

            config.UseSerialization<NewtonsoftSerializer>();
            config.UsePersistence<InMemoryPersistence>();
            config.SendFailedMessagesTo(settings.ErrorQueue);
            config.LimitMessageProcessingConcurrencyTo(1);
            config.DisableFeature<AutoSubscribe>();
            config.EnableFeature<RawMetricsFeature>();
            config.EnableFeature<QueueLength.QueueLengthFeature>();
            config.EnableFeature<HttpEndpoint>();
            config.EnableFeature<DiagramFeature>();
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