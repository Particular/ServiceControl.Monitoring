namespace ServiceControl.Monitoring.Tests.MonitoredEndpoints
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Metrics.Raw;
    using Monitoring.QueueLength;
    using NUnit.Framework;
    using Timings;

    public class AggregationTests
    {
        protected ProcessingTimeStore processingTimeStore;
        protected CriticalTimeStore criticalTimeStore;
        protected QueueLengthDataStore queueLengthDataStore;
        protected TimingsAggregator aggregator;
        protected DateTime now;

        [SetUp]
        public void SetUp()
        {
            processingTimeStore = new ProcessingTimeStore();
            criticalTimeStore = new CriticalTimeStore();
            queueLengthDataStore = new QueueLengthDataStore(new QueueLengthCalculator());

            aggregator = new TimingsAggregator(processingTimeStore, criticalTimeStore, queueLengthDataStore);
            now = DateTime.UtcNow;
        }

        protected static LongValueOccurrences BuildMessage(Dictionary<DateTime, long> measurements)
        {
            var sortedMeasurements = measurements.OrderBy(kv => kv.Key).ToList();
            var baseDateTime = sortedMeasurements.First().Key;

            var message = new LongValueOccurrences
            {
                Version = 1,
                BaseTicks = baseDateTime.Ticks,
                Ticks = new int[measurements.Count],
                Values = new long[measurements.Count]
            };

            for (var i = 0; i < sortedMeasurements.Count; i++)
            {
                message.Ticks[i] = (int)(sortedMeasurements[i].Key.Ticks - baseDateTime.Ticks);
                message.Values[i] = sortedMeasurements[i].Value;
            }

            return message;
        }
    }
}