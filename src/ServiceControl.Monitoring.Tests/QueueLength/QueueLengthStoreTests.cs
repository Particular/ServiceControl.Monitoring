namespace ServiceControl.Monitoring.Tests.QueueLength
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Monitoring.Infrastructure;
    using Monitoring.QueueLength;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class QueueLengthStoreTests
    {
        [Test]
        public void It_extracts_sent_sequence()
        {
            var message = BuildMessage(
                sendCounters: new[]
                {
                    new Counter("seq-1", 2),
                    new Counter("seq-2", 1),
                });
            
            var calculator = new FakeCalculator();
            var consumer = new QueueLengthStore(calculator);

            consumer.Store(EmptyEndpointInstanceId(), message);

            Assert.AreEqual(2, calculator.SentSequences["seq-1"]);
            Assert.AreEqual(1, calculator.SentSequences["seq-2"]);
        }

        [Test]
        public void It_ignores_counters_of_different_type()
        {
            var message = BuildMessage(
                sendCounters: new[]
                {
                    new Counter("seq", 10, type: "someType"),
                });

            var calculator = new FakeCalculator();
            var consumer = new QueueLengthStore(calculator);

            consumer.Store(EmptyEndpointInstanceId(), message);

            Assert.IsFalse(calculator.SentSequences.ContainsKey("someKey"));
        }

        [Test]
        public void It_extracts_received_sequence()
        {
            var message = BuildMessage(
                receiveGauges: new[]
                {
                    new Gauge(42, "fc3b1c43-7964-4a75-81e1-3260b85d6065", "ReceivingMessage.Receiver@SIMON-MAC"),
                });

            var calculator = new FakeCalculator();
            var consumer = new QueueLengthStore(calculator);
            consumer.Store(EmptyEndpointInstanceId(), message);

            Assert.AreEqual(42, calculator.ReceivedSequences["fc3b1c43-7964-4a75-81e1-3260b85d6065"]);
            Assert.AreEqual("ReceivingMessage.Receiver@SIMON-MAC", calculator.Queues["fc3b1c43-7964-4a75-81e1-3260b85d6065"]);
        }

        [Test]
        public void It_ignores_gauges_of_different_type()
        {
            var report = BuildMessage(
                receiveGauges: new[]
                {
                    new Gauge(42, "someKey", type: "someType"),
                });
            
            var calculator = new FakeCalculator();
            var consumer = new QueueLengthStore(calculator);

            consumer.Store(EmptyEndpointInstanceId(), report);

            Assert.IsFalse(calculator.ReceivedSequences.ContainsKey("someKey"));
            Assert.IsFalse(calculator.Queues.ContainsKey("someKey"));
        }

        
        [Test]
        public void It_calculates_endpoint_queue_length_as_sum_of_all_input_queue_lengths()
        {
            var receiverReport = BuildMessage(
                receiveGauges: new[]
                {
                    new Gauge(42, "seq-1", "ReceiverQueue-1"),
                    new Gauge(11, "seq-2", "ReceiverQueue-2")
                });

            var senderAReport = BuildMessage(
                sendCounters: new[]
                {
                    new Counter("seq-1", 47)
                }
            );

            var senderBReport = BuildMessage(
                sendCounters: new[]
                {
                    new Counter("seq-2", 15),
                });

            var consumer = new QueueLengthStore(new QueueLengthCalculator());

            consumer.Store(new EndpointInstanceId("SenderA", string.Empty), senderAReport);
            consumer.Store(new EndpointInstanceId("Receiver", string.Empty), receiverReport);
            consumer.Store(new EndpointInstanceId("SenderB", string.Empty), senderBReport);

            var now = DateTime.UtcNow;
            consumer.SnapshotCurrentQueueLengthEstimations(now);

            var lengths = consumer.GetIntervals(HistoryPeriod.FromMinutes(5), now);

            Assert.AreEqual(1, lengths.Length);
            Assert.AreEqual(9, lengths[0].Intervals.First().TotalValue);
        }

        
        [Test]
        public void If_there_is_only_send_sequence_no_value_snapshot_is_not_taken()
        {
            var report = BuildMessage(
                sendCounters: new[]
                {
                    new Counter("seq-1", 1)
                });

            var consumer = new QueueLengthStore(new QueueLengthCalculator());

            var instanceId = EmptyEndpointInstanceId();
            consumer.Store(instanceId, report);

            var now = DateTime.UtcNow;
            consumer.SnapshotCurrentQueueLengthEstimations(now);

            var lengths = consumer.GetIntervals(HistoryPeriod.FromMinutes(5), now);

            Assert.AreEqual(0, lengths.Length);
        }

        
        [Test]
        public void If_there_is_only_receive_sequence_no_value_snapshot_is_taken()
        {
            var report = BuildMessage(
                receiveGauges: new[]
                {
                    new Gauge(1, "seq"),
                });

            var consumer = new QueueLengthStore(new QueueLengthCalculator());

            var instanceId = EmptyEndpointInstanceId();
            consumer.Store(instanceId, report);

            var now = DateTime.UtcNow;
            consumer.SnapshotCurrentQueueLengthEstimations(now);

            var lengths = consumer.GetIntervals(HistoryPeriod.FromMinutes(5), now);

            Assert.AreEqual(0, lengths.Length);
        }

        
        [Test]
        public void If_send_report_is_received_value_snapshot_is_taken_for_endpoints_with_matching_receive_sequences()
        {
            var receiverAReport = BuildMessage(receiveGauges: new[]
            {
                new Gauge(1, "seq-A")
            });

            var receiverBReport = BuildMessage(receiveGauges: new[]
            {
                new Gauge(1, "seq-B"),
            });

            var sendReport = BuildMessage(sendCounters: new[]
            {
                new Counter("seq-A", 3)
            });

            var consumer = new QueueLengthStore(new QueueLengthCalculator());

            consumer.Store(new EndpointInstanceId("ReceiverA", string.Empty), receiverAReport);
            consumer.Store(new EndpointInstanceId("ReceiverB", string.Empty), receiverBReport);
            consumer.Store(new EndpointInstanceId("Sender", string.Empty), sendReport);

            var now = DateTime.UtcNow;
            consumer.SnapshotCurrentQueueLengthEstimations(now);

            var lengths = consumer.GetIntervals(HistoryPeriod.FromMinutes(5), now);

            Assert.AreEqual(1, lengths.Length);
            Assert.AreEqual(3 - 1, lengths[0].Intervals.First().TotalValue);

        }

        static JObject BuildMessage(Counter[] sendCounters = null, Gauge[] receiveGauges = null)
        {
            sendCounters = sendCounters ?? new Counter[0];
            receiveGauges = receiveGauges ?? new Gauge[0];

            var counters = string.Join(",", sendCounters.Select(c => $@"{{
                    ""Name"": ""Sent sequence for {c.SequenceKey}"",
                    ""Count"": {c.Value},
                    ""Unit"": ""Sequence"",
                    ""Tags"": [""key:{c.SequenceKey}"",
                    ""type:{c.Type}""]
                }}"));

            var gauges = string.Join(",", receiveGauges.Select(g => $@"{{
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

        static EndpointInstanceId EmptyEndpointInstanceId()
        {
            return new EndpointInstanceId(String.Empty, string.Empty);
        }

        class Counter
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

        class Gauge
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

        class FakeCalculator : IQueueLengthCalculator
        {
            public void UpdateReceivedSequence(VirtualQueueId virtualQueueId, double value)
            {
                ReceivedSequences[virtualQueueId.SessionKey] = value;
                Queues[virtualQueueId.SessionKey] = virtualQueueId.QueueName;

            }

            public void UpdateSentSequence(string key, double value)
            {
                SentSequences[key] = value;
            }

            public Dictionary<VirtualQueueId, double> GetQueueLengths()
            {
                return new Dictionary<VirtualQueueId, double>();
            }

            public readonly Dictionary<string, double> SentSequences = new Dictionary<string, double>();
            public readonly Dictionary<string, double> ReceivedSequences = new Dictionary<string, double>();
            public readonly Dictionary<string, string> Queues = new Dictionary<string, string>();
        }
    }
}