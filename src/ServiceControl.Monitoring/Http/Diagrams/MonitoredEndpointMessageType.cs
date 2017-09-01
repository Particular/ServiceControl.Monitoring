namespace ServiceControl.Monitoring.Http.Diagrams
{
    using System.Collections.Generic;

    public class MonitoredEndpointMessageType
    {
        public string MessageType { get; set; }

        public Dictionary<string, MonitoredValues> Metrics { get; } = new Dictionary<string, MonitoredValues>();
    }
}