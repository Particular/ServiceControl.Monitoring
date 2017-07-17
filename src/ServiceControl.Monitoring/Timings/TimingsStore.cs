namespace ServiceControl.Monitoring.Timings
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Metrics.Raw;

    public abstract class TimingsStore
    {
        ConcurrentDictionary<EndpointInstanceId, ConcurrentDictionary<DateTime, MeasurementInterval>> timings = 
            new ConcurrentDictionary<EndpointInstanceId, ConcurrentDictionary<DateTime, MeasurementInterval>>();

        public void Store(EndpointInstanceId instanceId, LongValueOccurrences message, DateTime now)
        {
            var instanceData = timings.GetOrAdd(instanceId, _ => new ConcurrentDictionary<DateTime, MeasurementInterval>());

            for (var i = 0; i < message.Ticks.Length; i++)
            {
                var date = new DateTime(message.BaseTicks + message.Ticks[i], DateTimeKind.Utc);
                var intervalId = date.RoundDownToNearest(IntervalSize);

                instanceData.AddOrUpdate(
                    intervalId,
                    _ => new MeasurementInterval(1, message.Values[i]),
                    (_, b) => b.Update(message.Values[i]));
            }

            if (instanceData.Count > 2 * NumberOfHistoricalIntervals)
            {
                var historySize = TimeSpan.FromTicks(IntervalSize.Ticks * NumberOfHistoricalIntervals);
                var oldestValidInterval = now.Subtract(historySize).RoundDownToNearest(IntervalSize);

                foreach (var interval in instanceData.Keys)
                {
                    if (interval < oldestValidInterval)
                    {
                        MeasurementInterval bucket;
                        instanceData.TryRemove(interval, out bucket);
                    }
                }
            }
        }

        public EndpointInstanceTimings[] GetTimings(DateTime now)
        {
            var result = new List<EndpointInstanceTimings>();
            var intervals = GenerateIntervalIds(now, NumberOfHistoricalIntervals);

            foreach (var instanceId in timings.Keys)
            {
                ConcurrentDictionary<DateTime, MeasurementInterval> endpointInstanceData;

                if (timings.TryGetValue(instanceId, out endpointInstanceData))
                {
                    var totalDuration = 0L;
                    var totalMeasurements = 0L;
                    var timingIntervals = new TimingInterval[intervals.Length];

                    for (var i = 0; i < intervals.Length; i++)
                    {
                        timingIntervals[i] = new TimingInterval
                        {
                            IntervalStart = intervals[i]
                        };

                        MeasurementInterval bucket;

                        if (endpointInstanceData.TryGetValue(intervals[i], out bucket))
                        {
                            totalDuration += bucket.TotalTime;
                            totalMeasurements += bucket.TotalMeasurements;

                            timingIntervals[i].TotalTime = bucket.TotalTime;
                            timingIntervals[i].TotalMeasurements = bucket.TotalMeasurements;
                        }
                    }

                    result.Add(new EndpointInstanceTimings
                    {
                        Id = instanceId,
                        TotalMeasurements = totalMeasurements,
                        TotalTime = totalDuration,
                        Intervals = timingIntervals
                    });
                }
            }

            return result.ToArray();
        }

        static DateTime[] GenerateIntervalIds(DateTime start, int numberOfPastIntervals)
        {
            var intervals = new DateTime[numberOfPastIntervals];

            intervals[0] = start.RoundDownToNearest(TimeSpan.FromSeconds(15));

            for (var i = 1; i < numberOfPastIntervals; i++)
            {
                intervals[i] = intervals[i - 1].Subtract(IntervalSize);
            }

            return intervals;
        }

        struct MeasurementInterval
        {
            public int TotalMeasurements;
            public long TotalTime;

            public MeasurementInterval(int totalMeasurements, long totalTime) : this()
            {
                TotalMeasurements = totalMeasurements;
                TotalTime = totalTime;
            }

            public MeasurementInterval Update(long measuredTime)
            {
                return new MeasurementInterval(TotalMeasurements + 1, TotalTime + measuredTime);
            }
        }

        public class EndpointInstanceTimings { 
            public EndpointInstanceId Id { get; set; }
            public TimingInterval[] Intervals { get; set; }
            public long TotalTime { get; set; }
            public long TotalMeasurements { get; set; }
        }

        public class TimingInterval
        {
            public DateTime IntervalStart { get; set; }
            public long TotalTime { get; set; }
            public long TotalMeasurements { get; set; }
        }

        /// Number of 15s intervals in 5 minutes
        internal static int NumberOfHistoricalIntervals = 4 * 5;

        static TimeSpan IntervalSize = TimeSpan.FromSeconds(15);
    }
}