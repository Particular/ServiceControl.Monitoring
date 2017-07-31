namespace ServiceControl.Monitoring.Http.Diagrams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;

    public static class IntervalsAggregator
    {
        internal static MonitoredEndpointValues AggregateTimings(List<IntervalsStore.EndpointInstanceIntervals> intervals)
        {
            Func<long, double> returnOneIfZero = x => x == 0 ? 1 : x;

            return new MonitoredEndpointValues
            {
                Average = intervals.Sum(t => t.TotalValue) / returnOneIfZero(intervals.Sum(t => t.TotalMeasurements)),
                Points = intervals.SelectMany(t => t.Intervals)
                    .GroupBy(i => i.IntervalStart)
                    .OrderBy(g => g.Key)
                    .Select(g => g.Sum(i => i.TotalValue) / returnOneIfZero(g.Sum(i => i.TotalMeasurements)))
                    .ToArray()
            };
        }

        internal static MonitoredEndpointValues AggregateRetries(List<IntervalsStore.EndpointInstanceIntervals> intervals)
        {
            Func<long, double> returnOneIfZero = x => x == 0 ? 1 : x;

            return new MonitoredEndpointValues
            {
                Average = intervals.Sum(t => t.TotalValue) / returnOneIfZero(intervals.Sum(t => t.Intervals.Length)),
                Points = intervals.SelectMany(t => t.Intervals)
                    .GroupBy(i => i.IntervalStart)
                    .OrderBy(g => g.Key)
                    .Select(g => (double)g.Sum(i => i.TotalValue))
                    .ToArray()
            };
        }

        internal static MonitoredEndpointValues AggregateQueueLength(List<IntervalsStore.EndpointInstanceIntervals> intervals)
        {
            Func<long, double> returnOneIfZero = x => x == 0 ? 1 : x;

            return new MonitoredEndpointValues
            {
                // To calculate the average we count TotalMeasurements instead of the number of intervals. 
                // This means that if in a specific interval value was not snapshotted (and recorded in the store)
                // it will have no impact on average. In other words, only intervals that had their value captured will be considered in calculations.
                // This ensures that receiving no-data will have no impact on the average, presenting average only from the periods that were reported.

                Average = intervals.Sum(t => t.TotalValue) / returnOneIfZero(intervals.Sum(t => t.TotalMeasurements)),
                Points = intervals.SelectMany(t => t.Intervals)
                    .GroupBy(i => i.IntervalStart)
                    .OrderBy(g => g.Key)
                    .Select(g => g.Sum(i => i.TotalValue) / returnOneIfZero(g.Sum(i => i.TotalMeasurements)))
                    .ToArray()
            };
        }
    }
}