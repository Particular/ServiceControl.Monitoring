namespace ServiceControl.Monitoring.QueueLength
{
    using System.Collections.Generic;
    using System.Linq;
    using Http;
    using Newtonsoft.Json.Linq;
    using Processing.Snapshot;

    class QueueLengthDataConsumer : ISnapshotDataConsumer, ISnapshotDataProvider
    {
        public QueueLengthDataConsumer(IQueueLengthCalculator calculator)
        {
            this.calculator = calculator;
        }

        public IEnumerable<KeyValuePair<string, JObject>> Current
        {
            get { return calculator.GetQueueLengths().Select(kvp => new KeyValuePair<string, JObject>(kvp.Key, new JObject(new JProperty("Count", kvp.Value)))); }
        }

        string ISnapshotDataProvider.Name { get; } = Name;

        public void Consume(IReadOnlyDictionary<string, string> headers, JObject data)
        {
            var counters = (JArray) data["Counters"] ?? EmptyArray;
            UpdateSends(counters);

            var gauges = (JArray) data["Gauges"] ?? EmptyArray;
            UpdateReceives(gauges);
        }

        void UpdateSends(IEnumerable<JToken> sends)
        {
            foreach (var send in sends)
            {
                var tags = send["Tags"].ToObject<string[]>();
                string type;
                if (!tags.TryGetTagValue("type", out type) || type != "queue-length.sent")
                {
                    continue;
                }
                var key = tags.GetTagValue("key");
                var value = send.Value<long>("Count");

                calculator.UpdateSentSequence(key, value);
            }
        }

        void UpdateReceives(IEnumerable<JToken> receives)
        {
            foreach (var receive in receives)
            {
                var tags = receive["Tags"].ToObject<string[]>();
                string type;
                if (!tags.TryGetTagValue("type", out type) || type != "queue-length.received")
                {
                    continue;
                }
                var queue = tags.GetTagValue("queue");
                var key = tags.GetTagValue("key");
                var value = receive.Value<double>("Value");

                calculator.UpdateReceivedSequence(key, (long) value, queue);
            }
        }

        IQueueLengthCalculator calculator;
        const string Name = "QueueLength";
        static readonly JArray EmptyArray = new JArray();
    }
}