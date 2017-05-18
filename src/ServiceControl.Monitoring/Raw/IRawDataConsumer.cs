namespace ServiceControl.Monitoring.Raw
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Consumes raw metrics data sent as a serialized Metrics context.
    /// </summary>
    public interface IRawDataConsumer
    {
        /// <summary>
        /// Consumes a new portion of data.
        /// </summary>
        void Consume(IReadOnlyDictionary<string, string> headers, JObject data);
    }
}