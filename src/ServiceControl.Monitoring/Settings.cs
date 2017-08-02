namespace ServiceControl.Monitoring
{
    using System.IO;
    using System.Reflection;
    using NLog;
    using NServiceBus;
    using System;

    public class Settings
    {
        const string DEFAULT_ENDPOINT_NAME = "Particular.ServiceControl.Monitoring";

        public string EndpointName { get; set; } = DEFAULT_ENDPOINT_NAME;
        public string TransportType { get; set; }
        public string ErrorQueue { get; set; }
        public string LogPath { get; set; }
        public LogLevel LogLevel { get; set; }
        public string Username { get; set; }
        public bool EnableInstallers { get; set; }
        public string HttpHostName { get; set; }
        public string HttpPort { get; set; }
        public TimeSpan EndpointUptimeGracePeriod { get; set; }

        internal static Settings Load(SettingsReader reader)
        {
            var settings = new Settings
            {
                TransportType = reader.Read("Monitoring/TransportType", typeof(MsmqTransport).AssemblyQualifiedName),
                LogLevel = MonitorLogs.InitializeLevel(reader),
                LogPath = reader.Read("Monitoring/LogPath", DefautLogLocation()),
                ErrorQueue = reader.Read("Monitoring/ErrorQueue", "error"),
                HttpHostName = reader.Read<string>("Monitoring/HttpHostname"),
                HttpPort = reader.Read<string>("Monitoring/HttpPort"),
                EndpointUptimeGracePeriod = TimeSpan.Parse(reader.Read<string>("Monitoring/EndpointUptimeGracePeriod", "00:00:40"))
            };
            return settings;
        }

        // SC installer always populates LogPath in app.config on installation/change/upgrade so this will only be used when
        // debugging or if the entry is removed manually. In those circumstances default to the folder containing the exe
        internal static string DefautLogLocation()
        {
            var assemblyLocation = Assembly.GetEntryAssembly().Location;
            return Path.GetDirectoryName(assemblyLocation);
        }
    }
}