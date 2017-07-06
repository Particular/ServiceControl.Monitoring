namespace ServiceControl.Monitoring.Raw
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Processing.Snapshot;

    /// <summary>
    /// The diagram endpoint data provider, consuming data with <see cref="Consume" /> and providing them with data optimized
    /// for displaying diagrams.
    /// </summary>
    public class DiagramDataProvider : ISnapshotDataConsumer
    {
        public MonitoringData MonitoringData { get; } = new MonitoringData(10);

        /// <summary>
        /// Consumes a new portion of data.
        /// </summary>
        public void Consume(IReadOnlyDictionary<string, string> headers, JObject data)
        {
            JsonConvert.SerializeObject(data["Timestamp"]);

            var timestamp = data["Timestamp"].Value<DateTime>();

            float? criticalTime = null;
            float? processingTime = null;

            var timers = data["Timers"]?.ToObject<List<JObject>>() ?? new List<JObject>();

            foreach (var timer in timers)
            {
                var timerName = timer["Name"].ToString();

                if (timerName == "Critical Time")
                {
                    criticalTime = timer["Histogram"]["Mean"].Value<float?>();
                }
                else if (timerName == "Processing Time")
                {
                    processingTime = timer["Histogram"]["Mean"].Value<float?>();
                }
            }

            var endpointName = headers.GetOriginatingEndpoint();
            var endpointData = MonitoringData.Get(endpointName);

            endpointData.Record(timestamp, criticalTime, processingTime);
        }

        public const string Name = "Diagram";
    }
}