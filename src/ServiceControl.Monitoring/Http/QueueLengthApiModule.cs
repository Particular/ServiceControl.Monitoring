namespace ServiceControl.Monitoring.Http
{
    using System.Linq;
    using Nancy;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using QueueLength;

    public class QueueLengthApiModule : ApiModule
    {
        /// <summary>
        /// Initializes the metric API module.
        /// </summary>
        public QueueLengthApiModule(QueueLengthDataStore store)
        {
            Get["/metrics/queue-length"] = x =>
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
        }
    }
}