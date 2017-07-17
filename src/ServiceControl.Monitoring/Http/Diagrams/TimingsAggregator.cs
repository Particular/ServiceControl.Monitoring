using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceControl.Monitoring.Timings
{
    public class TimingsAggregator
    {
        readonly ProcessingTimeStore processingTimeStore;
        readonly CriticalTimeStore criticalTimeStore;

        public TimingsAggregator(ProcessingTimeStore processingTimeStore, CriticalTimeStore criticalTimeStore)
        {
            this.processingTimeStore = processingTimeStore;
            this.criticalTimeStore = criticalTimeStore;
        }

        public IEnumerable<MonitoredEndpoint> AggregateIntoLogicalEndpoints()
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


        internal IEnumerable<MonitoredEndpointInstance> AggregateDataForLogicalEndpoint(string endpointName)
        {
            var now = DateTime.UtcNow;

            var processingTime = processingTimeStore.GetTimings(now).Where(pt => pt.Id.EndpointName == endpointName).ToList();
            var criticalTime = criticalTimeStore.GetTimings(now).Where(ct => ct.Id.EndpointName == endpointName).ToList();

            var instanceIds = criticalTime.Concat(processingTime).Select(i => i.Id).Distinct();

            foreach (var instanceId in instanceIds)
            {
                var monitoredInstance = new MonitoredEndpointInstance
                {
                    Id = instanceId.InstanceId,
                    Name = instanceId.InstanceName,
                    ProcessingTime = AggregateTimings(processingTime.Where(pt => pt.Id.Equals(instanceId)).ToList()),
                    CriticalTime = AggregateTimings(criticalTime.Where(ct => ct.Id.Equals(instanceId)).ToList())
                };

                yield return monitoredInstance;
            }
        }

        static MonitoredTimings AggregateTimings(List<TimingsStore.EndpointInstanceTimings> timings)
        {
            Func<long, double> returnOneIfZero = x => x == 0 ? 1 : x;

            return new MonitoredTimings
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
        public MonitoredTimings ProcessingTime { get; set; }
        public MonitoredTimings CriticalTime { get; set; }
    }

    public class MonitoredEndpointInstance
    {
        public string Name { get; set; }
        public string Id { get; set; }

        public MonitoredTimings ProcessingTime { get; set; }
        public MonitoredTimings CriticalTime { get; set; }
    }

    public class MonitoredTimings
    {
        public double? Average { get; set; }
        public double[] Points { get; set; }
    }
}