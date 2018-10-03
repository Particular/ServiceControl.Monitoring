namespace ServiceControl.Transports.AzureServiceBus
{
    using NServiceBus;
    using NServiceBus.Settings;
    using NServiceBus.Transport;

    public class EndpointOrientedTopologyAzureServiceBusTransport : AzureServiceBusTransport
    {
        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            settings.Set("AzureServiceBus.Settings.Topology.Selected", "EndpointOrientedTopology");

            return base.Initialize(settings, connectionString);
        }
    }
}