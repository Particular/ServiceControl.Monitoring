namespace ServiceControl.Monitoring.Processing.Snapshot
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Consumes raw metrics data sent as a serialized Metrics context.
    /// </summary>
    public interface ISnapshotDataConsumer
    {
        /// <summary>
        /// Consumes a new portion of data.
        /// </summary>
        void Consume(IReadOnlyDictionary<string, string> headers, JObject data);
    }
}