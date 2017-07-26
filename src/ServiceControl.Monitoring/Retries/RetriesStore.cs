namespace ServiceControl.Monitoring
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;
    using Timings;
    public class RetriesStore : IntervalsStore, IProvideEndpointMonitoringData, IProvideEndpointInstanceMonitoringData
    {
        public void FillIn(MonitoredEndpoint[] data, DateTime now)
        {
            var snapshot = GetIntervals(now).ToLookup(x => x.Id.EndpointName);

            foreach (var item in data)
            {
                item.Retries = AggregateRetries(snapshot[item.Name].ToList());
            }
        }

        public void FillIn(MonitoredEndpointInstance[] data, DateTime now)
        {
            var snapshot = GetIntervals(now).ToLookup(k => $"{k.Id.EndpointName}-{k.Id.InstanceId}");

            foreach (var item in data)
            {
                item.Retries = AggregateRetries(snapshot[$"{item.Name}-{item.Id}"].ToList());
            }
        }

        static MonitoredEndpointValues AggregateRetries(List<EndpointInstanceIntervals> timings)
        {
            Func<long, double> returnOneIfZero = x => x == 0 ? 1 : x;

            return new MonitoredEndpointValues
            {
                Average = timings.Sum(t => t.TotalValue) / returnOneIfZero(timings.Sum(t => t.Intervals.Length)),
                Points = timings.SelectMany(t => t.Intervals)
                    .GroupBy(i => i.IntervalStart)
                    .OrderBy(g => g.Key)
                    .Select(g => (double)g.Sum(i => i.TotalValue))
                    .ToArray()
            };
        }

    }
}