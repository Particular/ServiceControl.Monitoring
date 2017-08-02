namespace ServiceControl.Monitoring.Http.Diagrams
{
    public class MonitoredEndpoint
    {
        public string Name { get; set; }
        public string[] EndpointInstanceIds { get; set; }
        public MonitoredEndpointValues ProcessingTime { get; set; }
        public MonitoredEndpointValues CriticalTime { get; set; }
        public MonitoredEndpointValues Retries { get; set; }
        public MonitoredEndpointValues QueueLength { get; set; }
        public MonitoredEndpointValues Throughput { get; set; }
    }
}