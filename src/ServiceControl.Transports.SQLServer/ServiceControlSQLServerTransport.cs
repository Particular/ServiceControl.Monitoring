namespace ServiceControl.Transports.SQLServer
{
    using System.Data.Common;
    using NServiceBus;
    using NServiceBus.Settings;
    using NServiceBus.Transport;

    public class ServiceControlSQLServerTransport : SqlServerTransport
    {
        public override TransportInfrastructure Initialize(SettingsHolder settings, string connectionString)
        {
            const string queueSchemaName = "Queue schema";

            var builder = new DbConnectionStringBuilder
            {
                ConnectionString = connectionString
            };

            object customSchema;

            if (builder.TryGetValue(queueSchemaName, out customSchema))
            {
                builder.Remove(queueSchemaName);

                settings.Set("SqlServer.DisableConnectionStringValidation", true);
                settings.Set("SqlServer.SchemaName", customSchema);
            }

            return base.Initialize(settings, builder.ConnectionString);
        }

        
    }
}
