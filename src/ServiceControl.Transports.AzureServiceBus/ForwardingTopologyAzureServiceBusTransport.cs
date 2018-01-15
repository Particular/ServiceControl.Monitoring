namespace ServiceControl.Transports.AzureServiceBus
{
    using NServiceBus;
    using NServiceBus.Settings;
    using NServiceBus.Transport;

    public class ForwardingTopologyAzureServiceBusTransport : AzureServiceBusTransport
    {
        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            // NOTE: Topology is required for v7 of the transport. 
            // As the monitoring instance is not performing pub/sub, it does not matter which topology is used. 
            // ForwardingTopology is the recommended topology for new projects

            settings.Set("AzureServiceBus.Settings.Topology.Selected", "ForwardingTopology");

            return base.Initialize(settings, connectionString);
        }
    }
}