﻿namespace ServiceControl.Monitoring.Tests.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using Monitoring.Infrastructure;
    using NUnit.Framework;

    [TestFixture]
    public class VariableHistoryIntervalStoreTests
    {
        DateTime now;
        EndpointInstanceId endpointInstanceId;

        [SetUp]
        public void SetUp()
        {
            now = DateTime.UtcNow;
            endpointInstanceId = new EndpointInstanceId(string.Empty, string.Empty);
        }

        [Test]
        public void Store_updates_all_supported_historical_periods()
        {
            var store = new SomeStore();

            var entries = EntriesBuilder.Build(new Dictionary<DateTime, long>
            {
                {now, 5L}
            });

            store.Store(endpointInstanceId, entries);

            foreach (var period in HistoryPeriod.All)
            {
                var intervals = store.GetIntervals(period, now);

                Assert.AreEqual(1, intervals.Length);
                Assert.AreEqual(5L, intervals[0].TotalValue);
                Assert.AreEqual(1L, intervals[0].TotalMeasurements);
            }
        }
    }

    public class SomeStore : VariableHistoryIntervalStore
    {
    }
}