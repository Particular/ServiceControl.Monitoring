namespace ServiceControl.Transports.AzureServiceBus
{
    using NServiceBus;
    using NServiceBus.Settings;
    using NServiceBus.Transport;

    public class EndpointOrientedTopologyAzureServiceBusTransport : AzureServiceBusTransport
    {
        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            //TODO add the configuration for the endpoint oriented topology

            return base.Initialize(settings, connectionString);
        }
    }
}