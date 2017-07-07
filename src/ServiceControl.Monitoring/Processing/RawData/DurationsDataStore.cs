namespace ServiceControl.Monitoring.Processing.RawData
{
    using System;
    using System.Collections.Concurrent;
    using NServiceBus.Metrics;

    public class DurationsDataStore
    {
        public ConcurrentDictionary<string, ConcurrentDictionary<DateTime, DurationBucket>> CriticalTimes = 
            new ConcurrentDictionary<string, ConcurrentDictionary<DateTime, DurationBucket>>();

        public ConcurrentDictionary<string, ConcurrentDictionary<DateTime, DurationBucket>> ProcessingTimes =  
            new ConcurrentDictionary<string, ConcurrentDictionary<DateTime, DurationBucket>>();

        public void RecordProcessingTime(string endpointName, LongValueOccurrences message)
        {
            var endpointData = ProcessingTimes.GetOrAdd(endpointName, new ConcurrentDictionary<DateTime, DurationBucket>());

            Store(message, endpointData);
        }

        public void RecordCriticalTime(string endpointName, LongValueOccurrences message)
        {
            var endpointData = CriticalTimes.GetOrAdd(endpointName, new ConcurrentDictionary<DateTime, DurationBucket>());

            Store(message, endpointData);
        }

        void Store(LongValueOccurrences message, ConcurrentDictionary<DateTime, DurationBucket> endpointData)
        {
            for (var i = 0; i < message.Ticks.Length; i++)
            {
                var date = new DateTime(message.BaseTicks + message.Ticks[i]);
                var bucketId = Pad(date);

                endpointData.AddOrUpdate(
                    bucketId,
                    _ => new DurationBucket(1, message.Values[i]),
                    (_, b) =>
                    {
                        b.TotalMeasurements += 1;
                        b.TotalTime += message.Values[i];
                        return b;
                    });
            }
        }

        DateTime Pad(DateTime date)
        {
            var seconds = date.Second < 15 ? 0 :
                          date.Second < 30 ? 15 :
                          date.Second < 45 ? 30 :
                          date.Second < 60 ? 45 : 0;

            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, seconds, date.Kind);
        }

        public struct DurationBucket
        {
            public long TotalMeasurements;
            public long TotalTime;

            public DurationBucket(int totalMeasurements, long totalTime) : this()
            {
                TotalMeasurements = totalMeasurements;
                TotalTime = totalTime;
            }
        }
    }
}