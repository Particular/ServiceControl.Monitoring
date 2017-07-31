namespace ServiceControl.Monitoring.Infrastructure
{
    using System;
    using Messaging;
    using System.Collections.Generic;

    public abstract class VariableHistoryIntervalStore
    {
        Dictionary<HistoryPeriod, IntervalsStore> histories;

        protected VariableHistoryIntervalStore()
        {
            histories = new Dictionary<HistoryPeriod, IntervalsStore>();

            foreach (var period in HistoryPeriod.All)
            {
                var intervalSize = TimeSpan.FromTicks(period.Value.Ticks / IntervalsPerStore);

                histories.Add(period, new IntervalsStore(intervalSize, IntervalsPerStore));
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
            foreach (var kvHistory in histories)
            {
                kvHistory.Value.Store(instanceId, entries);
            }
        }

        const int IntervalsPerStore = 20;
    }
}