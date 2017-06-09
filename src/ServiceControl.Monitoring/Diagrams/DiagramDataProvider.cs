namespace ServiceControl.Monitoring.Raw
{
    using System.Collections.Generic;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The diagram endpoint data provider, consuming data with <see cref="Consume"/> and providing them with data optimized for displaying diagrams.
    /// </summary>
    public class DiagramDataProvider : IRawDataConsumer
    {
        public const string Name = "Diagram";

        /// <summary>
        /// Consumes a new portion of data.
        /// </summary>
        public void Consume(IReadOnlyDictionary<string, string> headers, JObject data)
        {
            var timestamp = data["Timestamp"].ToString();

            var criticalTime = string.Empty;
            var processingTime = string.Empty;

            var timers = data["Timers"]?.ToObject<List<JObject>>() ?? new List<JObject>();

            foreach (var timer in timers)
            {
                var timerName = timer["Name"].ToString();

                if (timerName == "Critical Time")
                {
                    criticalTime = timer["Rate"]["OneMinuteRate"].ToString();
                }
                else if (timerName == "Processing Time")
                {
                    processingTime = timer["Rate"]["OneMinuteRate"].ToString();
                }
            }

            var endpointName = headers.GetOriginatingEndpoint();
            var endpointData = monitoringData.Get(endpointName);

            endpointData.Record(timestamp, criticalTime, processingTime);
        }

        MonitoringData monitoringData = new MonitoringData(10);
    }
}