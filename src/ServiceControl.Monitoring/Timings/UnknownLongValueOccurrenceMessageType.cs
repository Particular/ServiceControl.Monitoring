namespace ServiceControl.Monitoring.Timings
{
    using System;
    public class UnknownLongValueOccurrenceMessageType : Exception
    {
        public UnknownLongValueOccurrenceMessageType(string messageType)
            : base($"Unknown LongValueOccurrence type: {messageType}")
        {
        }
    }
}