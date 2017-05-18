namespace ServiceControl.Monitoring.Raw
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using Http;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The raw endpoint data provider, consuming data with <see cref="Consume"/> and providing them with raw data.
    /// </summary>
    public class RawDataProvider : IEndpointDataProvider, IRawDataConsumer
    {
        public const string Name = "Raw";

        /// <summary>
        /// Consumes a new portion of data.
        /// </summary>
        public void Consume(IReadOnlyDictionary<string, string> headers, JObject data)
        {
            var name = headers.GetOriginatingEndpoint();
            contexts.AddOrUpdate(name, data, (context, currentData) => data);
        }

        ConcurrentDictionary<string, JObject> contexts = new ConcurrentDictionary<string, JObject>();

        public IEnumerable<KeyValuePair<string, JObject>> Current => contexts;
        string IEndpointDataProvider.Name { get; } = Name;
    }
}