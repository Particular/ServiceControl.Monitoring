namespace ServiceControl.Monitoring.Metrics.Raw
{

    public class Occurrences : RawMessage
    {
        public bool TryRecord(long dateTicks)
        {
            if (Index == MaxEntries)
            {
                return false;
            }

            Entries[Index].DateTicks = dateTicks;
            Entries[Index].Value = 1;

            Index += 1;

            return true;
        }

        public long MinDateTick => Entries[0].DateTicks;
        public long MaxDateTick => Entries[Index].DateTicks;
    }
}