﻿namespace ServiceControl.Monitoring.Tests.MonitoredEndpoints
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
        static readonly IntervalsStore<BreakdownId>.TimeInterval[] EmptyIntervals = new IntervalsStore<BreakdownId>.TimeInterval[0];

        [Test]
        public void Timings_average_is_sum_of_total_values_by_total_measurements()
        {
            var intervals = new List<IntervalsStore<BreakdownId>.IntervalsBreakdown>
            {
              new IntervalsStore<BreakdownId>.IntervalsBreakdown
              {
                  Id = new BreakdownId{Id = 0},
                  TotalMeasurements = 2,
                  TotalValue = 2,
                  Intervals = EmptyIntervals
              },
              new IntervalsStore<BreakdownId>.IntervalsBreakdown
              {
                  Id = new BreakdownId{Id = 0},
                  TotalMeasurements = 4,
                  TotalValue = 1,
                  Intervals = EmptyIntervals
              }
            };

            var values = IntervalsAggregator.AggregateTimings(intervals, HistoryPeriod.FromMinutes(5));

            Assert.AreEqual(0.5d, values.Average);
        }

        [Test]
        public void Timings_intervals_are_merged_by_interval_start()
        {
            var intervals = new List<IntervalsStore<BreakdownId>.IntervalsBreakdown>
            {
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    Intervals = new []
                    {
                        new IntervalsStore<BreakdownId>.TimeInterval{IntervalStart = now, TotalMeasurements = 1, TotalValue = 1}
                    }
                },
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    Intervals = new []
                    {
                        new IntervalsStore<BreakdownId>.TimeInterval{IntervalStart = now, TotalMeasurements = 2, TotalValue = 2}
                    }
                }
            };

            var values = IntervalsAggregator.AggregateTimings(intervals, HistoryPeriod.FromMinutes(5));

            Assert.AreEqual(1, values.Points.Length);
            Assert.AreEqual(1.0d, values.Points[0]);
        }

        [Test]
        public void Retires_average_is_sum_of_total_values_by_number_of_intervals()
        {
            var intervals = new List<IntervalsStore<BreakdownId>.IntervalsBreakdown>
            {
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    TotalValue = 2,
                    Intervals = new []
                    {
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now },
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now.AddSeconds(1) }
                    }
                },
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    TotalValue = 4,
                    Intervals = new []
                    {
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now },
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now.AddSeconds(1) },
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now }
                    }
                }
            };

            var values = IntervalsAggregator.AggregateRetries(intervals, HistoryPeriod.FromMinutes(5));

            Assert.AreEqual(6d / 5d, values.Average);
        }

        [Test]
        public void Retires_intervals_are_merged_by_interval_start()
        {
            var intervals = new List<IntervalsStore<BreakdownId>.IntervalsBreakdown>
            {
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    Intervals = new []
                    {
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now },
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now.AddSeconds(1) }
                    }
                },
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    Intervals = new []
                    {
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now },
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now.AddSeconds(2) },
                    }
                }
            };

            var values = IntervalsAggregator.AggregateRetries(intervals, HistoryPeriod.FromMinutes(5));

            Assert.AreEqual(3, values.Points.Length);
        }

        [Test]
        public void Queue_length_average_is_sum_of_total_values_by_number_of_intervals()
        {
            var intervals = new List<IntervalsStore<BreakdownId>.IntervalsBreakdown>
            {
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    TotalValue = 3,
                    TotalMeasurements = 1,
                    Intervals = EmptyIntervals
                },
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    TotalValue = 41,
                    TotalMeasurements = 5,
                    Intervals = EmptyIntervals
                }
            };

            var values = IntervalsAggregator.AggregateQueueLength(intervals, HistoryPeriod.FromMinutes(5));

            Assert.AreEqual((3d + 41d) / (1d + 5d), values.Average);
        }

        [Test]
        public void Queue_length_intervals_are_merged_by_interval_start()
        {
            var intervals = new List<IntervalsStore<BreakdownId>.IntervalsBreakdown>
            {
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    Intervals = new []
                    {
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now },
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now.AddSeconds(2) }
                    }
                },
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    Intervals = new []
                    {
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now },
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now.AddSeconds(2) },
                    }
                }
            };

            var values = IntervalsAggregator.AggregateQueueLength(intervals, HistoryPeriod.FromMinutes(5));

            Assert.AreEqual(2, values.Points.Length);
        }

        [Test]
        public void Queue_length_overlapping_intervals_are_merged_by_interval_start()
        {
            var intervals = new List<IntervalsStore<BreakdownId>.IntervalsBreakdown>
            {
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    Intervals = new []
                    {
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now, TotalValue = 3, TotalMeasurements = 4},
                    }
                },
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    Intervals = new []
                    {
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now, TotalValue = 5, TotalMeasurements = 6},
                    }
                }
            };

            var values = IntervalsAggregator.AggregateQueueLength(intervals, HistoryPeriod.FromMinutes(5));

            Assert.AreEqual(1, values.Points.Length);
            Assert.AreEqual((3d + 5d) / (4d + 6d), values.Points[0]);
        }

        [Test]
        public void Total_measurements_per_second_are_merged_by_interval_start()
        {
            const long ridiculouslyBigLong1 = 374859734593849583;
            const long ridiculouslyBigLong2 = 898394895890348954;

            var intervals = new List<IntervalsStore<BreakdownId>.IntervalsBreakdown>
            {
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    Intervals = new []
                    {
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now, TotalValue = ridiculouslyBigLong1, TotalMeasurements = 4},
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now.AddSeconds(2), TotalValue = ridiculouslyBigLong2, TotalMeasurements = 5}
                    },
                    TotalMeasurements = 4 + 5
                },
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    Intervals = new []
                    {
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now, TotalValue = ridiculouslyBigLong1, TotalMeasurements = 6},
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now.AddSeconds(2), TotalValue = ridiculouslyBigLong2, TotalMeasurements = 7},
                    },
                    TotalMeasurements = 6 + 7
                }
            };

            var period = HistoryPeriod.FromMinutes(5);
            var seconds = period.IntervalSize.TotalSeconds;
            var values = IntervalsAggregator.AggregateTotalMeasurementsPerSecond(intervals, period);

            Assert.AreEqual((4d + 5d + 6d + 7d) / 4 / seconds, values.Average);
            Assert.AreEqual(2, values.Points.Length);
            Assert.AreEqual((4d + 6d) / seconds, values.Points[0]);
            Assert.AreEqual((5d + 7d) / seconds, values.Points[1]);
        }

        [Test]
        public void Total_measurements_per_second_are_sum_of_total_measurements_by_number_of_intervals_by_seconds()
        {
            const long ridiculouslyBigLong1 = 374859734593849583;
            const long ridiculouslyBigLong2 = 898394895890348954;

            var intervals = new List<IntervalsStore<BreakdownId>.IntervalsBreakdown>
            {
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    TotalValue = ridiculouslyBigLong1,
                    TotalMeasurements = 7,
                    Intervals = new []
                    {
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now },
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now.AddSeconds(1) }
                    }
                },
                new IntervalsStore<BreakdownId>.IntervalsBreakdown
                {
                    Id = new BreakdownId{Id = 0},
                    TotalValue = ridiculouslyBigLong2,
                    TotalMeasurements = 9,
                    Intervals = new []
                    {
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now },
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now.AddSeconds(1) },
                        new IntervalsStore<BreakdownId>.TimeInterval { IntervalStart = now }
                    }
                }
            };

            var period = HistoryPeriod.FromMinutes(5);
            var seconds = period.IntervalSize.TotalSeconds;

            var values = IntervalsAggregator.AggregateTotalMeasurementsPerSecond(intervals, period);

            Assert.AreEqual((7d + 9d) / 5 / seconds, values.Average);
        }

        class BreakdownId
        {
            public int Id { get; set; }

            protected bool Equals(BreakdownId other)
            {
                return Id == other.Id;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((BreakdownId) obj);
            }

            public override int GetHashCode()
            {
                return Id;
            }
        }
    }
}