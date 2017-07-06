namespace ServiceControl.Monitoring.Processing.RawData
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using global::NServiceBus;
    using NServiceBus.Metrics;
    using Snapshot;

    public class RawDataProvider : IConsumeLongValueOccurrences
    {
        public void Consume(IReadOnlyDictionary<string, string> headers, LongValueOccurrences data)
        {
            var messageType = headers[Headers.EnclosedMessageTypes];
            var endpointName = headers.GetOriginatingEndpoint();

            if (messageType.EndsWith("ProcessingTime"))
            {
                ProcessingTimes.AddOrUpdate(endpointName, data, (_, __) => data);
            }

            if (messageType.EndsWith("CriticalTime"))
            {
                CriticalTimes.AddOrUpdate(endpointName, data, (_, __) => data);
            }
        }

        public ConcurrentDictionary<string, LongValueOccurrences> CriticalTimes = new ConcurrentDictionary<string, LongValueOccurrences>();

        public ConcurrentDictionary<string, LongValueOccurrences> ProcessingTimes = new ConcurrentDictionary<string, LongValueOccurrences>();
    }
}