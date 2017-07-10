namespace ServiceControl.Monitoring.Http
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Nancy;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Timings;

    /// <summary>
    /// Exposes ServiceControl.Monitoring metrics needed for in endpoint overview page.
    /// </summary>
    public class DiagramsApiModule : NancyModule
    {
        /// <summary>
        /// Initializes the metric API module.
        /// </summary>
        public DiagramsApiModule(ProcessingTimeStore processingTimeStore, CriticalTimeStore criticalTimeStore) : base("/diagrams")
        {
            After.AddItemToEndOfPipeline(ctx => ctx.Response
                .WithHeader("Access-Control-Allow-Origin", "*")
                .WithHeader("Access-Control-Allow-Methods", "POST,GET")
                .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type"));

            Get["/data"] = x =>
            {
                var now = DateTime.UtcNow;

                var processingTimes = ToJsonResult(processingTimeStore.GetTimings(now));
                var criticalTimes = ToJsonResult(criticalTimeStore.GetTimings(now));

                var result = new JObject
                {
                    {"ProcessingTime", new JArray(processingTimes)},
                    {"CriticalTime", new JArray(criticalTimes) }
                };

                return Response.AsText(result.ToString(Formatting.None), "application/json");
            };
        }

        static IEnumerable<JObject> ToJsonResult(TimingsStore.EndpointTimings[] data)
        {
            return data.Select(d =>
                new JObject
                {
                    {"EndpointName", d.EndpointName},
                    {"Average", d.Average},
                    {
                        "Data", new JArray(d.Intervals.Select(i => new JObject
                        {
                            {"Time", i.IntervalStart},
                            {"Average", i.Average}
                        }))
                    }
                });
        }
    }
}