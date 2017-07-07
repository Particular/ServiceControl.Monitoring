namespace ServiceControl.Monitoring.Tests.QueueLength
{
    using System;
    using System.Collections.Generic;
    using Monitoring.QueueLength;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    public class QueueLengthDataConsumerTests
    {
        [Test]
        public void It_extracts_sent_sequence()
        {
            const string json = @"{
    ""Version"": ""2"",
    ""Timestamp"": ""2017-05-11T07:13:28.5918Z"",
    ""Context"": ""SendingMessage.Sender"",
    ""Counters"": [{
        ""Name"": ""Sent sequence for sendingmessage.receiver1-a328a49b-4212-4a34-8e90-726848230c03"",
        ""Count"": 2,
        ""Unit"": ""Sequence"",
        ""Tags"": [""key:sendingmessage.receiver1-a328a49b-4212-4a34-8e90-726848230c03"",
        ""type:queue-length.sent""]
    },
    {
        ""Name"": ""Sent sequence for sendingmessage.receiver2-a328a49b-4212-4a34-8e90-726848230c03"",
        ""Count"": 1,
        ""Unit"": ""Sequence"",
        ""Tags"": [""key:sendingmessage.receiver2-a328a49b-4212-4a34-8e90-726848230c03"",
        ""type:queue-length.sent""]
    }],
    ""Meters"": [],
    ""Timers"": []
}";
            var calculator = new FakeCalculator();
            var consumer = new QueueLengthDataStore(calculator);
            consumer.Store(JObject.Parse(json));

            Assert.AreEqual(1, calculator.SentSequences["sendingmessage.receiver2-a328a49b-4212-4a34-8e90-726848230c03"]);
            Assert.AreEqual(2, calculator.SentSequences["sendingmessage.receiver1-a328a49b-4212-4a34-8e90-726848230c03"]);
        }

        [Test]
        public void It_ignores_counters_of_different_type()
        {
            const string json = @"{
    ""Version"": ""2"",
    ""Timestamp"": ""2017-05-11T07:13:28.5918Z"",
    ""Context"": ""SendingMessage.Sender"",
    ""Counters"": [
    {
        ""Name"": ""Not a queue length counter"",
        ""Count"": 10,
        ""Unit"": ""Sequence"",
        ""Tags"": [""key:someKey"",
        ""type:someType""]
    }],
    ""Meters"": [],
    ""Timers"": []
}";
            var calculator = new FakeCalculator();
            var consumer = new QueueLengthDataStore(calculator);
            consumer.Store(JObject.Parse(json));

            Assert.IsFalse(calculator.SentSequences.ContainsKey("someKey"));
        }

        [Test]
        public void It_extracts_received_sequence()
        {
            const string json = @"{
    ""Version"": ""2"",
    ""Timestamp"": ""2017-05-11T07:41:53.7893Z"",
    ""Context"": ""ReceivingMessage.Receiver"",
    ""Gauges"": [{
        ""Name"": ""Received sequence for fc3b1c43-7964-4a75-81e1-3260b85d6065"",
        ""Value"": 42.00,
        ""Unit"": ""Sequence"",
        ""Tags"": [""key:fc3b1c43-7964-4a75-81e1-3260b85d6065"",
        ""queue:ReceivingMessage.Receiver@SIMON-MAC"",
        ""type:queue-length.received""]
    }],
    ""Meters"": [],
    ""Timers"": []
}";
            var calculator = new FakeCalculator();
            var consumer = new QueueLengthDataStore(calculator);
            consumer.Store(JObject.Parse(json));

            Assert.AreEqual(42, calculator.ReceivedSequences["fc3b1c43-7964-4a75-81e1-3260b85d6065"]);
            Assert.AreEqual("ReceivingMessage.Receiver@SIMON-MAC", calculator.Queues["fc3b1c43-7964-4a75-81e1-3260b85d6065"]);
        }

        [Test]
        public void It_ignores_gauges_of_different_type()
        {
            const string json = @"{
    ""Version"": ""2"",
    ""Timestamp"": ""2017-05-11T07:41:53.7893Z"",
    ""Context"": ""ReceivingMessage.Receiver"",
    ""Gauges"": [{
        ""Name"": ""Received sequence for fc3b1c43-7964-4a75-81e1-3260b85d6065"",
        ""Value"": 42.00,
        ""Unit"": ""Sequence"",
        ""Tags"": [""key:someKey"",
        ""queue:ReceivingMessage.Receiver@SIMON-MAC"",
        ""type:someType""]
    }],
    ""Meters"": [],
    ""Timers"": []
}";
            var calculator = new FakeCalculator();
            var consumer = new QueueLengthDataStore(calculator);
            consumer.Store(JObject.Parse(json));

            Assert.IsFalse(calculator.ReceivedSequences.ContainsKey("someKey"));
            Assert.IsFalse(calculator.Queues.ContainsKey("someKey"));
        }

        class FakeCalculator : IQueueLengthCalculator
        {
            public void UpdateReceivedSequence(string key, long value, string queue)
            {
                ReceivedSequences[key] = value;
                Queues[key] = queue;
            }

            public void UpdateSentSequence(string key, long value)
            {
                SentSequences[key] = value;
            }

            public Dictionary<string, long> GetQueueLengths()
            {
                throw new NotImplementedException();
            }

            public Dictionary<string, long> SentSequences = new Dictionary<string, long>();
            public Dictionary<string, long> ReceivedSequences = new Dictionary<string, long>();
            public Dictionary<string, string> Queues = new Dictionary<string, string>();
        }
    }
}