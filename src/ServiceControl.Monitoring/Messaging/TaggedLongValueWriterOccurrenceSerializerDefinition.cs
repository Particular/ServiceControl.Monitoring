namespace ServiceControl.Monitoring.Messaging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using NServiceBus.MessageInterfaces;
    using NServiceBus.Serialization;
    using NServiceBus.Settings;

    class TaggedLongValueWriterOccurrenceSerializerDefinition : SerializationDefinition
    {
        public override Func<IMessageMapper, IMessageSerializer> Configure(ReadOnlySettings settings)
        {
            return mapper => new TaggedLongValueSerializer();
        }
    }

    class TaggedLongValueSerializer : IMessageSerializer
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

                var tagKeyToValue = new Dictionary<int, string>();
                var tagKeyToMessage = new Dictionary<int, TaggedLongValueOccurrence>();

                var tagDecoder = new UTF8Encoding(false);

                var tagCount = reader.ReadInt32();
                var messagePool = RawMessage.Pool<TaggedLongValueOccurrence>.Default;

                for (var i = 0; i < tagCount; i++)
                {
                    var tagKey = reader.ReadInt32();
                    var tagLen = reader.ReadInt32();
                    var tagValue = tagDecoder.GetString(reader.ReadBytes(tagLen));

                    tagKeyToValue.Add(tagKey, tagValue);

                    var message = messagePool.Lease();
                    message.TagValue = tagValue;

                    tagKeyToMessage.Add(tagKey, messagePool.Lease());
                }

                var allMessages = new List<TaggedLongValueOccurrence>(tagCount); // usual case

                for (var i = 0; i < count; i++)
                {
                    var ticks = reader.ReadInt32();
                    var timestamp = baseTicks + ticks;
                    var tagKey = reader.ReadInt32();
                    var value = reader.ReadInt64();

                    var message = tagKeyToMessage[tagKey];

                    if (message.Length == RawMessage.MaxEntries)
                    {
                        allMessages.Add(message);

                        message = messagePool.Lease();
                        message.TagValue = tagKeyToValue[tagKey];

                        tagKeyToMessage[tagKey] = message;
                    }

                    if (message.TryRecord(timestamp, value) == false)
                    {
                        throw new Exception("The value should have been written to a newly leased message");
                    }
                }

                allMessages.AddRange(tagKeyToMessage.Values);

                return allMessages.ToArray();
            }

            throw new Exception($"The message version number '{version}' cannot be handled properly.");
        }

        public string ContentType { get; } = "TaggedLongValueWriterOccurrence";
    }
}