namespace ServiceControl.Monitoring.Http
{
    using System.Linq;
    using Nancy;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using QueueLength;

    public class QueueLengthApiModule : NancyModule
    {
        /// <summary>
        /// Initializes the metric API module.
        /// </summary>
        public QueueLengthApiModule(QueueLengthDataStore store) : base("/metrics")
        {
            After.AddItemToEndOfPipeline(ctx => ctx.Response
                .WithHeader("Access-Control-Allow-Origin", "*")
                .WithHeader("Access-Control-Allow-Methods", "POST,GET")
                .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type"));

            // consider hypermedia like listing of metrics
            // Get[""] = x => Response

            Get["/queue-length"] = x =>
            {
                var endpoints = store.Current.Select(kvp => new JObject
                    {
                        {kvp.Key, kvp.Value}
                    })
                    .ToArray();

                var result = new JObject
                {
                    {"NServiceBus.Endpoints", new JArray(endpoints)}
                };

                // TODO: think about writing directly to the output stream
                return Response.AsText(result.ToString(Formatting.None), "application/json");
            };

            //Get["/{metricName}/{aggregation?}"] = x => $"<p>{x.metricName}:{(string.IsNullOrEmpty(x.aggregation) ? "raw" : x.aggregation)}</p>";
        }
    }
}