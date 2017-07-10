namespace ServiceControl.Monitoring.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using Processing.RawData.NServiceBus.Metrics;
    using Timings;

    [TestFixture]
    public class TimingsStoreTests
    {
        TimingsStore store;
        DateTime now;

        [SetUp]
        public void SetUp()
        {
            store = new ProcessingTimeStore();
            now = DateTime.UtcNow;
        }

        [Test]
        public void Number_of_intervals_is_constant_per_know_endpoint()
        {
            var message = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-9), 0L}
            });

            store.Store(string.Empty, message, now);

            var timings = store.GetTimings(now);

            Assert.AreEqual(1, timings.Length);
            Assert.AreEqual(TimingsStore.NumberOfHistoricalIntervals, timings[0].Intervals.Length);
        }

        [Test]
        public void With_single_interval_global_average_equals_interval_average()
        {
            var message = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddSeconds(-9), 1L}
            });

            store.Store(string.Empty, message, now);

            var timings = store.GetTimings(now);

            Assert.AreEqual(1L, timings[0].Average);
        }

        [Test]
        public void Intervals_older_than_5_minutes_are_discarded()
        {
            var message = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddMinutes(-5), 3L}
            });

            store.Store(string.Empty, message, now);

            var timings = store.GetTimings(now);

            Assert.IsTrue(timings[0].Intervals.All(i => i.Average == 0));
        }

        [Test]
        public void Intervals_from_the_future_are_stored()
        {
            var message = BuildMessage(new Dictionary<DateTime, long>
            {
                {now.AddMinutes(5), 1L}
            });

            store.Store(string.Empty, message, now);

            var currentTimings = store.GetTimings(now);

            Assert.IsTrue(currentTimings[0].Average == 0);

            var futureTimings = store.GetTimings(now.AddMinutes(6));

            Assert.IsTrue(futureTimings[0].Average == 1);
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

            store.Store(string.Empty, firstMessage, now);
            store.Store(string.Empty, secondMessage, now);

            var timings = store.GetTimings(now);

            var nonEmptyIntervals = timings[0].Intervals.Where(i => i.Average > 0).ToArray();

            Assert.AreEqual(1.5d, timings[0].Average);
            Assert.AreEqual(3d, nonEmptyIntervals.Length);
            CollectionAssert.AreEqual(new double[]{2, 1, 1}, nonEmptyIntervals.Select(i => i.Average));
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

            store.Store(string.Empty, message, now);

            var timings = store.GetTimings(now);
            var intervalStarts = timings[0].Intervals.Select(i => i.IntervalStart).ToArray();

            Assert.IsTrue(intervalStarts[0]> intervalStarts[1]);
            Assert.IsTrue(intervalStarts[1] > intervalStarts[2]);
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