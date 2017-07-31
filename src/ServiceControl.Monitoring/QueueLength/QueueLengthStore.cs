namespace ServiceControl.Monitoring.QueueLength
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;
    using Messaging;
    using Newtonsoft.Json.Linq;

    public class QueueLengthStore : IntervalsStore
    {
        public QueueLengthStore(IQueueLengthCalculator calculator)
        {
            this.calculator = calculator;
        }

        public void Store(EndpointInstanceId endpointId, JObject data)
        {
            var counters = (JArray) data["Counters"] ?? new JArray();
            UpdateSends(counters);

            var gauges = (JArray) data["Gauges"] ?? new JArray();
            UpdateReceives(endpointId.EndpointName, gauges);
        }

        public void SnapshotCurrentQueueLengthEstimations(DateTime now)
        {
            var virtualQueueLengths = calculator.GetQueueLengths();

            var queueLengths = virtualQueueLengths
                .GroupBy(vq => vq.Key.EndpointName)
                .Select(g => new
                {
                    Id = new EndpointInstanceId(g.Key, string.Empty),
                    Entry = new RawMessage.Entry
                    {
                        DateTicks = now.Ticks,
                        Value = (long)g.Sum(i => i.Value)
                    }
                });

            foreach (var queueLength in queueLengths)
            {
                Store(queueLength.Id, new[]
                {
                    queueLength.Entry
                });
            }
        }

        void UpdateSends(IEnumerable<JToken> sends)
        {
            foreach (var send in sends)
            {
                var tags = send["Tags"].ToObject<string[]>();
                string type;
                if (!tags.TryGetTagValue("type", out type) || type != "queue-length.sent")
                    continue;
                var key = tags.GetTagValue("key");
                var value = send.Value<long>("Count");

                calculator.UpdateSentSequence(key, value);
            }
        }

        void UpdateReceives(string endpointName, IEnumerable<JToken> receives)
        {
            foreach (var receive in receives)
            {
                var tags = receive["Tags"].ToObject<string[]>();
                string type;
                if (!tags.TryGetTagValue("type", out type) || type != "queue-length.received")
                    continue;
                var queue = tags.GetTagValue("queue");
                var key = tags.GetTagValue("key");
                var value = receive.Value<double>("Value");

                var virtualQueueId = new VirtualQueueId
                {
                    EndpointName = endpointName,
                    QueueName = queue,
                    SessionKey = key
                };

                calculator.UpdateReceivedSequence(virtualQueueId, value);
            }
        }

        readonly IQueueLengthCalculator calculator;
    }
}