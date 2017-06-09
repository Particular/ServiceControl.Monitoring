namespace ServiceControl.Monitoring.Tests.Diagrams
{
    using System;
    using System.Linq;
    using NUnit.Framework;
    using Raw;

    [TestFixture]
    public class EndpointDataTests
    {
        [Test]
        public void When_recroding_more_processing_times_than_size_the_oldest_are_removed()
        {
            var size = 3;
            var endpointData = new EndpointData(size);

            endpointData.Record(DateTime.Now, 1, 1);
            endpointData.Record(DateTime.Now, 2, 1);
            endpointData.Record(DateTime.Now, 3, 1);
            endpointData.Record(DateTime.Now, 4, 1);

            Assert.IsTrue(endpointData.ProcessingTime.Contains(2));
            Assert.IsTrue(endpointData.ProcessingTime.Contains(3));
            Assert.IsTrue(endpointData.ProcessingTime.Contains(4));
            Assert.IsFalse(endpointData.ProcessingTime.Contains(1));
        }

        [Test]
        public void When_recroding_more_critical_time_than_size_the_oldest_are_removed()
        {
            var size = 2;
            var endpointData = new EndpointData(size);

            var firstDateTime = new DateTime(2000, 1, 1);
            var secondDateTime = new DateTime(2002, 2, 2);

            endpointData.Record(firstDateTime, 1, 2);
            endpointData.Record(secondDateTime, 3, 4);
            var firstIndex = Array.IndexOf(endpointData.Timestamps, firstDateTime);
            var secondIndex = Array.IndexOf(endpointData.Timestamps, secondDateTime);

            Assert.AreEqual(1, endpointData.CriticalTime[firstIndex]);
            Assert.AreEqual(2, endpointData.ProcessingTime[firstIndex]);

            Assert.AreEqual(3, endpointData.CriticalTime[secondIndex]);
            Assert.AreEqual(4, endpointData.ProcessingTime[secondIndex]);
        }
    }
}