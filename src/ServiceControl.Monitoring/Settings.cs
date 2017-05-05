namespace ServiceControl.Monitoring
{
    using NServiceBus;

    public class Settings
    {
        public string EndpointName { get; set; } = "scmonitoring";
        public string TransportType { get; set; }
        public string TransportConnectionString { get; set; }
        public string Username { get; set; }
        public bool EnableInstallers { get; set; }

        internal static Settings Load(SettingsReader reader)
        {
            var settings = new Settings
            {
                TransportType = reader.Read("Transport/Type", typeof(MsmqTransport).AssemblyQualifiedName),
                TransportConnectionString = reader.Read<string>("Transport/ConnectionString")
            };

            return settings;
        }
    }
}