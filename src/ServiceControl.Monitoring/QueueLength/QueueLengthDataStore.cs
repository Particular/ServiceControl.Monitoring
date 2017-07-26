namespace ServiceControl.Monitoring.QueueLength
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;
    using Newtonsoft.Json.Linq;
    using Timings;

    public class QueueLengthDataStore : IKnowAboutEndpoints, IProvideEndpointMonitoringData
    {
        public QueueLengthDataStore(IQueueLengthCalculator calculator)
        {
            this.calculator = calculator;
        }

        public Dictionary<string, Dictionary<DateTime, double>> GetQueueLengths(DateTime now)
        {
            var results = new Dictionary<string, Dictionary<DateTime, double>>();

            foreach (var queueLength in queueLengths)
            {
                var endpointName = queueLength.Key;

                ConcurrentDictionary<DateTime, double> endpointData;

                if (queueLengths.TryGetValue(endpointName, out endpointData))
                {
                    var endpointDataSnapshot = endpointData.ToArray();

                    var points = endpointDataSnapshot
                        .Where(kvp => kvp.Key > now.Subtract(HistoricalPeriod))
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                    results.Add(endpointName, points);

                    var outdatedPoints = endpointData.Where(kvp => kvp.Key <= now.Subtract(HistoricalPeriod));

                    foreach (var outdatedPoint in outdatedPoints)
                    {
                        double removedValue;
                        endpointData.TryRemove(outdatedPoint.Key, out removedValue);
                    }
                }
            }

            return results;
        }

        public void Store(EndpointInstanceId endpointId, JObject data)
        {
            var updatedEndpoints = new List<string>();

            var counters = (JArray) data["Counters"] ?? new JArray();
            updatedEndpoints.AddRange(UpdateSends(counters));

            var gauges = (JArray) data["Gauges"] ?? new JArray();
            updatedEndpoints.AddRange(UpdateReceives(endpointId.EndpointName, gauges));

            var virtualQueueLengths = calculator.GetQueueLengths();

            foreach (var updatedEndpoint in updatedEndpoints.Distinct())
            {
                SnapshotValueForEndpoint(virtualQueueLengths, updatedEndpoint);
            }
        }

        List<string> UpdateSends(IEnumerable<JToken> sends)
        {
            var allUpdatedVirtualQueues = new List<VirtualQueueId>();

            foreach (var send in sends)
            {
                var tags = send["Tags"].ToObject<string[]>();
                string type;
                if (!tags.TryGetTagValue("type", out type) || type != "queue-length.sent")
                {
                    continue;
                }
                var key = tags.GetTagValue("key");
                var value = send.Value<long>("Count");

                var updatedVirtualQueues = calculator.UpdateSentSequence(key, value);

                allUpdatedVirtualQueues.AddRange(updatedVirtualQueues);
            }

            return allUpdatedVirtualQueues.Select(v => v.EndpointName).Distinct().ToList();
        }

        void SnapshotValueForEndpoint(Dictionary<VirtualQueueId, double> virtualQueueLengths, string endpointName)
        {
            var endpointQueueLength = virtualQueueLengths
                .Where(kv => kv.Key.EndpointName == endpointName)
                .Sum(kv => kv.Value);

            var endpointData = queueLengths.GetOrAdd(endpointName, _ => new ConcurrentDictionary<DateTime, double>());

            endpointData.AddOrUpdate(DateTime.UtcNow, _ => endpointQueueLength, (_, __) => endpointQueueLength);
        }

        List<string> UpdateReceives(string endpointName, IEnumerable<JToken> receives)
        {
            var updatedEndpoints = new List<string>();

            foreach (var receive in receives)
            {
                var tags = receive["Tags"].ToObject<string[]>();
                string type;
                if (!tags.TryGetTagValue("type", out type) || type != "queue-length.received")
                {
                    continue;
                }
                var queue = tags.GetTagValue("queue");
                var key = tags.GetTagValue("key");
                var value = receive.Value<double>("Value");

                var virtualQueueId = new VirtualQueueId
                {
                    EndpointName = endpointName,
                    QueueName = queue,
                    SessionKey = key
                };

                var endpointUpdated = calculator.UpdateReceivedSequence(virtualQueueId, value);

                if (endpointUpdated)
                {
                    updatedEndpoints.Add(endpointName);
                }
            }

            return updatedEndpoints.Distinct().ToList();
        }

        IQueueLengthCalculator calculator;
        TimeSpan HistoricalPeriod = TimeSpan.FromMinutes(5);
        ConcurrentDictionary<string, ConcurrentDictionary<DateTime, double>> queueLengths = new ConcurrentDictionary<string, ConcurrentDictionary<DateTime, double>>();


        IDictionary<string, IEnumerable<EndpointInstanceId>> IKnowAboutEndpoints.AllEndpointData()
        {
            return queueLengths.Keys
                .ToDictionary(
                    endpointName => endpointName,
                    _ => Enumerable.Empty<EndpointInstanceId>()
                );
        }

        public void FillIn(MonitoredEndpoint[] data, DateTime now)
        {
            var snapshot = GetQueueLengths(now);

            foreach (var monitoredEndpoint in data)
            {
                Dictionary<DateTime, double> queueLength;

                if (snapshot.TryGetValue(monitoredEndpoint.Name, out queueLength) && queueLength.Count > 0)
                {
                    var queueLengthValues = queueLength.OrderBy(kvp => kvp.Key).ToArray();
                    var queueLengthMinDate = queueLengthValues.First().Key;

                    monitoredEndpoint.QueueLength = new LinearMonitoredValues
                    {
                        Average = queueLength.Values.Average(),
                        Points = queueLengthValues.Select(kvp => kvp.Value).ToArray(),
                        PointsAxisValues = queueLengthValues
                            .Select(kvp => (int)kvp.Key.Subtract(queueLengthMinDate).TotalMilliseconds)
                            .ToArray()
                    };
                }

            }

        }
    }
}