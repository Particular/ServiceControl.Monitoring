namespace ServiceControl.Monitoring.Http
{
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

            Get["/data"] = x =>
            {
                var data = provider.MonitoringData.Endpoints.Select(e => new JObject
                {
                    {
                        e.Key, new JObject
                        {
                            {"Timestamps", new JArray(e.Value.Timestamps)},
                            {"CriticalTime", new JArray(e.Value.CriticalTime)},
                            {"ProcessingTime", new JArray(e.Value.ProcessingTime)}
                        }
                    }
                }).ToArray();

                var result = new JObject
                {
                    {EndpointsKey, new JArray(data)}
                };

                // TODO: think about writing directly to the output stream
                return Response.AsText(result.ToString(Formatting.None), "application/json");
            };
        }
    }
}
