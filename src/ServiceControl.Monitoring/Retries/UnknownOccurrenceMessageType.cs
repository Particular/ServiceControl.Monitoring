namespace ServiceControl.Monitoring
{
    using System;
    public class UnknownOccurrenceMessageType : Exception
    {
        public UnknownOccurrenceMessageType(string messageType)
            : base($"Unknown Occurrence type: {messageType}")
        {
        }
    }
}