namespace ServiceControl.Monitoring.Http
{
    using System.Linq;
    using Nancy;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Processing.RawData;
    using Raw;

    /// <summary>
    /// Exposes ServiceControl.Monitoring metrics needed for in endpoint overview page.
    /// </summary>
    public class DiagramsApiModule : NancyModule
    {
        /// <summary>
        /// Initializes the metric API module.
        /// </summary>
        public DiagramsApiModule(DiagramDataProvider provider, RawDataProvider rawDataProvider) : base("/diagrams")
        {
            After.AddItemToEndOfPipeline(ctx => ctx.Response
                .WithHeader("Access-Control-Allow-Origin", "*")
                .WithHeader("Access-Control-Allow-Methods", "POST,GET")
                .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type"));

            Get["/data"] = x =>
            {
                var data = provider.MonitoringData.Endpoints.Select(e => new JObject
                {
                    {
                        e.Key, new JObject
                        {
                            {"Timestamps", new JArray(e.Value.Timestamps)}
                        }
                    }
                }).ToArray();

                var rawCriticalTime = rawDataProvider.CriticalTimes.Select(e => new JObject
                {
                    {
                        e.Key, new JArray(e.Value.Values)
                    }
                });

                var rawProcessingTime = rawDataProvider.ProcessingTimes.Select(e => new JObject
                {
                    {
                        e.Key, new JArray(e.Value.Values)
                    }
                });

                var result = new JObject
                {
                    {EndpointsKey, new JArray(data)},
                    {"CriticalTimes", new JArray(rawCriticalTime)},
                    {"ProcessingTime", new JArray(rawProcessingTime)}
                };

                // TODO: think about writing directly to the output stream
                return Response.AsText(result.ToString(Formatting.None), "application/json");
            };
        }

        const string EndpointsKey = "NServiceBus.Endpoints";
    }
}