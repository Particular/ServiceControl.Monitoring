namespace ServiceControl.Monitoring.Tests.QueueLength
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Monitoring.Infrastructure;
    using Monitoring.QueueLength;
    using NUnit.Framework;

    [TestFixture]
    public class QueueLengthStoreTests
    {
        [Test]
        public void It_extracts_sent_sequence()
        {
            var message = MessageBuilder.BuildMessage(
                sendCounters: new[]
                {
                    new MessageBuilder.Counter("seq-1", 2),
                    new MessageBuilder.Counter("seq-2", 1),
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
            var message = MessageBuilder.BuildMessage(
                sendCounters: new[]
                {
                    new MessageBuilder.Counter("seq", 10, type: "someType"),
                });

            var calculator = new FakeCalculator();
            var consumer = new QueueLengthStore(calculator);

            consumer.Store(EmptyEndpointInstanceId(), message);

            Assert.IsFalse(calculator.SentSequences.ContainsKey("someKey"));
        }

        [Test]
        public void It_extracts_received_sequence()
        {
            var message = MessageBuilder.BuildMessage(
                receiveGauges: new[]
                {
                    new MessageBuilder.Gauge(42, "fc3b1c43-7964-4a75-81e1-3260b85d6065", "ReceivingMessage.Receiver@SIMON-MAC"),
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
            var report = MessageBuilder.BuildMessage(
                receiveGauges: new[]
                {
                    new MessageBuilder.Gauge(42, "someKey", type: "someType"),
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
            var receiverReport = MessageBuilder.BuildMessage(
                receiveGauges: new[]
                {
                    new MessageBuilder.Gauge(42, "seq-1", "ReceiverQueue-1"),
                    new MessageBuilder.Gauge(11, "seq-2", "ReceiverQueue-2")
                });

            var senderAReport = MessageBuilder.BuildMessage(
                sendCounters: new[]
                {
                    new MessageBuilder.Counter("seq-1", 47)
                }
            );

            var senderBReport = MessageBuilder.BuildMessage(
                sendCounters: new[]
                {
                    new MessageBuilder.Counter("seq-2", 15),
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
            var report = MessageBuilder.BuildMessage(
                sendCounters: new[]
                {
                    new MessageBuilder.Counter("seq-1", 1)
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
            var report = MessageBuilder.BuildMessage(
                receiveGauges: new[]
                {
                    new MessageBuilder.Gauge(1, "seq"),
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
            var receiverAReport = MessageBuilder.BuildMessage(receiveGauges: new[]
            {
                new MessageBuilder.Gauge(1, "seq-A")
            });

            var receiverBReport = MessageBuilder.BuildMessage(receiveGauges: new[]
            {
                new MessageBuilder.Gauge(1, "seq-B"),
            });

            var sendReport = MessageBuilder.BuildMessage(sendCounters: new[]
            {
                new MessageBuilder.Counter("seq-A", 3)
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

        static EndpointInstanceId EmptyEndpointInstanceId()
        {
            return new EndpointInstanceId(String.Empty, string.Empty);
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