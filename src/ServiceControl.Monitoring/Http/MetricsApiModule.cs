namespace ServiceControl.Monitoring.Http
{
    using System.Collections.Generic;
    using System.Linq;
    using Nancy;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Exposes ServiceControl.Monitoring metrics.
    /// </summary>
    public class MetricsApiModule : NancyModule
    {
        const string EndpointsKey = "NServiceBus.Endpoints";

        /// <summary>
        /// Initializes the metric API module.
        /// </summary>
        public MetricsApiModule(IEnumerable<IEndpointDataProvider> providers) : base("/metrics")
        {
            // consider hypermedia like listing of metrics
            // Get[""] = x => Response

            Get["/raw"] = x =>
            {
                var endpoints = providers.SelectMany(p => p.Current.Select(kvp => new
                    {
                        Provider = p.Name,
                        Endpoint = kvp.Key,
                        kvp.Value
                    }))
                    .GroupBy(a => a.Endpoint)
                    .Select(endpointGrouped => new JProperty(endpointGrouped.Key, new JObject(endpointGrouped.Select(e => new JProperty(e.Provider, e.Value)))))
                    .ToArray();

                var result = new JObject
                {
                    {EndpointsKey, new JObject(endpoints)}
                };
                
                // TODO: think about writing directly to the output stream
                return Response.AsText(result.ToString(Formatting.None), "application/json");
            };

            //Get["/{metricName}/{aggregation?}"] = x => $"<p>{x.metricName}:{(string.IsNullOrEmpty(x.aggregation) ? "raw" : x.aggregation)}</p>";
        }
    }
}
