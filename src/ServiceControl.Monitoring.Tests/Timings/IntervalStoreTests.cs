namespace ServiceControl.Monitoring.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Metrics.Raw;
    using NUnit.Framework;
    using Timings;

    [TestFixture]
    public class IntervalStoreTests
    {
        IntervalsStore store;
        DateTime now;
        EndpointInstanceId endpointInstanceId;

        [SetUp]
        public void SetUp()
        {
            store = new ProcessingTimeStore();
            now = DateTime.UtcNow;
            endpointInstanceId = new EndpointInstanceId(string.Empty, string.Empty);
        }

        [Test]
        public void Number_of_intervals_is_constant_per_know_endpoint_and_have_intervals_assigned()
        {
            var message = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-9), 0L}
            });

            store.Store(endpointInstanceId, message.Entries);

            var timings = store.GetIntervals(now);

            Assert.AreEqual(1, timings.Length);
            Assert.AreEqual(IntervalsStore.NumberOfHistoricalIntervals, timings[0].Intervals.Length);

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
        public void With_single_interval_global_stats_equals_interval_stats()
        {
            var message = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-9), 2L}
            });

            store.Store(endpointInstanceId, message.Entries);

            var timings = store.GetIntervals(now);

            Assert.AreEqual(1, timings[0].TotalMeasurements);
            Assert.AreEqual(2L, timings[0].TotalValue);
        }

        [Test]
        public void Intervals_older_than_5_minutes_are_discarded()
        {
            var message = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddMinutes(-5), 3L}
            });

            store.Store(endpointInstanceId, message.Entries);

            var timings = store.GetIntervals(now);

            Assert.IsTrue(timings[0].Intervals.All(i => i.TotalMeasurements == 0));
        }

        [Test]
        public void Intervals_from_the_future_are_stored()
        {
            var message = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddMinutes(5), 1L}
            });

            store.Store(endpointInstanceId, message.Entries);

            var currentTimings = store.GetIntervals(now);

            Assert.IsTrue(currentTimings[0].TotalMeasurements == 0);

            var futureTimings = store.GetIntervals(now.AddMinutes(6));

            Assert.IsTrue(futureTimings[0].TotalMeasurements == 1);
        }

        [Test]
        public void Intervals_can_store_data_from_two_messages()
        {
            var firstMessage = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-15), 1L},
                {now, 1L}
            });

            var secondMessage = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-30), 1L},
                {now, 3L}
            });

            store.Store(endpointInstanceId, firstMessage.Entries);
            store.Store(endpointInstanceId, secondMessage.Entries);

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
            var message = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-45), 1L},
                {now.AddSeconds(-30), 1L},
                {now, 1L}
            });

            store.Store(endpointInstanceId, message.Entries);

            var timings = store.GetIntervals(now);
            var intervalStarts = timings[0].Intervals.Select(i => i.IntervalStart).ToArray();

            Assert.IsTrue(intervalStarts[0]> intervalStarts[1]);
            Assert.IsTrue(intervalStarts[1] > intervalStarts[2]);
        }

        static LongValueOccurrences BuildMessage(Dictionary<DateTime, long> measurements)
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