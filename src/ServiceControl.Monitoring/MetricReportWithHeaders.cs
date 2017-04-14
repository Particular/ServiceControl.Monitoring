namespace NServiceBus.Metrics
{
    using System.Collections.Generic;
    using global::Newtonsoft.Json.Linq;

    /// <summary>
    /// Provides metrics data with headers.
    /// </summary>
    public class MetricReportWithHeaders
    {
        public MetricReportWithHeaders(JObject data, IReadOnlyDictionary<string, string> headers)
        {
            Data = data;
            Headers = headers;
        }

        public JObject Data { get; private set; }
        public IReadOnlyDictionary<string, string> Headers { get; private set; }
    }
}