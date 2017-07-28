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
        public MonitoredEndpointsModule(EndpointRegistry endpointRegistry, CriticalTimeStore criticalTimeStore, ProcessingTimeStore processingTimeStore, RetriesStore retriesStore, QueueLengthStore queueLengthStore)
        {
            const int DefaultHistory = 5;

            Get["/monitored-endpoints"] = parameters =>
            {
                var endpoints = GetMonitoredEndpoints(endpointRegistry);
                var period = HistoryPeriod.FromMinutes((int?)Request.Query["history"] ?? DefaultHistory);

                FillInEndpointData(endpoints, criticalTimeStore, period, (e, v) => e.CriticalTime = v, IntervalsAggregator.AggregateTimings);
                FillInEndpointData(endpoints, processingTimeStore, period, (e, v) => e.ProcessingTime = v, IntervalsAggregator.AggregateTimings);
                FillInEndpointData(endpoints, retriesStore, period, (e, v) => e.Retries = v, IntervalsAggregator.AggregateRetries);
                FillInEndpointData(endpoints, queueLengthStore, period, (e, v) => e.QueueLength = v, IntervalsAggregator.AggregateQueueLength);

                return Negotiate.WithModel(endpoints);
            };

            Get["/monitored-endpoints/{endpointName}"] = parameters =>
            {
                var endpointName = (string)parameters.EndpointName;

                var instances = GetMonitoredEndpointInstances(endpointRegistry, endpointName);
                var period = HistoryPeriod.FromMinutes((int?)Request.Query["history"] ?? DefaultHistory);

                FillInInstanceData(instances, criticalTimeStore, period, (e, v) => e.CriticalTime = v, IntervalsAggregator.AggregateTimings);
                FillInInstanceData(instances, processingTimeStore, period, (e, v) => e.ProcessingTime = v, IntervalsAggregator.AggregateTimings);
                FillInInstanceData(instances, retriesStore, period, (e, v) => e.Retries = v, IntervalsAggregator.AggregateRetries);

                return Negotiate.WithModel(instances);
            };
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

        static MonitoredEndpointInstance[] GetMonitoredEndpointInstances(EndpointRegistry endpointRegistry, string endpointName)
        {
            return endpointRegistry.GetEndpointInstances(endpointName)
                .Select(endpointInstance => new MonitoredEndpointInstance
                {
                    Id = endpointInstance.InstanceId,
                    Name = endpointInstance.EndpointName
                }).ToArray();
        }

        static MonitoredEndpoint[] GetMonitoredEndpoints(EndpointRegistry endpointRegistry)
        {
            return endpointRegistry.GetAllEndpoints()
                .Select(endpoint => new MonitoredEndpoint
                {
                    Name = endpoint.Key,
                    EndpointInstanceIds = endpoint.Value.Select(i => i.InstanceId).ToArray()
                }).ToArray();
        }
    }
}