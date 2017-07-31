namespace ServiceControl.Monitoring.Tests.MonitoredEndpoints
{
    using System;
    using System.Collections.Generic;
    using Http.Diagrams;
    using Monitoring.Infrastructure;
    using NUnit.Framework;

    [TestFixture]
    public class AggregatorTests
    {
        DateTime now = DateTime.UtcNow;
        static readonly IntervalsStore.TimeInterval[] EmptyIntervals = new IntervalsStore.TimeInterval[0];

        [Test]
        public void Timings_average_is_sum_of_total_values_by_total_measurements()
        {
            var intervals = new List<IntervalsStore.EndpointInstanceIntervals>
            {
              new IntervalsStore.EndpointInstanceIntervals
              {
                  Id = new EndpointInstanceId(string.Empty, string.Empty),
                  TotalMeasurements = 2,
                  TotalValue = 2,
                  Intervals = EmptyIntervals
              },
              new IntervalsStore.EndpointInstanceIntervals
              {
                  Id = new EndpointInstanceId(string.Empty, string.Empty),
                  TotalMeasurements = 4,
                  TotalValue = 1,
                  Intervals = EmptyIntervals
              }
            };

            var values = IntervalsAggregator.AggregateTimings(intervals);

            Assert.AreEqual(0.5d, values.Average);
        }

        [Test]
        public void Timings_intervals_are_merged_by_interval_start()
        {
            var intervals = new List<IntervalsStore.EndpointInstanceIntervals>
            {
                new IntervalsStore.EndpointInstanceIntervals
                {
                    Id = new EndpointInstanceId(string.Empty, string.Empty),
                    Intervals = new []
                    {
                        new IntervalsStore.TimeInterval{IntervalStart = now, TotalMeasurements = 1, TotalValue = 1}
                    }
                },
                new IntervalsStore.EndpointInstanceIntervals
                {
                    Id = new EndpointInstanceId(string.Empty, string.Empty),
                    Intervals = new []
                    {
                        new IntervalsStore.TimeInterval{IntervalStart = now, TotalMeasurements = 2, TotalValue = 2}
                    }
                }
            };

            var values = IntervalsAggregator.AggregateTimings(intervals);

            Assert.AreEqual(1, values.Points.Length);
            Assert.AreEqual(1.0d, values.Points[0]);
        }

        [Test]
        public void Retires_average_is_sum_of_total_values_by_number_of_intervals()
        {
            var intervals = new List<IntervalsStore.EndpointInstanceIntervals>
            {
                new IntervalsStore.EndpointInstanceIntervals
                {
                    Id = new EndpointInstanceId(string.Empty, string.Empty),
                    TotalValue = 2,
                    Intervals = new []
                    {
                        new IntervalsStore.TimeInterval { IntervalStart = now },
                        new IntervalsStore.TimeInterval { IntervalStart = now.AddSeconds(1) }
                    }
                },
                new IntervalsStore.EndpointInstanceIntervals
                {
                    Id = new EndpointInstanceId(string.Empty, string.Empty),
                    TotalValue = 4,
                    Intervals = new []
                    {
                        new IntervalsStore.TimeInterval { IntervalStart = now },
                        new IntervalsStore.TimeInterval { IntervalStart = now.AddSeconds(1) },
                        new IntervalsStore.TimeInterval { IntervalStart = now }
                    }
                }
            };

            var values = IntervalsAggregator.AggregateRetries(intervals);

            Assert.AreEqual(6d / 5d, values.Average);
        }

        [Test]
        public void Retires_intervals_are_merged_by_interval_start()
        {
            var intervals = new List<IntervalsStore.EndpointInstanceIntervals>
            {
                new IntervalsStore.EndpointInstanceIntervals
                {
                    Id = new EndpointInstanceId(string.Empty, string.Empty),
                    Intervals = new []
                    {
                        new IntervalsStore.TimeInterval { IntervalStart = now },
                        new IntervalsStore.TimeInterval { IntervalStart = now.AddSeconds(1) }
                    }
                },
                new IntervalsStore.EndpointInstanceIntervals
                {
                    Id = new EndpointInstanceId(string.Empty, string.Empty),
                    Intervals = new []
                    {
                        new IntervalsStore.TimeInterval { IntervalStart = now },
                        new IntervalsStore.TimeInterval { IntervalStart = now.AddSeconds(2) },
                    }
                }
            };

            var values = IntervalsAggregator.AggregateRetries(intervals);

            Assert.AreEqual(3, values.Points.Length);
        }

        [Test]
        public void Queue_length_average_is_sum_of_total_values_by_number_of_intervals()
        {
            var intervals = new List<IntervalsStore.EndpointInstanceIntervals>
            {
                new IntervalsStore.EndpointInstanceIntervals
                {
                    Id = new EndpointInstanceId(string.Empty, string.Empty),
                    TotalValue = 3,
                    TotalMeasurements = 1,
                    Intervals = EmptyIntervals
                },
                new IntervalsStore.EndpointInstanceIntervals
                {
                    Id = new EndpointInstanceId(string.Empty, string.Empty),
                    TotalValue = 41,
                    TotalMeasurements = 5,
                    Intervals = EmptyIntervals
                }
            };

            var values = IntervalsAggregator.AggregateQueueLength(intervals);

            Assert.AreEqual((3d + 41d) / (1d + 5d), values.Average);
        }

        [Test]
        public void Queue_length_intervals_are_merged_by_interval_start()
        {
            var intervals = new List<IntervalsStore.EndpointInstanceIntervals>
            {
                new IntervalsStore.EndpointInstanceIntervals
                {
                    Id = new EndpointInstanceId(string.Empty, string.Empty),
                    Intervals = new []
                    {
                        new IntervalsStore.TimeInterval { IntervalStart = now },
                        new IntervalsStore.TimeInterval { IntervalStart = now.AddSeconds(2) }
                    }
                },
                new IntervalsStore.EndpointInstanceIntervals
                {
                    Id = new EndpointInstanceId(string.Empty, string.Empty),
                    Intervals = new []
                    {
                        new IntervalsStore.TimeInterval { IntervalStart = now },
                        new IntervalsStore.TimeInterval { IntervalStart = now.AddSeconds(2) },
                    }
                }
            };

            var values = IntervalsAggregator.AggregateQueueLength(intervals);

            Assert.AreEqual(2, values.Points.Length);
        }

        [Test]
        public void Queue_length_overlapping_intervals_are_merged_by_interval_start()
        {
            var intervals = new List<IntervalsStore.EndpointInstanceIntervals>
            {
                new IntervalsStore.EndpointInstanceIntervals
                {
                    Id = new EndpointInstanceId(string.Empty, string.Empty),
                    Intervals = new []
                    {
                        new IntervalsStore.TimeInterval { IntervalStart = now, TotalValue = 3, TotalMeasurements = 4},
                    }
                },
                new IntervalsStore.EndpointInstanceIntervals
                {
                    Id = new EndpointInstanceId(string.Empty, string.Empty),
                    Intervals = new []
                    {
                        new IntervalsStore.TimeInterval { IntervalStart = now, TotalValue = 5, TotalMeasurements = 6},
                    }
                }
            };

            var values = IntervalsAggregator.AggregateQueueLength(intervals);

            Assert.AreEqual(1, values.Points.Length);
            Assert.AreEqual((3d + 5d) / (4d + 6d), values.Points[0]);
        }
    }
}