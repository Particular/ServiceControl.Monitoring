namespace ServiceControl.Monitoring.Tests.QueueLength
{
    using Monitoring.QueueLength;
    using NUnit.Framework;

    public class CalculatorTests
    {
        [Test]
        public void When_only_receiver_reports_Count_should_be_zero()
        {
            var calculator = new QueueLengthCalculator();
            calculator.UpdateReceivedSequence("Key", 9990, "queue");

            Assert.AreEqual(0, calculator.GetQueueLengths()["queue"]);
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
            calculator.UpdateReceivedSequence("Key", 5, "queue");
            calculator.UpdateSentSequence("Key", 10);

            Assert.AreEqual(5, calculator.GetQueueLengths()["queue"]);
        }

        [Test]
        public void When_receiver_and_sender_report_same_value_Count_should_be_zero()
        {
            var calculator = new QueueLengthCalculator();
            calculator.UpdateReceivedSequence("Key", 10, "queue");
            calculator.UpdateSentSequence("Key", 10);

            Assert.AreEqual(0, calculator.GetQueueLengths()["queue"]);
        }

        [Test]
        public void When_reporting_value_twice_Should_use_the_bigger()
        {
            var calculator = new QueueLengthCalculator();
            calculator.UpdateSentSequence("Key", 15);

            calculator.UpdateReceivedSequence("Key", 10, "queue");
            calculator.UpdateReceivedSequence("Key", 1, "queue");

            Assert.AreEqual(5, calculator.GetQueueLengths()["queue"]);
        }

        [Test]
        public void When_sender_is_lagging_Should_not_report_negative_value()
        {
            var calculator = new QueueLengthCalculator();
            calculator.UpdateReceivedSequence("Key", 15, "queue");
            calculator.UpdateSentSequence("Key", 10);

            Assert.AreEqual(0, calculator.GetQueueLengths()["queue"]);
        }

        [Test]
        public void When_there_are_multiple_senders_Should_report_sum()
        {
            var calculator = new QueueLengthCalculator();
            calculator.UpdateSentSequence("S1", 10);
            calculator.UpdateSentSequence("S2", 10);
            calculator.UpdateReceivedSequence("S1", 5, "queue");
            calculator.UpdateReceivedSequence("S2", 5, "queue");

            Assert.AreEqual(10, calculator.GetQueueLengths()["queue"]);
        }
    }
}