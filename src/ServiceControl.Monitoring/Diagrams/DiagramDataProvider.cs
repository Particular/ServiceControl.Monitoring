namespace ServiceControl.Monitoring.Raw
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Http;
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
            var name = headers.GetOriginatingEndpoint();
            Dictionary<string, DiagramData> endpointData;
            
            if (!monitoringData.Endpoints.TryGetValue(name, out endpointData))
            {
                endpointData = new Dictionary<string, DiagramData>();
                monitoringData.Endpoints.Add(name, endpointData);
            }

            var meters = data["Meters"]?.ToObject<List<Meter>>() ?? new List<Meter>();
            foreach (var meter in meters)
            {
                DiagramData diagramData;
                endpointData.TryGetValue(meter.Name, out diagramData);
                if (diagramData == null)
                {
                    diagramData = new DiagramData();
                    endpointData.Add(meter.Name, diagramData);
                }
                diagramData.Data.Add(meter.Count);
            }

            var timers = data["Timers"]?.ToObject<List<Timer>>() ?? new List<Timer>();
            foreach (var timer in timers)
            {
                DiagramData diagramData;
                endpointData.TryGetValue(timer.Name, out diagramData);
                if (diagramData == null)
                {
                    diagramData = new DiagramData();
                    endpointData.Add(timer.Name, diagramData);
                }
                diagramData.Data.Add(timer.TotalTime);
            }            
        }

        MonitoringData monitoringData = new MonitoringData();

        public IEnumerable<KeyValuePair<string, JObject>> Current => monitoringData.Endpoints.Select(x => new KeyValuePair<string, JObject>(x.Key, JObject.FromObject(x.Value)));
    }
}