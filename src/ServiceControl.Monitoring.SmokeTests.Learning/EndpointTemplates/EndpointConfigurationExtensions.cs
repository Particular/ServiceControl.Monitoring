﻿namespace ServiceControl.Monitoring.SmokeTests.Learning.EndpointTemplates
{
    using NServiceBus;
    using NServiceBus.Configuration.AdvanceExtensibility;

    public static class EndpointConfigurationExtensions
    {
        public static TransportExtensions ConfigureTransport(this EndpointConfiguration endpointConfiguration)
        {
            return new TransportExtensions(endpointConfiguration.GetSettings());
        }
    }
}