using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceControl.Monitoring.Timings
{
    class TimingsAggregator
    {
        public static IEnumerable<MonitoredEndpoint> AggregateIntoLogicalEndpoints(ProcessingTimeStore processingTimeStore, CriticalTimeStore criticalTimeStore)
        {
            var now = DateTime.UtcNow;

            var processingTimes = processingTimeStore.GetTimings(now);
            var criticalTimes = criticalTimeStore.GetTimings(now);

            var endpointNames = processingTimes.Concat(criticalTimes).Select(t => t.Id.EndpointName).Distinct();

            foreach (var endpointName in endpointNames)
            {
                var instanceProcessingTime = processingTimes.Where(pt => pt.Id.EndpointName == endpointName).ToList();
                var instanceCriticalTime = criticalTimes.Where(ct => ct.Id.EndpointName == endpointName).ToList();

                var monitoredEndpoint = new MonitoredEndpoint
                {
                    Name = endpointName,
                    EndpointInstanceIds = new[] {instanceProcessingTime, instanceCriticalTime }
                        .SelectMany(t => t).Select(t => t.Id.InstanceId)
                        .Distinct().ToArray(),
                    ProcessingTime = AggregateTimings(instanceProcessingTime),
                    CriticalTime = AggregateTimings(instanceCriticalTime)
                };

                yield return monitoredEndpoint;
            }
        }

        static MonitoredEndpointTimings AggregateTimings(List<TimingsStore.EndpointInstanceTimings> timings)
        {
            Func<long, double> returnOneIfZero = x => x == 0 ? 1 : x;

            return new MonitoredEndpointTimings
            {
                Average = timings.Sum(t => t.TotalTime) / returnOneIfZero(timings.Sum(t => t.TotalMeasurements)),
                Points = timings.SelectMany(t => t.Intervals)
                                .GroupBy(i => i.IntervalStart)
                                .OrderBy(g => g.Key)
                                .Select(g => g.Sum(i => i.TotalTime) / returnOneIfZero(g.Sum(i => i.TotalMeasurements)))
                                .ToArray()
            };
        }
    }

    public class MonitoredEndpoint
    {
        public string Name { get; set; }
        public string[] EndpointInstanceIds { get; set; }
        public MonitoredEndpointTimings ProcessingTime { get; set; }
        public MonitoredEndpointTimings CriticalTime { get; set; }
    }

    public class MonitoredEndpointTimings
    {
        public double? Average { get; set; }
        public double[] Points { get; set; }
    }
}