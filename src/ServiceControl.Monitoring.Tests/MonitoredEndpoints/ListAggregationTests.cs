namespace ServiceControl.Monitoring.Tests.MonitoredEndpoints
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class ListAggregationTests : AggregationTests
    {
        [Test]
        public void Aggregator_returns_stored_data_for_logical_endpoint()
        {
            var message = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-9), 0L}
            });

            processingTimeStore.Store(new EndpointInstanceId("Endpoint1", "Endpoint1@machine"), message.Entries);

            var aggregation = aggregator.AggregateIntoLogicalEndpoints().ToList();

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

            processingTimeStore.Store(new EndpointInstanceId("Endpoint1", "Endpoint1@machine"), firstMessage.Entries);
            processingTimeStore.Store(new EndpointInstanceId("Endpoint2", "Endpoint2@machine"), secondMessage.Entries);

            var aggregation = aggregator.AggregateIntoLogicalEndpoints().ToList();

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

            processingTimeStore.Store(new EndpointInstanceId("Endpoint1", "Endpoint1@machine"), firstMessage.Entries);
            processingTimeStore.Store(new EndpointInstanceId("Endpoint1", "Endpoint1@machine2"), secondMessage.Entries);

            var aggregation = aggregator.AggregateIntoLogicalEndpoints().ToList();

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

            processingTimeStore.Store(new EndpointInstanceId("Endpoint1", "Endpoint1@machine"), message.Entries);

            var aggregation = aggregator.AggregateIntoLogicalEndpoints().ToList();

            Assert.AreEqual(1, aggregation.Count);
            Assert.AreEqual("Endpoint1@machine", aggregation[0].EndpointInstanceIds[0]);
        }
    }
}