namespace ServiceControl.Monitoring.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class HistoryPeriod
    {
        static HistoryPeriod()
        {
            all = new Dictionary<TimeSpan, HistoryPeriod>();

            AddPeriod(TimeSpan.FromMinutes(5));
            AddPeriod(TimeSpan.FromMinutes(10));
            AddPeriod(TimeSpan.FromMinutes(15));
            AddPeriod(TimeSpan.FromMinutes(30));
            AddPeriod(TimeSpan.FromMinutes(60));

            All = all.Values.ToList().AsReadOnly();
        }

        HistoryPeriod(TimeSpan value)
        {
            Value = value;
        }

        public TimeSpan Value { get; }

        static void AddPeriod(TimeSpan value)
        {
            all.Add(value, new HistoryPeriod(value));
        }

        public static HistoryPeriod FromMinutes(int minutes)
        {
            HistoryPeriod period;
            if (all.TryGetValue(TimeSpan.FromMinutes(minutes), out period))
                return period;

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

        static readonly Dictionary<TimeSpan, HistoryPeriod> all;

        public static IReadOnlyCollection<HistoryPeriod> All;
    }
}