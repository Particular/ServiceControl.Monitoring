namespace ServiceControl.Monitoring.QueueLength
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    class QueueLengthCalculator : IQueueLengthCalculator
    {
        public void UpdateReceivedSequence(string key, long value, string queue)
        {
            receivedSequences.AddOrUpdate(key, _ => value, (_, prev) => Math.Max(prev, value));
            sessionKeyToQueueMapping.GetOrAdd(key, _ => queue);
        }

        public void UpdateSentSequence(string key, long value)
        {
            sentSequences.AddOrUpdate(key, _ => value, (_, prev) => Math.Max(prev, value));
        }

        public Dictionary<string, long> GetQueueLengths()
        {
            var queueLengths = new Dictionary<string, long>();

            foreach (var kvp in sessionKeyToQueueMapping)
            {
                var queueName = kvp.Value;
                long queueLength;
                queueLengths.TryGetValue(queueName, out queueLength);

                long sent;
                sentSequences.TryGetValue(kvp.Key, out sent);
                var received = receivedSequences[kvp.Key];

                queueLength += sent;
                queueLength -= received;

                queueLength = Math.Max(0, queueLength);
                queueLengths[queueName] = queueLength;
            }
            return queueLengths;
        }

        ConcurrentDictionary<string, long> receivedSequences = new ConcurrentDictionary<string, long>();
        ConcurrentDictionary<string, long> sentSequences = new ConcurrentDictionary<string, long>();
        ConcurrentDictionary<string, string> sessionKeyToQueueMapping = new ConcurrentDictionary<string, string>();
    }
}