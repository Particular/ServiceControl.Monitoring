namespace ServiceControl.Transports.AmazonSQS
{
    using System;
    using System.Linq;
    using System.Text;
    using Amazon.S3;
    using Amazon.SQS;
    using NServiceBus.Settings;

    static class QueueNameHelper
    {
        // copied from https://github.com/Particular/NServiceBus.AmazonSQS/blob/402230de0989ab333124c124cdfdeec14b642263/src/NServiceBus.AmazonSQS/QueueNameHelper.cs
        public static string GetSqsQueueName(string queue, TransportConfiguration transportConfiguration)
        {
            if (string.IsNullOrWhiteSpace(queue))
            {
                throw new ArgumentNullException(nameof(queue));
            }

            var s = transportConfiguration.QueueNamePrefix + queue;

            if (transportConfiguration.PreTruncateQueueNames && s.Length > 80)
            {
                var charsToTake = 80 - transportConfiguration.QueueNamePrefix.Length;
                s = transportConfiguration.QueueNamePrefix +
                    new string(s.Reverse().Take(charsToTake).Reverse().ToArray());
            }

            if (s.Length > 80)
            {
                throw new Exception($"Address {queue} with configured prefix {transportConfiguration.QueueNamePrefix} is longer than 80 characters and therefore cannot be used to create an SQS queue. Use a shorter queue name.");
            }

            var skipCharacters = s.EndsWith(".fifo") ? 5 : 0;
            var queueNameBuilder = new StringBuilder(s);

            // SQS queue names can only have alphanumeric characters, hyphens and underscores.
            // Any other characters will be replaced with a hyphen.
            for (var i = 0; i < queueNameBuilder.Length - skipCharacters; ++i)
            {
                var c = queueNameBuilder[i];
                if (!char.IsLetterOrDigit(c)
                    && c != '-'
                    && c != '_')
                {
                    queueNameBuilder[i] = '-';
                }
            }

            return queueNameBuilder.ToString();
        }
    }

    class TransportConfiguration
    {
        public TransportConfiguration(ReadOnlySettings settings)
        {
            // Accessing the settings bag during runtime means a lot of boxing and unboxing,
            // all properties of this class are lazy initialized once they are accessed
            this.settings = settings;
        }

        public Func<IAmazonSQS> SqsClientFactory
        {
            get
            {
                if (sqsClientFactory == null)
                {
                    sqsClientFactory = settings.GetOrDefault<Func<IAmazonSQS>>(SettingsKeys.SqsClientFactory) ?? (() => new AmazonSQSClient());
                }
                return sqsClientFactory;
            }
        }

        public Func<IAmazonS3> S3ClientFactory
        {
            get
            {
                if (s3ClientFactory == null)
                {
                    s3ClientFactory = settings.GetOrDefault<Func<IAmazonS3>>(SettingsKeys.S3ClientFactory) ?? (() => new AmazonS3Client());
                }
                return s3ClientFactory;
            }
        }

        public string QueueNamePrefix
        {
            get
            {
                if (queueNamePrefix == null)
                {
                    queueNamePrefix = settings.GetOrDefault<string>(SettingsKeys.QueueNamePrefix);
                }
                return queueNamePrefix;
            }
        }

        public bool PreTruncateQueueNames
        {
            get
            {
                if (!preTruncateQueueNames.HasValue)
                {
                    preTruncateQueueNames = settings.GetOrDefault<bool>(SettingsKeys.PreTruncateQueueNames);
                }
                return preTruncateQueueNames.Value;
            }
        }


        ReadOnlySettings settings;
        string queueNamePrefix;
        bool? preTruncateQueueNames;
        Func<IAmazonS3> s3ClientFactory;
        Func<IAmazonSQS> sqsClientFactory;

        static class SettingsKeys
        {
            const string Prefix = "NServiceBus.AmazonSQS.";
            public const string SqsClientFactory = Prefix + nameof(SqsClientFactory);
            public const string S3ClientFactory = Prefix + nameof(S3ClientFactory);
            public const string QueueNamePrefix = Prefix + nameof(QueueNamePrefix);
            public const string PreTruncateQueueNames = Prefix + nameof(PreTruncateQueueNames);
        }
    }
}