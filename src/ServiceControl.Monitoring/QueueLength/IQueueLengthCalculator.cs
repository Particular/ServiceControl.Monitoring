namespace ServiceControl.Monitoring.QueueLength
{
    using System.Collections.Generic;

    public interface IQueueLengthCalculator
    {
        bool UpdateReceivedSequence(VirtualQueueId virtualQueueId, double value);
        List<VirtualQueueId> UpdateSentSequence(string key, double value);
        Dictionary<VirtualQueueId, double> GetQueueLengths();
    }
}