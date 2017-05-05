namespace ServiceControl.Monitoring
{
    using System;
    using System.IO;
    using NLog;
    using NServiceBus;

    public class Settings
    {
        const string DEFAULT_ENDPOINT_NAME = "Particular.ServiceControl.Monitoring";
        public string EndpointName { get; set; } = DEFAULT_ENDPOINT_NAME;
        public string TransportType { get; set; }
        public string TransportConnectionString { get; set; }
        public string LogPath { get; set; }
        public LogLevel LogLevel { get; set; }
        public string Username { get; set; }
        public bool EnableInstallers { get; set; }

        internal static Settings Load(SettingsReader reader)
        {
            var settings = new Settings
            {
                TransportType = reader.Read("Transport/Type", typeof(MsmqTransport).AssemblyQualifiedName),
                TransportConnectionString = reader.Read<string>("Transport/ConnectionString"),
                LogPath = CalculateLogPathForEndpointName(DEFAULT_ENDPOINT_NAME),
                LogLevel = MonitorLogs.InitializeLevel(reader)
            };

            return settings;
        }

        internal static string CalculateLogPathForEndpointName(string endpointName)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $"Particular\\{endpointName}\\logs");
        }
    }
}