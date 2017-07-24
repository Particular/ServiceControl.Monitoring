namespace ServiceControl.Monitoring.Tests.QueueLength
{
    using System.Linq;
    using Monitoring.QueueLength;
    using NUnit.Framework;

    public class CalculatorTests
    {
        [Test]
        public void When_only_receiver_reports_no_data_for_the_endpoint_should_be_calculated()
        {
            var calculator = new QueueLengthCalculator();
            calculator.UpdateReceivedSequence(VirtualQueueId("Key", "queue"), 9990);

            Assert.AreEqual(0, calculator.GetQueueLengths().Count);
        }

        [Test]
        public void When_only_sender_reports_Count_is_unknown()
        {
            var calculator = new QueueLengthCalculator();
            calculator.UpdateSentSequence("Key", 9990);

            Assert.AreEqual(0, calculator.GetQueueLengths().Count);
        }

        [Test]
        public void When_sent_value_is_higher_than_received_value_Should_report_positive()
        {
            var calculator = new QueueLengthCalculator();
            calculator.UpdateReceivedSequence(VirtualQueueId("Key", "queue"), 5);
            calculator.UpdateSentSequence("Key", 10);

            Assert.AreEqual(5, calculator.GetQueueLengths().First(vq => vq.Key.QueueName == "queue").Value);
        }

        [Test]
        public void When_receiver_and_sender_report_same_value_Count_should_be_zero()
        {
            var calculator = new QueueLengthCalculator();
            calculator.UpdateReceivedSequence(VirtualQueueId("Key", "queue"), 10);
            calculator.UpdateSentSequence("Key", 10);

            Assert.AreEqual(0, calculator.GetQueueLengths().First(vq => vq.Key.QueueName == "queue").Value);
        }

        [Test]
        public void When_reporting_value_twice_Should_use_the_bigger()
        {
            var calculator = new QueueLengthCalculator();
            calculator.UpdateSentSequence("Key", 15);

            calculator.UpdateReceivedSequence(VirtualQueueId("Key", "queue"), 10);
            calculator.UpdateReceivedSequence(VirtualQueueId("Key", "queue"), 1);

            Assert.AreEqual(5, calculator.GetQueueLengths().First(vq => vq.Key.QueueName == "queue").Value);
        }

        [Test]
        public void When_sender_is_lagging_Should_not_report_negative_value()
        {
            var calculator = new QueueLengthCalculator();
            calculator.UpdateReceivedSequence(VirtualQueueId("Key", "queue"), 15);
            calculator.UpdateSentSequence("Key", 10);

            Assert.AreEqual(0, calculator.GetQueueLengths().First(vq => vq.Key.QueueName == "queue").Value);
        }

        [Test]
        public void When_there_are_multiple_senders_Should_report_both()
        {
            var calculator = new QueueLengthCalculator();
            calculator.UpdateSentSequence("S1", 10);
            calculator.UpdateSentSequence("S2", 10);
            calculator.UpdateReceivedSequence(VirtualQueueId("S1", "queue"), 5);
            calculator.UpdateReceivedSequence(VirtualQueueId("S2", "queue"), 5);

            var result = calculator.GetQueueLengths();

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(10, result.Sum(i => i.Value));
        }

        static VirtualQueueId VirtualQueueId(string sessionKey, string queue)
        {
            return new VirtualQueueId
            {
                EndpointName = string.Empty,
                QueueName = queue,
                SessionKey = sessionKey
            };
        }
    }
}