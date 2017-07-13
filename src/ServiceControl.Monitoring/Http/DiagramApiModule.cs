namespace ServiceControl.Monitoring.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Nancy;
    using Timings;

    /// <summary>
    /// Exposes ServiceControl.Monitoring metrics needed for in endpoint overview page.
    /// </summary>
    public class MonitoredEndpointsModule : ApiModule
    {
        /// <summary>
        /// Initializes the metric API module.
        /// </summary>
        public MonitoredEndpointsModule(ProcessingTimeStore processingTimeStore, CriticalTimeStore criticalTimeStore)
        {
            Get["/monitored-endpoints"] = x =>
            {
                var now = DateTime.UtcNow;

                var processingTimes = processingTimeStore.GetTimings(now);
                var criticalTimes = criticalTimeStore.GetTimings(now);

                var endpointNames = processingTimes.Select(e => e.EndpointName)
                    .Union(criticalTimes.Select(e => e.EndpointName));

                var endpointsResult = new List<MonitoredEndpoint>();

                foreach (var endpointName in endpointNames)
                {
                    var processingTime = processingTimes.FirstOrDefault(pt => pt.EndpointName == endpointName);
                    var criticalTime = criticalTimes.FirstOrDefault(ct => ct.EndpointName == endpointName);

                    endpointsResult.Add(new MonitoredEndpoint
                    {
                        Name = endpointName,
                        ProcessingTime = new MonitoredEndpointTimings
                        {
                            Average = processingTime?.Average,
                            Points = processingTime?.Intervals.OrderBy(i => i.IntervalStart).Select(i => i.Average).ToArray()
                        },
                        CriticalTime = new MonitoredEndpointTimings
                        {
                            Average = criticalTime?.Average,
                            Points = criticalTime?.Intervals.OrderBy(i => i.IntervalStart).Select(i => i.Average).ToArray()
                        }
                    });
                }

                return Negotiate.WithModel(endpointsResult.ToArray());
            };
        }
    }

    public class MonitoredEndpoint
    {
        public string Name { get; set; }
        public MonitoredEndpointTimings ProcessingTime { get; set; }
        public MonitoredEndpointTimings CriticalTime { get; set; }
    }

    public class MonitoredEndpointTimings
    {
        public double? Average { get; set; }
        public double[] Points { get; set; }
    }
}