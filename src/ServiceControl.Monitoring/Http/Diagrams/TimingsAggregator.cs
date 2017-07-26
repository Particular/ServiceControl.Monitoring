using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceControl.Monitoring.Timings
{
    using Infrastructure;

    public class TimingsAggregator
    {
        readonly EndpointRegistry endpointRegistry;
        readonly IProvideEndpointMonitoringData[] endpointMonitoringDataProviders;
        readonly IProvideEndpointInstanceMonitoringData[] endpointInstanceMonitoringDataProviders;

        public TimingsAggregator(EndpointRegistry endpointRegistry, IProvideEndpointMonitoringData[] endpointMonitoringDataProviders, IProvideEndpointInstanceMonitoringData[] endpointInstanceMonitoringDataProviders)
        {
            this.endpointRegistry = endpointRegistry;
            this.endpointMonitoringDataProviders = endpointMonitoringDataProviders;
            this.endpointInstanceMonitoringDataProviders = endpointInstanceMonitoringDataProviders;
        }

        internal IEnumerable<MonitoredEndpoint> AggregateIntoLogicalEndpoints()
        {
            var now = DateTime.UtcNow;

            var result = endpointRegistry.AllEndpoints()
                .Select(endpoint => new MonitoredEndpoint
                {
                    Name = endpoint.Key,
                    EndpointInstanceIds = endpoint.Value.Select(x => x.InstanceId).ToArray()
                }).ToArray();

            foreach (var endpointMonitoringDataProvider in endpointMonitoringDataProviders)
            {
                endpointMonitoringDataProvider.FillIn(result, now);
            }

            return result;
        }

        internal IEnumerable<MonitoredEndpointInstance> AggregateDataForLogicalEndpoint(string endpointName)
        {
            var now = DateTime.UtcNow;

            var monitoredEndpointInstances = endpointRegistry.AllEndpoints()[endpointName]
                .Select(endpointInstance => new MonitoredEndpointInstance
                {
                    Id = endpointInstance.InstanceId,
                    Name = endpointInstance.EndpointName
                }).ToArray();

            foreach (var endpointInstanceMonitoringDataProvider in endpointInstanceMonitoringDataProviders)
            {
                endpointInstanceMonitoringDataProvider.FillIn(monitoredEndpointInstances, now);
            }

            return monitoredEndpointInstances;
        }
    }

    public class MonitoredEndpoint
    {
        public string Name { get; set; }
        public string[] EndpointInstanceIds { get; set; }
        public MonitoredEndpointValues ProcessingTime { get; set; }
        public MonitoredEndpointValues CriticalTime { get; set; }
        public MonitoredEndpointValues Retries { get; set; }
        public LinearMonitoredValues QueueLength { get; set; }
    }

    public class MonitoredEndpointInstance
    {
        public string Name { get; set; }
        public string Id { get; set; }

        public MonitoredEndpointValues ProcessingTime { get; set; }
        public MonitoredEndpointValues CriticalTime { get; set; }
        public MonitoredEndpointValues Retries { get; set; }
    }

    public class MonitoredEndpointValues
    {
        public double? Average { get; set; }
        public double[] Points { get; set; }
    }

    public class LinearMonitoredValues : MonitoredEndpointValues
    {
        public int[] PointsAxisValues { get; set; }
    }
}