namespace ServiceControl.Monitoring.Infrastructure
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading;
    using Messaging;

    public abstract class IntervalsStore
    {
        ConcurrentDictionary<EndpointInstanceId, Measurement> intervals = new ConcurrentDictionary<EndpointInstanceId, Measurement>();

        public void Store(EndpointInstanceId instanceId, RawMessage.Entry[] entries)
        {
            var measurement = intervals.GetOrAdd(instanceId, _ => new Measurement());

            measurement.Report(entries);
        }

        public EndpointInstanceIntervals[] GetIntervals(DateTime now)
        {
            var result = new List<EndpointInstanceIntervals>();

            foreach (var interval in intervals)
            {
                var instanceId = interval.Key;
                var measurement = interval.Value;

                var item = new EndpointInstanceIntervals
                {
                    Id = instanceId,
                    Intervals = new TimeInterval[NumberOfHistoricalIntervals]
                };

                measurement.ReportTimeIntervals(now, item);
                result.Add(item);
            }

            return result.ToArray();
        }

        class Measurement
        {
            const int Size = NumberOfHistoricalIntervals*2;
            ReaderWriterLockSlim rwl = new ReaderWriterLockSlim();
            MeasurementInterval[] intervals = new MeasurementInterval[Size];

            // ReSharper disable once SuggestBaseTypeForParameter
            public void ReportTimeIntervals(DateTime now, EndpointInstanceIntervals item)
            {
                var epoch = GetEpoch(now.Ticks);
                var intervalsToFill = item.Intervals;
                var numberOfIntervalsToFill = intervalsToFill.Length;

                var totalDuration = 0L;
                var totalMeasurements = 0;

                rwl.EnterReadLock();
                try
                {
                    for (var i = 0; i < numberOfIntervalsToFill ; i++)
                    {
                        var epochIndex = epoch % Size;
                        var interval = intervals[epochIndex];

                        intervalsToFill[i] = new TimeInterval
                        {
                            IntervalStart = GetDateTime(epoch),
                        };

                        // the interval might contain data from the right epoch, or epochs before that have the same index
                        // we calculate data only if that's the right epoch
                        if (interval.Epoch == epoch)
                        {
                            intervalsToFill[i].TotalValue = interval.TotalTime;
                            intervalsToFill[i].TotalMeasurements = interval.TotalMeasurements;

                            totalDuration += interval.TotalTime;
                            totalMeasurements += interval.TotalMeasurements;
                        }

                        epoch -= 1;
                    }
                }
                finally
                {
                    rwl.ExitReadLock();
                }

                item.TotalValue = totalDuration;
                item.TotalMeasurements = totalMeasurements;
            }

            public void Report(RawMessage.Entry[] entries)
            {
                rwl.EnterWriteLock();
                try
                {
                    for (var i = 0; i < entries.Length; i++)
                    {
                        Report(ref entries[i]);
                    }
                }
                finally
                {
                    rwl.ExitWriteLock();
                }
            }

            void Report(ref RawMessage.Entry entry)
            {
                var epoch = GetEpoch(ref entry);
                var epochIndex = epoch % Size;

                if (intervals[epochIndex].Epoch == epoch)
                {
                    intervals[epochIndex].TotalTime += entry.Value;
                    intervals[epochIndex].TotalMeasurements += 1;
                }
                else
                {
                    // only if epoch is newer than the one written before, overwrite
                    // this ensures that old, out-of-order messages do not flush the existing data
                    if (epoch > intervals[epochIndex].Epoch)
                    {
                        intervals[epochIndex].Epoch = epoch;
                        intervals[epochIndex].TotalTime = entry.Value;
                        intervals[epochIndex].TotalMeasurements = 1;
                    }
                }
            }

            struct MeasurementInterval
            {
                public int TotalMeasurements;
                public long Epoch;
                public long TotalTime;

                public override string ToString()
                {
                    return $"{nameof(TotalMeasurements)}: {TotalMeasurements}, {nameof(Epoch)}: {Epoch}, {nameof(TotalTime)}: {TotalTime}";
                }
            }

            static long GetEpoch(ref RawMessage.Entry entry)
            {
                return GetEpoch(entry.DateTicks);
            }

            static long GetEpoch(long ticks)
            {
                return ticks / IntervalSize.Ticks;
            }

            static DateTime GetDateTime(long epoch)
            {
                return new DateTime(epoch * IntervalSize.Ticks, DateTimeKind.Utc);
            }
        }
        
        public class EndpointInstanceIntervals { 
            public EndpointInstanceId Id { get; set; }
            public TimeInterval[] Intervals { get; set; }
            public long TotalValue { get; set; }
            public long TotalMeasurements { get; set; }
        }

        public class TimeInterval
        {
            public DateTime IntervalStart { get; set; }
            public long TotalValue { get; set; }
            public long TotalMeasurements { get; set; }
        }

        /// Number of 15s intervals in 5 minutes
        internal const int NumberOfHistoricalIntervals = 4 * 5;

        static TimeSpan IntervalSize = TimeSpan.FromSeconds(15);
    }
}