namespace ServiceControl.Transports.AmazonSQS
{
    using System;
    using System.Data.Common;
    using System.Linq;
    using Amazon;
    using NServiceBus;
    using NServiceBus.Settings;
    using NServiceBus.Transport;

    public class ServiceControlSqsTransport : SqsTransport
    {
        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };

            PromoteEnvironmentVariableFromConnectionString(builder, "AccessKeyId", "AWS_ACCESS_KEY_ID");
            PromoteEnvironmentVariableFromConnectionString(builder, "SecretAccessKey", "AWS_SECRET_ACCESS_KEY");
            var region = PromoteEnvironmentVariableFromConnectionString(builder, "Region", "AWS_REGION");

            var awsRegion = RegionEndpoint.EnumerableAllRegions
                .SingleOrDefault(x => x.SystemName == region);

            if (awsRegion == null)
            {
                throw new ArgumentException($"Unknown region: \"{region}\"");
            }

            settings.Set("NServiceBus.AmazonSQS.Region", awsRegion);

            // SQS doesn't support connection strings so pass in null.
            return base.Initialize(settings, null);
        }

        static string PromoteEnvironmentVariableFromConnectionString(DbConnectionStringBuilder builder, string connectionStringKey, string environmentVariableName)
        {
            if (builder.TryGetValue(connectionStringKey, out var value))
            {
                var valueAsString = (string) value;
                Environment.SetEnvironmentVariable(environmentVariableName, valueAsString, EnvironmentVariableTarget.Process);
                return valueAsString;
            }

            throw new ArgumentException($"Missing value for '{connectionStringKey}'", connectionStringKey);
        }
    }
}