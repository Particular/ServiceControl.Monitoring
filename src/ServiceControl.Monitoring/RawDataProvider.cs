namespace ServiceControl.Monitoring
{
    using System.Collections.Concurrent;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using NServiceBus;
    using NServiceBus.Metrics;

    /// <summary>
    /// The raw endpoint data provider, consuming data with <see cref="Consume"/> and providing them with <see cref="CurrentRawData"/>.
    /// </summary>
    public class RawDataProvider
    {
        /// <summary>
        /// The endpoints property name for <see cref="CurrentRawData"/>.
        /// </summary>
        public const string EndpointsKey = "NServiceBus.Endpoints";

        /// <summary>
        /// The recent snapshot of data.
        /// </summary>
        public JObject CurrentRawData
        {
            get
            {
                var properties = contexts.Select(pair => new JProperty(pair.Key, pair.Value));
                var endpoints = new JObject(properties);

                return new JObject
                {
                    {EndpointsKey, endpoints}
                };
            }
        }

        /// <summary>
        /// Consumes a new portion of data.
        /// </summary>
        /// <param name="report"></param>
        public void Consume(MetricReportWithHeaders report)
        {
            var data = report.Data;

            string name;
            report.Headers.TryGetValue(Headers.OriginatingEndpoint, out name);
            name = name ?? "";
            contexts.AddOrUpdate(name, data, (context, currentData) => data);
        }

        ConcurrentDictionary<string, JObject> contexts = new ConcurrentDictionary<string, JObject>();
    }
}