namespace ServiceControl.Monitoring.Http
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides data grouped by endpoint.
    /// </summary>
    public interface ISnapshotDataProvider
    {
        /// <summary>
        /// Gets name of the provider
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Provides current snapshot of data.
        /// </summary>
        IEnumerable<KeyValuePair<string, JObject>> Current { get; }
    }
}