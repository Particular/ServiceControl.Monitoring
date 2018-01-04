namespace ServiceControl.Monitoring.Infrastructure
{
    using System;

    public interface IProvideBreakdownBy<T>
    {
        IntervalsStore<T>.IntervalsBreakdown[] GetIntervals(HistoryPeriod period, DateTime now);
    }
}