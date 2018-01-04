using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace ServiceControl.Monitoring.Tests.QueueLength
{
    static class MessageBuilder
    {
        public static JObject BuildMessage(Counter[] sendCounters = null, Gauge[] receiveGauges = null)
        {
            sendCounters = sendCounters ?? new Counter[0];
            receiveGauges = receiveGauges ?? new Gauge[0];

            var counters = String.Join(",", sendCounters.Select(c => $@"{{
                    ""Name"": ""Sent sequence for {c.SequenceKey}"",
                    ""Count"": {c.Value},
                    ""Unit"": ""Sequence"",
                    ""Tags"": [""key:{c.SequenceKey}"",
                    ""type:{c.Type}""]
                }}"));

            var gauges = String.Join(",", receiveGauges.Select(g => $@"{{
                    ""Name"": ""Received sequence for {g.SequenceKey}"",
                    ""Value"": {g.Value},
                    ""Unit"": ""Sequence"",
                    ""Tags"": [""key:{g.SequenceKey}"",
                    ""queue:{g.QueueName}"",
                    ""type:{g.Type}""]
                }}"));

            var json = $@"{{
                    ""Version"": ""2"",
                    ""Timestamp"": ""2017-05-11T07:13:28.5918Z"",
                    ""Context"": ""Whatever"",
                    ""Counters"": [{counters}],
                    ""Gauges"": [{gauges}],
                    ""Meters"": [],
                    ""Timers"": []
                  }}";

            return JObject.Parse(json);
        }

        public class Counter
        {
            public Counter(string sequenceKey, int value, string type = "queue-length.sent")
            {
                Value = value;
                SequenceKey = sequenceKey;
                Type = type;
            }

            public int Value { get; }
            public string SequenceKey { get; }
            public string Type { get; }
        }

        public class Gauge
        {
            public Gauge(int value, string sequenceKey, string queueName = "queue", string type = "queue-length.received")
            {
                Value = value;
                SequenceKey = sequenceKey;
                QueueName = queueName;
                Type = type;
            }

            public int Value { get; }
            public string SequenceKey { get; }
            public string QueueName { get; }
            public string Type { get; }
        }
    }
}