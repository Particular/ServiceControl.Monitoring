﻿namespace ServiceControl.Monitoring.SmokeTests.RabbitMQ.EndpointTemplates
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.AcceptanceTesting.Customization;
    using NServiceBus.AcceptanceTesting.Support;
    using NServiceBus.Config.ConfigurationSource;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Features;

    public class DefaultServer : IEndpointSetupTemplate
    {
        public DefaultServer()
        {
            typesToInclude = new List<Type>();
        }

        public DefaultServer(List<Type> typesToInclude)
        {
            this.typesToInclude = typesToInclude;
        }

#pragma warning disable CS0618
        public async Task<EndpointConfiguration> GetConfiguration(RunDescriptor runDescriptor, EndpointCustomizationConfiguration endpointConfiguration, IConfigurationSource configSource, Action<EndpointConfiguration> configurationBuilderCustomization)
#pragma warning restore CS0618
        {
            var settings = runDescriptor.Settings;

            var types = endpointConfiguration.GetTypesScopedByTestClass();

            typesToInclude.AddRange(types);

            var configuration = new EndpointConfiguration(endpointConfiguration.EndpointName);

            configuration.TypesToIncludeInScan(typesToInclude);
            configuration.CustomConfigurationSource(configSource);
            configuration.EnableInstallers();
            configuration.UseSerialization<JsonSerializer>();
            configuration.DisableFeature<TimeoutManager>();

            var recoverability = configuration.Recoverability();
            recoverability.Delayed(delayed => delayed.NumberOfRetries(0));
            recoverability.Immediate(immediate => immediate.NumberOfRetries(0));
            configuration.SendFailedMessagesTo("error");

            await configuration.DefineTransport(settings, endpointConfiguration)
                .ConfigureAwait(false);

            configuration.RegisterComponentsAndInheritanceHierarchy(runDescriptor);

            await configuration.DefinePersistence(settings, endpointConfiguration)
                .ConfigureAwait(false);

            configuration.GetSettings().SetDefault("ScaleOut.UseSingleBrokerQueue", true);
            configurationBuilderCustomization(configuration);

            return configuration;
        }

        List<Type> typesToInclude;
    }
}