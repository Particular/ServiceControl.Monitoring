namespace ServiceControl.Transports.AzureServiceBus
{
    using NServiceBus;
    using NServiceBus.Settings;
    using NServiceBus.Transport;
    using NServiceBus.Transport.AzureServiceBus;

    public class ForwardingTopologyAzureServiceBusTransport : AzureServiceBusTransport
    {
        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            // NOTE: Topology is required for v7 of the transport. 
            // As the monitoring instance is not performing pub/sub, it does not matter which topology is used. 
            // ForwardingTopology is the recommended topology for new projects

#pragma warning disable 618
            //TODO: This will sort it self out with the new transport seam
            //settings.Set<ITopology>(new ForwardingTopology());
#pragma warning restore 618

            return base.Initialize(settings, connectionString);
        }
    }
}