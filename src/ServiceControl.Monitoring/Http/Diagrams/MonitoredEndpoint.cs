namespace ServiceControl.Monitoring.Http
{
    public class MonitoredEndpoint
    {
        public string Name { get; set; }
        public MonitoredEndpointTimings ProcessingTime { get; set; }
        public MonitoredEndpointTimings CriticalTime { get; set; }
    }
}