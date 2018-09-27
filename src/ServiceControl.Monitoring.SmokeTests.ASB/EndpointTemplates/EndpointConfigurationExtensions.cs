﻿namespace ServiceControl.Monitoring.SmokeTests.ASB.EndpointTemplates
{
    using NServiceBus;
    using NServiceBus.Configuration.AdvancedExtensibility;

    public static class EndpointConfigurationExtensions
    {
        public static TransportExtensions ConfigureTransport(this EndpointConfiguration endpointConfiguration)
        {
            return new TransportExtensions(endpointConfiguration.GetSettings());
        }
    }
}