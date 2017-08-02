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
        const int DefaultHistory = 5;

        /// <summary>
        /// Initializes the metric API module.
        /// </summary>
        // ReSharper disable SuggestBaseTypeForParameter
        public MonitoredEndpointsModule(EndpointRegistry endpointRegistry, EndpointInstanceActivityTracker activityTracker, CriticalTimeStore criticalTimeStore, ProcessingTimeStore processingTimeStore, RetriesStore retriesStore, QueueLengthStore queueLengthStore)
        // ReSharper restore SuggestBaseTypeForParameter
        {
            Get["/monitored-endpoints"] = parameters =>
            {
                var endpoints = GetMonitoredEndpoints(endpointRegistry, activityTracker);
                var period = ExtractHistoryPeriod();
               
                FillInEndpointData(endpoints, criticalTimeStore, period, (e, v) => e.CriticalTime = v, IntervalsAggregator.AggregateTimings);
                FillInEndpointData(endpoints, processingTimeStore, period, (e, v) => e.ProcessingTime = v, IntervalsAggregator.AggregateTimings);
                FillInEndpointData(endpoints, retriesStore, period, (e, v) => e.Retries = v, IntervalsAggregator.AggregateRetries);
                FillInEndpointData(endpoints, queueLengthStore, period, (e, v) => e.QueueLength = v, IntervalsAggregator.AggregateQueueLength);
                FillInEndpointData(endpoints, processingTimeStore, period, (e, v) => e.Throughput = v, intervals => IntervalsAggregator.AggregateTotalMeasurementsPerSecond (intervals, period));

                return Negotiate.WithModel(endpoints);
            };

            Get["/monitored-endpoints/{endpointName}"] = parameters =>
            {
                var endpointName = (string)parameters.EndpointName;

                var instances = GetMonitoredEndpointInstances(endpointRegistry, endpointName, activityTracker);
                var period = ExtractHistoryPeriod();

                FillInInstanceData(instances, criticalTimeStore, period, (e, v) => e.CriticalTime = v, IntervalsAggregator.AggregateTimings);
                FillInInstanceData(instances, processingTimeStore, period, (e, v) => e.ProcessingTime = v, IntervalsAggregator.AggregateTimings);
                FillInInstanceData(instances, retriesStore, period, (e, v) => e.Retries = v, IntervalsAggregator.AggregateRetries);
                FillInInstanceData(instances, processingTimeStore, period, (e, v) => e.Throughput = v, intervals => IntervalsAggregator.AggregateTotalMeasurementsPerSecond(intervals, period));

                return Negotiate.WithModel(instances);
            };
        }

        HistoryPeriod ExtractHistoryPeriod()
        {
            return HistoryPeriod.FromMinutes(Request.Query["history"] == null || Request.Query["history"] == "undefined" ? DefaultHistory : (int)Request.Query["history"]);
        }

        static void FillInEndpointData(MonitoredEndpoint[] endpoints,
            VariableHistoryIntervalStore store,
            HistoryPeriod period,
            Action<MonitoredEndpoint, MonitoredEndpointValues> setter,
            Func<List<IntervalsStore.EndpointInstanceIntervals>, MonitoredEndpointValues> aggregate)
        {
            var intervals = store.GetIntervals(period, DateTime.UtcNow).ToLookup(k => k.Id.EndpointName);

            foreach (var endpoint in endpoints)
            {
                var values = aggregate(intervals[endpoint.Name].ToList());

                setter(endpoint, values);
            }
        }

        static void FillInInstanceData(MonitoredEndpointInstance[] instances,
            VariableHistoryIntervalStore store,
            HistoryPeriod period,
            Action<MonitoredEndpointInstance, MonitoredEndpointValues> setter,
            Func<List<IntervalsStore.EndpointInstanceIntervals>, MonitoredEndpointValues> aggregate)
        {
            var intervals = store.GetIntervals(period, DateTime.UtcNow).ToLookup(k => k.Id.InstanceId);

            foreach (var instance in instances)
            {
                var values = aggregate(intervals[instance.Id].ToList());

                setter(instance, values);
            }
        }

        static MonitoredEndpointInstance[] GetMonitoredEndpointInstances(EndpointRegistry endpointRegistry, string endpointName, EndpointInstanceActivityTracker activityTracker)
        {
            return endpointRegistry.GetEndpointInstances(endpointName)
                .Select(endpointInstance => new MonitoredEndpointInstance
                {
                    Id = endpointInstance.InstanceId,
                    Name = endpointInstance.EndpointName,
                    IsStale = activityTracker.IsStale(endpointInstance)
                }).ToArray();
        }

        static MonitoredEndpoint[] GetMonitoredEndpoints(EndpointRegistry endpointRegistry, EndpointInstanceActivityTracker activityTracker)
        {
            return endpointRegistry.GetAllEndpoints()
                .Select(endpoint => new MonitoredEndpoint
                {
                    Name = endpoint.Key,
                    EndpointInstanceIds = endpoint.Value.Select(i => i.InstanceId).ToArray(),
                }).ToArray();
        }
    }
}