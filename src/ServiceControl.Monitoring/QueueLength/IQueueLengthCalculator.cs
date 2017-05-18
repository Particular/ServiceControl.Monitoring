namespace ServiceControl.Monitoring.QueueLength
{
    using System.Collections.Generic;

    interface IQueueLengthCalculator
    {
        void UpdateReceivedSequence(string key, long value, string queue);
        void UpdateSentSequence(string key, long value);
        Dictionary<string, long> GetQueueLengths();
    }
}