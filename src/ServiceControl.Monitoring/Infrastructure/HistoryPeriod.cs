namespace ServiceControl.Monitoring.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class HistoryPeriod
    {
        static HistoryPeriod()
        {
            All = new List<HistoryPeriod>()
            {
                new HistoryPeriod(TimeSpan.FromMinutes(5)),
                new HistoryPeriod(TimeSpan.FromMinutes(10)),
                new HistoryPeriod(TimeSpan.FromMinutes(15)),
                new HistoryPeriod(TimeSpan.FromMinutes(30)),
                new HistoryPeriod(TimeSpan.FromMinutes(60))
            };
        }

        HistoryPeriod(TimeSpan value)
        {
            Value = value;
        }

        public TimeSpan Value { get; }

        public static HistoryPeriod FromMinutes(int minutes)
        {
            var period = All.FirstOrDefault(p => p.Value == TimeSpan.FromMinutes(minutes));
            if (period != null)
            {
                return period;
            }

            throw new Exception("Unknown history period.");
        }

        protected bool Equals(HistoryPeriod other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((HistoryPeriod) obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }

        public static IReadOnlyCollection<HistoryPeriod> All;
    }
}