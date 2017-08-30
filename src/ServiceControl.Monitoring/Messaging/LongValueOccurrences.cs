namespace ServiceControl.Monitoring.Messaging
{
    public class LongValueOccurrences : RawMessage
    {
        public bool TryRecord(long dateTicks, long value)
        {
            if (IsFull)
            {
                return false;
            }

            Entries[Index].DateTicks = dateTicks;
            Entries[Index].Value = value;

            Index += 1;

            return true;
        }
    }
}