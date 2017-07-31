namespace ServiceControl.Monitoring.Messaging
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

    class DurationRawDataSerializer : RawMessageSerializer<LongValueOccurrences>, IMessageSerializer
    {
        public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
        {
            return DeserializeRawMessage(stream);
        }

        public string ContentType { get; } = "LongValueOccurrence";

        protected override bool Store(long timestamp, BinaryReader reader, LongValueOccurrences message)
        {
            var value = reader.ReadInt64();

            return message.TryRecord(timestamp, value);
        }
    }
}