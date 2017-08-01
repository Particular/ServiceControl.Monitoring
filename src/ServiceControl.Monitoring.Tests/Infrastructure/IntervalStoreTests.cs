namespace ServiceControl.Monitoring.Tests.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Monitoring.Infrastructure;
    using NUnit.Framework;

    [TestFixture]
    public class IntervalStoreTests
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
        public void Returned_number_of_intervals_per_know_endpoint_equals_history_size()
        {
            var entries = EntriesBuilder.Build(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-9), 0L}
            });

            var store = new IntervalsStore(TimeSpan.FromSeconds(10), 33);

            store.Store(endpointInstanceId, entries);

            var timings = store.GetIntervals(now);

            Assert.AreEqual(1, timings.Length);
            Assert.AreEqual(33, timings[0].Intervals.Length);

            // ordering of intervals
            var dateTimes = timings[0].Intervals.Select(i => i.IntervalStart).ToArray();
            var orderedDateTimes = dateTimes.OrderByDescending(d => d).ToArray();

            CollectionAssert.AreEqual(orderedDateTimes, dateTimes);

            // length of intervals
            var intervalLength = dateTimes[0] - dateTimes[1];
            for (var i = 1; i < dateTimes.Length; i++)
            {
                var dateDiff = dateTimes[i-1] - dateTimes[i];
                Assert.AreEqual(intervalLength, dateDiff);
            }
        }

        [Test]
        public void With_single_measurement_global_stats_equals_interval_stats()
        {
            var entries = EntriesBuilder.Build(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-9), 2L}
            });

            var store = AnyStore();

            store.Store(endpointInstanceId, entries);

            var timings = store.GetIntervals(now);

            Assert.AreEqual(1, timings[0].TotalMeasurements);
            Assert.AreEqual(2L, timings[0].TotalValue);
        }

        [Test]
        public void Intervals_older_than_history_size_are_discarded()
        {
            var intervalSize = TimeSpan.FromSeconds(10);
            var numberOfIntervals = 100;
            var historySize = TimeSpan.FromTicks(intervalSize.Ticks * numberOfIntervals);

            var entries = EntriesBuilder.Build(new Dictionary<DateTime, long>
            {
                {now.Subtract(historySize), 3L}
            });
            
            var store = new IntervalsStore(intervalSize, numberOfIntervals);

            store.Store(endpointInstanceId, entries);

            var timings = store.GetIntervals(now);

            Assert.IsTrue(timings[0].Intervals.All(i => i.TotalMeasurements == 0));
        }

        [Test]
        public void Intervals_from_the_future_are_stored()
        {
            var entries = EntriesBuilder.Build(new Dictionary<DateTime, long>
            {
                {now.AddMinutes(5), 1L}
            });

            var store = AnyStore();

            store.Store(endpointInstanceId, entries);

            var currentTimings = store.GetIntervals(now);

            Assert.IsTrue(currentTimings[0].TotalMeasurements == 0);

            var futureTimings = store.GetIntervals(now.AddMinutes(6));

            Assert.IsTrue(futureTimings[0].TotalMeasurements == 1);
        }

        [Test]
        public void Intervals_can_store_data_from_two_entry_arrays()
        {
            var firstEntries = EntriesBuilder.Build(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-15), 1L},
                {now, 1L}
            });

            var secondEntries = EntriesBuilder.Build(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-30), 1L},
                {now, 3L}
            });

            var store = AnyStore();

            store.Store(endpointInstanceId, firstEntries);
            store.Store(endpointInstanceId, secondEntries);

            var timings = store.GetIntervals(now);

            var nonEmptyIntervals = timings[0].Intervals.Where(i => i.TotalMeasurements > 0).ToArray();

            Assert.AreEqual(3, nonEmptyIntervals.Length);
            Assert.AreEqual(4, timings[0].TotalMeasurements);
            CollectionAssert.AreEqual(new double[] { 4, 1, 1 }, nonEmptyIntervals.Select(i => i.TotalValue));
            CollectionAssert.AreEqual(new double[] { 2, 1, 1 }, nonEmptyIntervals.Select(i => i.TotalMeasurements));
        }

        [Test]
        public void Intervals_are_returned_in_descending_order()
        {
            var entries = EntriesBuilder.Build(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-45), 1L},
                {now.AddSeconds(-30), 1L},
                {now, 1L}
            });

            var store = AnyStore();

            store.Store(endpointInstanceId, entries);

            var timings = store.GetIntervals(now);
            var intervalStarts = timings[0].Intervals.Select(i => i.IntervalStart).ToArray();

            Assert.IsTrue(intervalStarts[0]> intervalStarts[1]);
            Assert.IsTrue(intervalStarts[1] > intervalStarts[2]);
        }

        IntervalsStore AnyStore()
        {
            return new IntervalsStore(TimeSpan.FromSeconds(5), 127);
        }
    }
}