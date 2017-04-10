namespace ServiceControl.Monitoring
{
    using System;
    using System.Collections.Generic;
    using NServiceBus;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Metrics;

    public static class Extensions
    {
        public static EndpointConfiguration AddActionOnMetricsReceived(this EndpointConfiguration config, Action<MetricReport> action)
        {
            config.GetSettings().GetOrCreate<List<Action<MetricReport>>>().Add(action);
            return config;
        }
    }
}