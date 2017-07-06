namespace ServiceControl.Monitoring.Http
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
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
        public DiagramsApiModule(DiagramDataProvider provider, DurationsDataStore durationsDataStore) : base("/diagrams")
        {
            After.AddItemToEndOfPipeline(ctx => ctx.Response
                .WithHeader("Access-Control-Allow-Origin", "*")
                .WithHeader("Access-Control-Allow-Methods", "POST,GET")
                .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type"));

            Get["/data"] = x =>
            {
                var lastEntryDate = DateTime.Now.Subtract(TimeSpan.FromMinutes(5));

                var processingTimes = ToJsonResult(durationsDataStore.ProcessingTimes, lastEntryDate);
                var criticalTimes = ToJsonResult(durationsDataStore.CriticalTimes, lastEntryDate);

                var result = new JObject
                {
                    {"ProcessingTime", new JArray(processingTimes)},
                    {"CriticalTime", new JArray(criticalTimes) }
                };

                return Response.AsText(result.ToString(Formatting.None), "application/json");
            };
        }

        static List<JObject> ToJsonResult(ConcurrentDictionary<string, ConcurrentDictionary<DateTime, DurationsDataStore.DurationBucket>> durations, 
            DateTime lastEntryDate)
        {
            var snapshots = durations.ToArray().Select(kv =>
                new EndpointSnapshot
                {
                    Name = kv.Key,
                    Data = kv.Value.ToArray().Where(d => d.Key > lastEntryDate).OrderBy(d => d.Key)
                }
            );

            var timings = new List<JObject>();

            foreach (var snapshot in snapshots)
            {
                var average = snapshot.Data.Sum(d => d.Value.TotalTime) /
                              (double) snapshot.Data.Sum(d => d.Value.TotalMeasurements);

                timings.Add(new JObject
                {
                    {"EndpointName", snapshot.Name},
                    {"Average", average},
                    {
                        "Data", new JArray(snapshot.Data.Select(s => new JObject
                        {
                            {"Time", s.Key},
                            {"Average", s.Value.TotalTime / (double) s.Value.TotalMeasurements}
                        }))
                    }
                });
            }

            return timings;
        }

        class EndpointSnapshot
        {
            public string Name;
            public IEnumerable<KeyValuePair<DateTime, DurationsDataStore.DurationBucket>> Data;
        }
    }
}