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

            var message = new LongValueOccurrences();
            
            foreach (var kvp in sortedMeasurements)
            {
                Assert.True(message.TryRecord(kvp.Key.Ticks, kvp.Value));
            }

            return message;
        }
    }
}