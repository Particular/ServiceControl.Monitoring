namespace ServiceControl.Monitoring.Metrics.Raw
{
    using NServiceBus;

    public class LongValueOccurrences : IMessage
    {
        public long Version;

        public long BaseTicks;

        public int[] Ticks;

        public long[] Values;
    }

    public class ProcessingTime : LongValueOccurrences
    {
    }

    public class CriticalTime : LongValueOccurrences
    {
    }
}