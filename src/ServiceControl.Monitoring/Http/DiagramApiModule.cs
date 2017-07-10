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

                var processingTimes = processingTimeStore.GetTimings(now);
                var criticalTimes = criticalTimeStore.GetTimings(now);

                var endpointNames = processingTimes.Select(e => e.EndpointName)
                    .Union(criticalTimes.Select(e => e.EndpointName));

                var endpointsResult = new List<JObject>();

                foreach (var endpointName in endpointNames)
                {
                    var processingTime = processingTimes.FirstOrDefault(pt => pt.EndpointName == endpointName);
                    var criticalTime = criticalTimes.FirstOrDefault(ct => ct.EndpointName == endpointName);

                    endpointsResult.Add(new JObject
                    {
                        { "name", endpointName },
                        { "processingTime", new JObject{ 
                                { "avg", processingTime?.Average },
                                { "points", new JArray(processingTime?.Intervals.OrderBy(i => i.IntervalStart).Select(i => i.Average)) }
                            }
                        },
                        {"criticalTime", new JObject {
                                { "avg", criticalTime?.Average },
                                { "points", new JArray(criticalTime?.Intervals.OrderBy(i => i.IntervalStart).Select(i => i.Average)) }
                            }
                        }
                    });
                }

                var result = new JArray(endpointsResult);

                return Response.AsText(result.ToString(Formatting.None), "application/json");
            };
        }
    }
}