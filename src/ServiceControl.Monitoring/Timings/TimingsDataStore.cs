namespace ServiceControl.Monitoring.Timings
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Processing.RawData.NServiceBus.Metrics;

    public class TimingsDataStore
    {
        ConcurrentDictionary<string, ConcurrentDictionary<DateTime, MeasurementInterval>> CriticalTimes = 
            new ConcurrentDictionary<string, ConcurrentDictionary<DateTime, MeasurementInterval>>();

        ConcurrentDictionary<string, ConcurrentDictionary<DateTime, MeasurementInterval>> ProcessingTimes =  
            new ConcurrentDictionary<string, ConcurrentDictionary<DateTime, MeasurementInterval>>();

        public void StoreProcessingTime(string endpointName, LongValueOccurrences message)
        {
            var endpointData = ProcessingTimes.GetOrAdd(endpointName, new ConcurrentDictionary<DateTime, MeasurementInterval>());

            Store(message, endpointData);
        }

        public void StoreCriticalTime(string endpointName, LongValueOccurrences message)
        {
            var endpointData = CriticalTimes.GetOrAdd(endpointName, new ConcurrentDictionary<DateTime, MeasurementInterval>());

            Store(message, endpointData);
        }

        void Store(LongValueOccurrences message, ConcurrentDictionary<DateTime, MeasurementInterval> endpointData)
        {
            for (var i = 0; i < message.Ticks.Length; i++)
            {
                var date = new DateTime(message.BaseTicks + message.Ticks[i]);
                var intervalId = IntervalId(date);

                endpointData.AddOrUpdate(
                    intervalId,
                    _ => new MeasurementInterval(1, message.Values[i]),
                    (_, b) =>
                    {
                        b.TotalMeasurements += 1;
                        b.TotalTime += message.Values[i];
                        return b;
                    });
            }

            if (endpointData.Count > 2 * MaxHistoricalIntervals)
            {
                var validIntervals = GenerateIntervalIds(DateTime.Now, MaxHistoricalIntervals);

                foreach (var interval in endpointData.Keys)
                {
                    if (validIntervals.Contains(interval) == false)
                    {
                        MeasurementInterval bucket;
                        endpointData.TryRemove(interval, out bucket);
                    }
                }

            }
        }

        public EndpointTimings[] GetCriticalTime()
        {
            return GetTime(CriticalTimes);
        }

        public EndpointTimings[] GetProcessingTime()
        {
            return GetTime(ProcessingTimes);
        }

        static EndpointTimings[] GetTime(ConcurrentDictionary<string, ConcurrentDictionary<DateTime, MeasurementInterval>> times)
        {
            var result = new List<EndpointTimings>();
            var intervals = GenerateIntervalIds(DateTime.Now, MaxHistoricalIntervals);

            foreach (var endpointName in times.Keys)
            {
                ConcurrentDictionary<DateTime, MeasurementInterval> endpointData;

                if (times.TryGetValue(endpointName, out endpointData))
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

                        if (endpointData.TryGetValue(intervals[i], out bucket))
                        {
                            totalDuration += bucket.TotalTime;
                            totalMeasurements += bucket.TotalMeasurements;

                            timingIntervals[i].Average = bucket.TotalTime / (double) bucket.TotalMeasurements;
                        }
                    }

                    var endpointTimings = new EndpointTimings
                    {
                        EndpointName = endpointName,
                        Average = totalMeasurements > 0 ? totalDuration / (double) totalMeasurements : 0L,
                        Intervals = timingIntervals
                    };

                    result.Add(endpointTimings);
                }
            }

            return result.ToArray();
        }

        static DateTime[] GenerateIntervalIds(DateTime now, int numberOfPastIntervals)
        {
            var intervals = new DateTime[numberOfPastIntervals];

            intervals[0] = IntervalId(now);

            for (var i = 1; i < numberOfPastIntervals; i++)
            {
                intervals[i] = intervals[i - 1].Subtract(TimeSpan.FromSeconds(IntervalSizeInSec));
            }

            return intervals;
        }

        static DateTime IntervalId(DateTime date)
        {
            var interval = 0;

            while (interval + IntervalSizeInSec < date.Second)
            {
                interval += IntervalSizeInSec;
            }

            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, interval, date.Kind);
        }

        struct MeasurementInterval
        {
            public long TotalMeasurements;
            public long TotalTime;

            public MeasurementInterval(int totalMeasurements, long totalTime) : this()
            {
                TotalMeasurements = totalMeasurements;
                TotalTime = totalTime;
            }
        }

        public class EndpointTimings
        {
            public string EndpointName { get; set; }
            public double Average { get; set; }

            public TimingInterval[] Intervals { get; set; }
        }

        public class TimingInterval
        {
            public DateTime IntervalStart { get; set; }
            public double Average { get; set; }
        }

        /// Number of 15s intervals in 5 minutes
        static int MaxHistoricalIntervals = 4 * 5;

        static int IntervalSizeInSec = 15;
    }
}