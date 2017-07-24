using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceControl.Monitoring.Timings
{
    using QueueLength;

    public class TimingsAggregator
    {
        readonly ProcessingTimeStore processingTimeStore;
        readonly CriticalTimeStore criticalTimeStore;
        readonly QueueLengthDataStore queueLengthDataStore;

        public TimingsAggregator(ProcessingTimeStore processingTimeStore, CriticalTimeStore criticalTimeStore, QueueLengthDataStore queueLengthDataStore)
        {
            this.processingTimeStore = processingTimeStore;
            this.criticalTimeStore = criticalTimeStore;
            this.queueLengthDataStore = queueLengthDataStore;
        }

        public IEnumerable<MonitoredEndpoint> AggregateIntoLogicalEndpoints()
        {
            var now = DateTime.UtcNow;

            var processingTimes = processingTimeStore.GetTimings(now);
            var criticalTimes = criticalTimeStore.GetTimings(now);
            var queueLengths = queueLengthDataStore.GetQueueLengths(now);

            var endpointNames = processingTimes.Concat(criticalTimes).Select(t => t.Id.EndpointName).Concat(queueLengths.Keys).Distinct();

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

                Dictionary<DateTime, double> queueLength;

                if(queueLengths.TryGetValue(endpointName, out queueLength) && queueLength.Count > 0)
                { 
                    var queueLengthValues = queueLength.OrderBy(kvp => kvp.Key).ToArray();
                    var queueLengthMinDate = queueLengthValues.First().Key;

                    monitoredEndpoint.QueueLength = new LinearMonitoredValues
                    {
                        Average = queueLength.Values.Average(),
                        Points = queueLengthValues.Select(kvp => kvp.Value).ToArray(),
                        PointsAxisValues = queueLengthValues
                            .Select(kvp => (int)kvp.Key.Subtract(queueLengthMinDate).TotalMilliseconds)
                            .ToArray()
                    };
                }

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

        static MonitoredEndpointValues AggregateTimings(List<TimingsStore.EndpointInstanceTimings> timings)
        {
            Func<long, double> returnOneIfZero = x => x == 0 ? 1 : x;

            return new MonitoredEndpointValues
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
        public MonitoredEndpointValues ProcessingTime { get; set; }
        public MonitoredEndpointValues CriticalTime { get; set; }
        public LinearMonitoredValues QueueLength { get; set; }
    }

    public class MonitoredEndpointInstance
    {
        public string Name { get; set; }
        public string Id { get; set; }

        public MonitoredEndpointValues ProcessingTime { get; set; }
        public MonitoredEndpointValues CriticalTime { get; set; }
        public LinearMonitoredValues QueueLength { get; set; }
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