namespace ServiceControl.Monitoring.Infrastructure
{
    using System;
    using System.Collections.Concurrent;
    using Messaging;

    public abstract class VariableHistoryIntervalStore
    {
        ConcurrentDictionary<HistoryPeriod, IntervalsStore> histories;

        protected VariableHistoryIntervalStore()
        {
            histories = new ConcurrentDictionary<HistoryPeriod, IntervalsStore>();

            foreach (var period in HistoryPeriod.All)
            {
                var intervalSize = TimeSpan.FromTicks(period.Value.Ticks / IntervalsPerStore);

                histories.TryAdd(period, new IntervalsStore(intervalSize, IntervalsPerStore));
            }
        }

        public IntervalsStore.EndpointInstanceIntervals[] GetIntervals(HistoryPeriod period, DateTime now)
        {
            IntervalsStore store;

            if (histories.TryGetValue(period, out store))
            {
                return store.GetIntervals(now);
            }

            throw new Exception("Unsupported history size.");
        }

        public void Store(EndpointInstanceId instanceId, RawMessage.Entry[] entries)
        {
            foreach (var kvHistory in histories.ToArray())
            {
                kvHistory.Value.Store(instanceId, entries);
            }
        }

        const int IntervalsPerStore = 20;
    }
}