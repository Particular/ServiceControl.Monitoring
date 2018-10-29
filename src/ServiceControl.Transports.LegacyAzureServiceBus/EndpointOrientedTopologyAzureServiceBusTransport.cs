namespace ServiceControl.Transports.LegacyAzureServiceBus
{
    using NServiceBus;
    using NServiceBus.Settings;
    using NServiceBus.Transport;

    public class EndpointOrientedTopologyAzureServiceBusTransport : AzureServiceBusTransport
    {
        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            var extensions = new TransportExtensions<AzureServiceBusTransport>(settings);
            extensions.UseEndpointOrientedTopology();

            return base.Initialize(settings, connectionString);
        }
    }
}