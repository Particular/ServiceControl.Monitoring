namespace ServiceControl.Monitoring.Http.Diagrams
{
    public class MonitoredEndpointDetails
    {
        public MonitoredEndpointInstance[] Instances { get; set; }
        public MonitoredEndpointMessageType[] MessageTypes { get; set; }
    }
}