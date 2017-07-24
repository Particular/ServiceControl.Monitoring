namespace ServiceControl.Monitoring.Http
{
    using Nancy;
    using QueueLength;
    using Timings;

    /// <summary>
    /// Exposes ServiceControl.Monitoring metrics needed for in endpoint overview page.
    /// </summary>
    public class MonitoredEndpointsModule : ApiModule
    {
        /// <summary>
        /// Initializes the metric API module.
        /// </summary>
        public MonitoredEndpointsModule(ProcessingTimeStore processingTimeStore, CriticalTimeStore criticalTimeStore, QueueLengthDataStore queueLengthDataStore)
        {
            var timingAggregator = new TimingsAggregator(processingTimeStore, criticalTimeStore, queueLengthDataStore);

            Get["/monitored-endpoints"] = x =>
            {
                var endpointsData = timingAggregator.AggregateIntoLogicalEndpoints();

                return Negotiate.WithModel(endpointsData);
            };

            Get["/monitored-endpoints/{endpointName}"] = parameters =>
            {
                var endpointName = (string)parameters.EndpointName;
                var endpointsData = timingAggregator.AggregateDataForLogicalEndpoint(endpointName);

                return Negotiate.WithModel(endpointsData);
            };
        }
    }
}