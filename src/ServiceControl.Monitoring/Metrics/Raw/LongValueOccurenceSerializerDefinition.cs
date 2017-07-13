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
        public void Serialize(object message, Stream stream)
        {
            throw new NotImplementedException();
        }

        public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
        {
            var message = new LongValueOccurrences();

            ToLongValueOccurrences(message, stream);

            return new object[]
            {
                message
            };
        }

        static void ToLongValueOccurrences(LongValueOccurrences message, Stream stream)
        {
            var reader = new BinaryReader(stream);

            message.Version = reader.ReadInt64();
            message.BaseTicks = reader.ReadInt64();

            var count = reader.ReadInt32();

            message.Ticks = new int[count];
            message.Values = new long[count];

            for (var i = 0; i < count; i++)
            {
                message.Ticks[i] = reader.ReadInt32();
                message.Values[i] = reader.ReadInt64();
            }
        }

        public string ContentType { get; } = "LongValueOccurrence";
    }
}