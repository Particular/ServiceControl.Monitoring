namespace ServiceControl.Monitoring.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Metrics.Raw;
    using NUnit.Framework;
    using Timings;

    [TestFixture]
    public class TimingAggregatorTests
    {
        ProcessingTimeStore processingTimeStore;
        CriticalTimeStore criticalTimeStore;
        DateTime now;

        [SetUp]
        public void SetUp()
        {
            processingTimeStore = new ProcessingTimeStore();
            criticalTimeStore = new CriticalTimeStore();
            now = DateTime.UtcNow;
        }

        [Test]
        public void Aggregator_returns_stored_data_for_logical_endpoint()
        {
            var message = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-9), 0L}
            });

            processingTimeStore.Store(new EndpointInstanceId("Endpoint1", "Endpoint1@machine"), message, now);

            var aggregation = TimingsAggregator.AggregateIntoLogicalEndpoints(processingTimeStore, criticalTimeStore).ToList();

            Assert.AreEqual(1, aggregation.Count);
            Assert.AreEqual("Endpoint1", aggregation[0].Name);
        }

        [Test]
        public void Aggregator_returns_two_different_stored_logical_endpoints()
        {
            var firstMessage = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-9), 0L}
            });

            var secondMessage = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-9), 0L}
            });

            processingTimeStore.Store(new EndpointInstanceId("Endpoint1", "Endpoint1@machine"), firstMessage, now);
            processingTimeStore.Store(new EndpointInstanceId("Endpoint2", "Endpoint2@machine"), secondMessage, now);

            var aggregation = TimingsAggregator.AggregateIntoLogicalEndpoints(processingTimeStore, criticalTimeStore).ToList();

            Assert.AreEqual(2, aggregation.Count);
            Assert.IsTrue(aggregation.Any(endpoint => endpoint.Name == "Endpoint1"));
            Assert.IsTrue(aggregation.Any(endpoint => endpoint.Name == "Endpoint2"));
        }

        [Test]
        public void Aggregator_aggregates_two_physical_endpoints_under_same_logical_endpoint()
        {
            var firstMessage = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-9), 0L}
            });

            var secondMessage = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-9), 0L}
            });

            processingTimeStore.Store(new EndpointInstanceId("Endpoint1", "Endpoint1@machine"), firstMessage, now);
            processingTimeStore.Store(new EndpointInstanceId("Endpoint1", "Endpoint1@machine2"), secondMessage, now);

            var aggregation = TimingsAggregator.AggregateIntoLogicalEndpoints(processingTimeStore, criticalTimeStore).ToList();

            Assert.AreEqual(1, aggregation.Count);
            Assert.IsTrue(aggregation.Any(endpoint => endpoint.Name == "Endpoint1"));
        }

        [Test]
        public void Aggregation_preserves_endpoint_physical_names()
        {
            var message = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-9), 0L}
            });

            processingTimeStore.Store(new EndpointInstanceId("Endpoint1", "Endpoint1@machine"), message, now);

            var aggregation = TimingsAggregator.AggregateIntoLogicalEndpoints(processingTimeStore, criticalTimeStore).ToList();

            Assert.AreEqual(1, aggregation.Count);
            Assert.AreEqual("Endpoint1@machine", aggregation[0].EndpointInstanceIds[0]);
        }

        static LongValueOccurrences BuildMessage(Dictionary<DateTime, long> measurements)
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