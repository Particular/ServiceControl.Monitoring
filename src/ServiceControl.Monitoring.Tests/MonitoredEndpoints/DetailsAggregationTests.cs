namespace ServiceControl.Monitoring.Tests.MonitoredEndpoints
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class DetailsAggregationTests : AggregationTests
    {
        [Test]
        public void Aggregator_returns_physical_instances_filtered_by_logical_endpoint_name()
        {
            var message = BuildMessage(new Dictionary<DateTime, long>
            {
                {now, 1L}
            });

            processingTimeStore.Store(new EndpointInstanceId("Endpoint1", "Instance1"), message.Entries);
            processingTimeStore.Store(new EndpointInstanceId("Endpoint2", "Instance2"), message.Entries);

            var aggregation = aggregator.AggregateDataForLogicalEndpoint("Endpoint1").ToList();

            Assert.AreEqual(1, aggregation.Count);
            Assert.AreEqual("Instance1", aggregation[0].Id);
        }

        [Test]
        public void Aggregator_returns_all_physical_instances_for_logical_endpoint_name()
        {
            var message = BuildMessage(new Dictionary<DateTime, long>
            {
                {now, 1L}
            });

            processingTimeStore.Store(new EndpointInstanceId("Endpoint1", "Instance1"), message.Entries);
            processingTimeStore.Store(new EndpointInstanceId("Endpoint1", "Instance2"), message.Entries);
            processingTimeStore.Store(new EndpointInstanceId("Endpoint1", "Instance3"), message.Entries);

            var aggregation = aggregator.AggregateDataForLogicalEndpoint("Endpoint1").ToList();

            Assert.AreEqual(3, aggregation.Count);
            CollectionAssert.AreEquivalent(aggregation.Select(i => i.Id), new[] { "Instance1", "Instance2", "Instance3" });
        }
    }
}