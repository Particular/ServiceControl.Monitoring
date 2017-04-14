namespace ServiceControl.Monitoring
{
    using System;
    using System.Collections.Generic;
    using NServiceBus;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Metrics;

    public static class Extensions
    {
        public static EndpointConfiguration AddActionOnMetricsReceived(this EndpointConfiguration config, Action<MetricReportWithHeaders> action)
        {
            config.GetSettings().GetOrCreate<List<Action<MetricReportWithHeaders>>>().Add(action);
            return config;
        }
    }
}