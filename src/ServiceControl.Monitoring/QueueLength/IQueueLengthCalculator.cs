namespace ServiceControl.Monitoring.QueueLength
{
    using System.Collections.Generic;

    public interface IQueueLengthCalculator
    {
        void UpdateReceivedSequence(VirtualQueueId virtualQueueId, double value);
        void UpdateSentSequence(string key, double value);
        Dictionary<VirtualQueueId, double> GetQueueLengths();
    }
}