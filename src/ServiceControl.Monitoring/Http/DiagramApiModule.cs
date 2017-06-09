namespace ServiceControl.Monitoring.Http
{
    using System.Collections.Generic;
    using System.Linq;
    using Nancy;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Raw;

    /// <summary>
    /// Exposes ServiceControl.Monitoring metrics needed for in endpoint overview page.
    /// </summary>
    public class DiagramsApiModule : NancyModule
    {
        const string EndpointsKey = "NServiceBus.Endpoints";

        /// <summary>
        /// Initializes the metric API module.
        /// </summary>
        public DiagramsApiModule(DiagramDataProvider provider) : base("/diagrams")
        {
            After.AddItemToEndOfPipeline(ctx => ctx.Response
                .WithHeader("Access-Control-Allow-Origin", "*")
                .WithHeader("Access-Control-Allow-Methods", "POST,GET")
                .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type"));

            // consider hypermedia like listing of metrics
            // Get[""] = x => Response

            /*
            Get["/data"] = x =>
            {
                var endpoints = provider.Current.Select(kvp => new
                    {
                        Provider = DiagramDataProvider.Name,
                        Endpoint = kvp.Key,
                        kvp.Value
                    })
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
            */
            //Get["/{metricName}/{aggregation?}"] = x => $"<p>{x.metricName}:{(string.IsNullOrEmpty(x.aggregation) ? "raw" : x.aggregation)}</p>";
        }
    }
}
