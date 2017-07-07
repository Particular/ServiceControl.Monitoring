namespace NServiceBus.Metrics
{
    using global::Newtonsoft.Json.Linq;
    using NServiceBus;

    /// <summary>
    /// The reporting message.
    /// </summary>
    public class MetricReport : IMessage
    {
        /// <summary>
        /// Serialized raw data of the report.
        /// </summary>
        public JObject Data { get; set; }
    }
}