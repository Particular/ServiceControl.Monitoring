namespace ServiceControl.Monitoring.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;

    public class OccurrenceSerializerDefinition : SerializationDefinition
    {
        public override Func<IMessageMapper, IMessageSerializer> Configure(ReadOnlySettings settings)
        {
            return mapper => new OccurrenceSerializer();
        }
    }

    public class OccurrenceSerializer : RawMessageSerializer<Occurrences>, IMessageSerializer
    {
        public object[] Deserialize(Stream stream, IList<Type> messageTypes = null)
        {
            return DeserializeRawMessage(stream);
        }

        public string ContentType { get; } = "Occurrence";

        protected override bool Store(long timestamp, BinaryReader reader, Occurrences message)
        {
            return message.TryRecord(timestamp);
        }
    }
}