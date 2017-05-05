namespace ServiceControl.Monitoring
{
    using System.Threading.Tasks;
    using NServiceBus;

    class SetupCommand : AbstractCommand
    {
        public override Task Execute(Settings settings)
        {
            var endpointConfig = EndpointFactory.PrepareConfiguration(settings);
            endpointConfig.EnableInstallers(settings.Username);
            return Endpoint.Create(endpointConfig);
        }
    }
}