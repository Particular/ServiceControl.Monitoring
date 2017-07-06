namespace ServiceControl.Monitoring.Processing.RawData
{
    using System.Collections.Concurrent;
    using NServiceBus.Metrics;

    public class DurationsDataStore
    {
        public ConcurrentDictionary<string, LongValueOccurrences> CriticalTimes = new ConcurrentDictionary<string, LongValueOccurrences>();

        public ConcurrentDictionary<string, LongValueOccurrences> ProcessingTimes = new ConcurrentDictionary<string, LongValueOccurrences>();

        public void RecordProcessingTime(string endpointName, LongValueOccurrences message)
        {
            throw new System.NotImplementedException();
        }

        public void RecordCriticalTime(string endpointName, LongValueOccurrences message)
        {
            throw new System.NotImplementedException();
        }
    }
}