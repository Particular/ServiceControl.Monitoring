namespace ServiceControl.Monitoring.Http.Diagrams
{
    using System;

    public class MonitoredEndpointDetails
    {
        public MonitoredEndpointDigest Digest { get; set; }
        public MonitoredEndpointInstance[] Instances { get; set; }
        public MonitoredEndpointMessageType[] MessageTypes { get; set; }
        public GraphData GraphData { get; set; }
    }

    public class GraphData
    {
        public MonitoredValues Throughput { get; set; }
        public DateTime[] TimeAxisValues { get; set; }
        
    }
}