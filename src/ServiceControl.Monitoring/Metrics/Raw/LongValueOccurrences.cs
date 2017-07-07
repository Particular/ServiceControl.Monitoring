namespace ServiceControl.Monitoring.Processing.RawData.NServiceBus.Metrics
{
    using global::NServiceBus;

    public class LongValueOccurrences : IMessage
    {
        public long Version;

        public long BaseTicks;

        public int[] Ticks;

        public long[] Values;
    }
}

namespace NServiceBus.Metrics
{
    using ServiceControl.Monitoring.Processing.RawData.NServiceBus.Metrics;

    public class ProcessingTime : LongValueOccurrences
    {
    }

    public class CriticalTime : LongValueOccurrences
    {
    }
}