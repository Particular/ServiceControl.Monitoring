namespace ServiceControl.Monitoring.Http.Diagrams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Infrastructure;
    using Nancy;
    using QueueLength;
    using Timings;

    /// <summary>
    /// Exposes ServiceControl.Monitoring metrics needed for in endpoint overview page.
    /// </summary>
    public class MonitoredEndpointsModule : ApiModule
    {
        /// <summary>
        /// Initializes the metric API module.
        /// </summary>
        // ReSharper disable SuggestBaseTypeForParameter
        public MonitoredEndpointsModule(IProvideBreakdownBy<EndpointInstanceId>[] metricByInstance, IProvideBreakdownBy<EndpointMessageType>[] metricByMessageType, EndpointRegistry endpointRegistry, EndpointInstanceActivityTracker activityTracker, MessageTypeRegistry messageTypeRegistry)
            // ReSharper restore SuggestBaseTypeForParameter
        {
            var metricByInstanceLookup = metricByInstance.ToDictionary(i => i.GetType());

            var metricByMessageTypeLookup = metricByMessageType.ToDictionary(i => i.GetType());

            var instanceMetrics = new[]
            {
                CreateMetric<EndpointInstanceId, ProcessingTimeStore>("ProcessingTime", IntervalsAggregator.AggregateTimings),
                CreateMetric<EndpointInstanceId, CriticalTimeStore>("CriticalTime", IntervalsAggregator.AggregateTimings),
                CreateMetric<EndpointInstanceId, RetriesStore>("Retries", IntervalsAggregator.AggregateRetries),
                CreateMetric<EndpointInstanceId, QueueLengthStore>("QueueLength", IntervalsAggregator.AggregateQueueLength),
                CreateMetric<EndpointInstanceId, ProcessingTimeStore>("Throughput", IntervalsAggregator.AggregateTotalMeasurementsPerSecond)
            };

            var messageTypeMetrics = new[]
            {
                CreateMetric<EndpointMessageType, ProcessingTimeStore>("ProcessingTime", IntervalsAggregator.AggregateTimings),
                CreateMetric<EndpointMessageType, CriticalTimeStore>("CriticalTime", IntervalsAggregator.AggregateTimings),
                CreateMetric<EndpointMessageType, RetriesStore>("Retries", IntervalsAggregator.AggregateRetries),
                CreateMetric<EndpointMessageType, ProcessingTimeStore>("Throughput", IntervalsAggregator.AggregateTotalMeasurementsPerSecond)
            };

            var detailedMetrics = new HashSet<string>
            {
                "Throughput"
            };

            Get["/monitored-endpoints"] = parameters =>
            {
                var endpoints = GetMonitoredEndpoints(endpointRegistry, activityTracker);
                var period = ExtractHistoryPeriod();

                foreach (var metric in instanceMetrics)
                {
                    var store = metricByInstanceLookup[metric.StoreType];
                    var intervals = store.GetIntervals(period, DateTime.UtcNow).ToLookup(k => k.Id.EndpointName);

                    foreach (var endpoint in endpoints)
                    {
                        var values = metric.Aggregate(intervals[endpoint.Name].ToList(), period);

                        endpoint.Metrics.Add(metric.ReturnName, values);
                    }
                }

                return Negotiate.WithModel(endpoints);
            };

            Get["/monitored-endpoints/{endpointName}"] = parameters =>
            {
                var endpointName = (string) parameters.EndpointName;
                var period = ExtractHistoryPeriod();

                var instances = GetMonitoredEndpointInstances(endpointRegistry, endpointName, activityTracker);

                var digest = new MonitoredEndpointDigest();
                var metricDetails = new MonitoredEndpointMetricDetails();

                foreach (var metric in instanceMetrics)
                {
                    var store = metricByInstanceLookup[metric.StoreType];
                    var intervals = store.GetIntervals(period, DateTime.UtcNow);

                    var intervalsByEndpoint = intervals.ToLookup(k => k.Id.EndpointName);

                    var endpointValues = metric.Aggregate(intervalsByEndpoint[endpointName].ToList(), period);

                    if (detailedMetrics.Contains(metric.ReturnName))
                    {
                        var details = new MonitoredValuesWithTimings
                        {
                            Points = endpointValues.Points,
                            Average = endpointValues.Average,
                            TimeAxisValues = GetTimeAxisValues(intervalsByEndpoint[endpointName])
                        };

                        metricDetails.Metrics.Add(metric.ReturnName, details);
                    }

                    var metricDigest = new MonitoredEndpointMetricDigest
                    {
                        Latest = endpointValues.Points.LastOrDefault(),
                        Average = endpointValues.Average
                    };

                    digest.Metrics.Add(metric.ReturnName, metricDigest);

                    var intervalsByInstanceId = intervals.ToLookup(k => k.Id.InstanceId);

                    foreach (var instance in instances)
                    {
                        var instanceValues = metric.Aggregate(intervalsByInstanceId[instance.Id].ToList(), period);

                        instance.Metrics.Add(metric.ReturnName, instanceValues);
                    }
                }

                var messageTypes = GetMonitoredMessageTypes(messageTypeRegistry, endpointName);

                foreach (var metric in messageTypeMetrics)
                {
                    var store = metricByMessageTypeLookup[metric.StoreType];
                    var intervals = store.GetIntervals(period, DateTime.UtcNow).ToLookup(k => k.Id);

                    foreach (var messageType in messageTypes)
                    {
                        var values = metric.Aggregate(intervals[new EndpointMessageType(endpointName, messageType.MessageType)].ToList(), period);

                        messageType.Metrics.Add(metric.ReturnName, values);
                    }
                }

                var data = new MonitoredEndpointDetails
                {
                    Digest = digest,
                    Instances = instances,
                    MessageTypes = messageTypes,
                    MetricDetails = metricDetails

                };

                return Negotiate.WithModel(data);
            };
        }

        static DateTime[] GetTimeAxisValues(IEnumerable<IntervalsStore<EndpointInstanceId>.IntervalsBreakdown> intervals)
        {
            return intervals
                .SelectMany(ib => ib.Intervals.Select(x => x.IntervalStart.ToUniversalTime()))
                .Distinct()
                .OrderBy(i => i)
                .ToArray();
        }

        static MonitoredMetric<BreakdownT> CreateMetric<BreakdownT, StoreT>(string name, Aggregation<BreakdownT> aggregation)
            where StoreT : IProvideBreakdownBy<BreakdownT>
        {
            return new MonitoredMetric<BreakdownT>
            {
                StoreType = typeof(StoreT),
                ReturnName = name,
                Aggregate = aggregation
            };
        }

        HistoryPeriod ExtractHistoryPeriod()
        {
            return HistoryPeriod.FromMinutes(Request.Query["history"] == null || Request.Query["history"] == "undefined" ? DefaultHistory : (int) Request.Query["history"]);
        }

        static MonitoredEndpointInstance[] GetMonitoredEndpointInstances(EndpointRegistry endpointRegistry, string endpointName, EndpointInstanceActivityTracker activityTracker)
        {
            return endpointRegistry.GetForEndpointName(endpointName)
                .Select(endpointInstance => new MonitoredEndpointInstance
                {
                    Id = endpointInstance.InstanceId,
                    Name = endpointInstance.EndpointName,
                    IsStale = activityTracker.IsStale(endpointInstance)
                }).ToArray();
        }

        static MonitoredEndpoint[] GetMonitoredEndpoints(EndpointRegistry endpointRegistry, EndpointInstanceActivityTracker activityTracker)
        {
            return endpointRegistry.GetGroupedByEndpointName()
                .Select(endpoint => new MonitoredEndpoint
                {
                    Name = endpoint.Key,
                    EndpointInstanceIds = endpoint.Value.Select(i => i.InstanceId).ToArray(),
                    IsStale = endpoint.Value.Any(activityTracker.IsStale)
                })
                .ToArray();
        }

        static MonitoredEndpointMessageType[] GetMonitoredMessageTypes(MessageTypeRegistry registry, string endpointName)
        {
            return registry.GetForEndpointName(endpointName)
                .Select(mt => new MonitoredEndpointMessageType
                {
                    MessageType = mt.MessageType
                })
                .ToArray();
        }

        const int DefaultHistory = 5;

        delegate MonitoredValues Aggregation<T>(List<IntervalsStore<T>.IntervalsBreakdown> intervals, HistoryPeriod period);

        class MonitoredMetric<T>
        {
            public Type StoreType { get; set; }
            public string ReturnName { get; set; }
            public Aggregation<T> Aggregate { get; set; }
        }
    }
}