namespace ServiceControl.Monitoring.Http
{
    using Nancy;
    using Timings;

    /// <summary>
    /// Exposes ServiceControl.Monitoring metrics needed for in endpoint overview page.
    /// </summary>
    public class MonitoredEndpointsModule : ApiModule
    {
        /// <summary>
        /// Initializes the metric API module.
        /// </summary>
        public MonitoredEndpointsModule(ProcessingTimeStore processingTimeStore, CriticalTimeStore criticalTimeStore)
        {
            Get["/monitored-endpoints"] = x =>
            {
                var endpointsData = TimingsAggregator.AggregateIntoLogicalEndpoints(processingTimeStore, criticalTimeStore);

                return Negotiate.WithModel(endpointsData);
            };
        }
    }
}