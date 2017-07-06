namespace ServiceControl.Monitoring.Processing.RawData
{
    using System.Collections.Generic;
    using NServiceBus.Metrics;

    public interface IConsumeLongValueOccurrences
    {
        void Consume(IReadOnlyDictionary<string, string> headers, LongValueOccurrences data);
    }
}