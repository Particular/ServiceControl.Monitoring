namespace ServiceControl.Monitoring.Tests
{
    using Metrics.Raw;
    using NUnit.Framework;

    public class LongValueOccurrencesPollTests
    {
        [Test]
        public void Message_lifecycle_is_preserved()
        {
            var pool = new LongValueOccurrences.Pool();
            var message = pool.Lease();

            message.TryRecord(3, 4);

            Assert.AreEqual(1, message.Length);
            Assert.AreEqual(3, message.entries[0].DateTicks);
            Assert.AreEqual(4, message.entries[0].Value);
            
            pool.Release(message);
            Assert.AreEqual(0, message.Length);
            Assert.AreEqual(0, message.entries[0].DateTicks);
            Assert.AreEqual(0, message.entries[0].Value);
        }
    }
}