namespace ServiceControl.Monitoring.Tests.MonitoredEndpoints
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;
    using Metrics.Raw;
    using Monitoring.QueueLength;
    using NUnit.Framework;
    using Timings;

    public class AggregationTests
    {
        protected ProcessingTimeStore processingTimeStore;
        protected CriticalTimeStore criticalTimeStore;
        protected RetriesStore retriesStore;
        protected QueueLengthDataStore queueLengthDataStore;
        protected TimingsAggregator aggregator;
        protected EndpointRegistry endpointRegistry;
        protected DateTime now;

        [SetUp]
        public void SetUp()
        {
            processingTimeStore = new ProcessingTimeStore();
            criticalTimeStore = new CriticalTimeStore();
            retriesStore = new RetriesStore();
            queueLengthDataStore = new QueueLengthDataStore(new QueueLengthCalculator());

            var stores = new object[]
            {
                processingTimeStore,
                criticalTimeStore,
                retriesStore,
                queueLengthDataStore
            };

            endpointRegistry = new EndpointRegistry(stores.OfType<IKnowAboutEndpoints>().ToArray());


            aggregator = new TimingsAggregator(endpointRegistry,
                stores.OfType<IProvideEndpointMonitoringData>().ToArray(),
                stores.OfType<IProvideEndpointInstanceMonitoringData>().ToArray()
            );

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