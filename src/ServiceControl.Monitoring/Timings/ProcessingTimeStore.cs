namespace ServiceControl.Monitoring.Timings
{
    using System;
    using System.Linq;
    using Infrastructure;
    public class ProcessingTimeStore : IntervalsStore, IProvideEndpointMonitoringData, IProvideEndpointInstanceMonitoringData
    {
        public void FillIn(MonitoredEndpoint[] data, DateTime now)
        {
            var snapshot = GetIntervals(now).ToLookup(x => x.Id.EndpointName);

            foreach (var item in data)
            {
                item.ProcessingTime = AggregateTimings(snapshot[item.Name].ToList());
            }
        }

        public void FillIn(MonitoredEndpointInstance[] data, DateTime now)
        {
            var snapshot = GetIntervals(now).ToLookup(k => $"{k.Id.EndpointName}-{k.Id.InstanceId}");

            foreach (var item in data)
            {
                item.ProcessingTime = AggregateTimings(snapshot[$"{item.Name}-{item.Id}"].ToList());
            }
        }
    }
}