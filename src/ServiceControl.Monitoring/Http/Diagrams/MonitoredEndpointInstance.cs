namespace ServiceControl.Monitoring.Http.Diagrams
{
    public class MonitoredEndpointInstance
    {
        public string Name { get; set; }
        public string Id { get; set; }

        public bool IsStale { get; set; }

        public MonitoredEndpointValues ProcessingTime { get; set; }
        public MonitoredEndpointValues CriticalTime { get; set; }
        public MonitoredEndpointValues Retries { get; set; }
        
        // Unit: [msg/s]
        public MonitoredEndpointValues Throughput { get; set; }
    }
}