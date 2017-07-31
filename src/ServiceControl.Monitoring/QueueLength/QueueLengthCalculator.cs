namespace ServiceControl.Monitoring.QueueLength
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    class QueueLengthCalculator : IQueueLengthCalculator
    {
        public void UpdateReceivedSequence(VirtualQueueId virtualQueueId, double value)
        {
            receivedSequences.AddOrUpdate(virtualQueueId, _ => value, (_, previousValue) => Math.Max(previousValue, value));
        }

        public void UpdateSentSequence(string key, double value)
        {
            sentSequences.AddOrUpdate(key, _ => value, (_, previousValue) => Math.Max(previousValue, value));
        }

        public Dictionary<VirtualQueueId, double> GetQueueLengths()
        {
            return receivedSequences.Join(sentSequences,
                r => r.Key.SessionKey,
                s => s.Key,
                (rkv, skv) => new KeyValuePair<VirtualQueueId, double>(rkv.Key, Math.Max(skv.Value - rkv.Value, 0))
            ).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        ConcurrentDictionary<VirtualQueueId, double> receivedSequences = new ConcurrentDictionary<VirtualQueueId, double>();
        ConcurrentDictionary<string, double> sentSequences = new ConcurrentDictionary<string, double>();
    }

    public class VirtualQueueId
    {
        public string QueueName { get; set; }
        public string SessionKey { get; set; }
        public string EndpointName { get; set; }

        protected bool Equals(VirtualQueueId other)
        {
            return string.Equals(QueueName, other.QueueName) && string.Equals(SessionKey, other.SessionKey) && string.Equals(EndpointName, other.EndpointName);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((VirtualQueueId) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (QueueName != null ? QueueName.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (SessionKey != null ? SessionKey.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (EndpointName != null ? EndpointName.GetHashCode() : 0);
                return hashCode;
            }
        }
    }
}