namespace ServiceControl.Monitoring.Metrics.Raw
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;

    class LongValueOccurrenceSerializerDefinition : SerializationDefinition
    {
        public override Func<IMessageMapper, IMessageSerializer> Configure(ReadOnlySettings settings)
        {
            return mapper => new DurationRawDataSerializer();
        }
    }

    class DurationRawDataSerializer : IMessageSerializer
    {
        static readonly object[] NoMessages = new object[0];

        public void Serialize(object message, Stream stream)
        {
            throw new NotImplementedException();
        }

        public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
        {
            var reader = new BinaryReader(stream);

            var version = reader.ReadInt64();

            if (version == 1)
            {
                var baseTicks = reader.ReadInt64();
                var count = reader.ReadInt32();

                if (count == 0)
                {
                    return NoMessages;
                }

                var messages = new List<object>(1); // usual case

                var msg = LongValueOccurrences.Pool.Default.Lease();
                messages.Add(msg);

                for (var i = 0; i < count; i++)
                {
                    var ticks = reader.ReadInt32();
                    var value = reader.ReadInt64();
                    var date = baseTicks + ticks;

                    if (msg.TryRecord(date, value) == false)
                    {
                        msg = LongValueOccurrences.Pool.Default.Lease();
                        messages.Add(msg);

                        if (msg.TryRecord(date, value) == false)
                        {
                            throw new Exception("The value should have been written to a newly leased message");
                        }
                    }
                }

                return messages.ToArray();
            }

            throw new Exception($"The message version number '{version}' cannot be handled properly.");
        }

        public string ContentType { get; } = "LongValueOccurrence";
    }
}