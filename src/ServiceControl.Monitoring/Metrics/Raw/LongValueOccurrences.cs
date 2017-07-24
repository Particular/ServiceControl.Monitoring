namespace ServiceControl.Monitoring.Metrics.Raw
{
    using System;
    using System.Collections.Concurrent;
    using NServiceBus;

    public class LongValueOccurrences : IMessage
    {
        public class Pool
        {
            public static readonly Pool Default = new Pool();
            ConcurrentStack<LongValueOccurrences> pool = new ConcurrentStack<LongValueOccurrences>();

            public LongValueOccurrences Lease()
            {
                LongValueOccurrences value;
                if (pool.TryPop(out value))
                {
                    return value;
                }

                return new LongValueOccurrences();
            }

            public void Release(LongValueOccurrences message)
            {
                message.Clear();
                pool.Push(message);
            }
        }

        int index = InitialIndex;
        public int Length => index;
        public Entry[] entries = new Entry[MaxEntries];
        const int MaxEntries = 512;
        const int InitialIndex = 0;

        public bool TryRecord(long dateTicks, long value)
        {
            if (index == MaxEntries)
            {
                return false;
            }

            entries[index].DateTicks = dateTicks;
            entries[index].Value = value;

            index += 1;

            return true;
        }

        public long MinDateTick => entries[0].DateTicks;
        public long MaxDateTick => entries[index].DateTicks;

        public struct Entry
        {
            public long DateTicks;
            public long Value;
        }

        void Clear()
        {
            index = InitialIndex;
            Array.Clear(entries, 0, MaxEntries);
        }
    }
}