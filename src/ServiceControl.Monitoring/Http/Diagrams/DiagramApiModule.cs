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
            Get["/monitored-endpoints"] = x =>
            {
                var endpoints = GetMonitoredEndpoints(endpointRegistry);

                FillInEndpointData(endpoints, criticalTimeStore, (e, v) => e.CriticalTime = v, IntervalsAggregator.AggregateTimings);
                FillInEndpointData(endpoints, processingTimeStore, (e, v) => e.ProcessingTime = v, IntervalsAggregator.AggregateTimings);
                FillInEndpointData(endpoints, retriesStore, (e, v) => e.Retries = v, IntervalsAggregator.AggregateRetries);
                FillInEndpointData(endpoints, queueLengthStore, (e, v) => e.QueueLength = v, IntervalsAggregator.AggregateQueueLength);

                return Negotiate.WithModel(endpoints);
            };

            Get["/monitored-endpoints/{endpointName}"] = parameters =>
            {
                var endpointName = (string)parameters.EndpointName;
                var instances = GetMonitoredEndpointInstances(endpointRegistry, endpointName);

                FillInInstanceData(instances, criticalTimeStore, (e, v) => e.CriticalTime = v, IntervalsAggregator.AggregateTimings);
                FillInInstanceData(instances, processingTimeStore, (e, v) => e.ProcessingTime = v, IntervalsAggregator.AggregateTimings);
                FillInInstanceData(instances, retriesStore, (e, v) => e.Retries = v, IntervalsAggregator.AggregateRetries);

                return Negotiate.WithModel(instances);
            };
        }

        static void FillInEndpointData(MonitoredEndpoint[] endpoints, VariableHistoryIntervalStore store, 
            Action<MonitoredEndpoint, MonitoredEndpointValues> setter,
            Func<List<IntervalsStore.EndpointInstanceIntervals>, MonitoredEndpointValues> aggregate)
        {
            var intervals = store.GetIntervals(HistoryPeriod.FromMinutes(5), DateTime.UtcNow).ToLookup(k => k.Id.EndpointName);

            foreach (var endpoint in endpoints)
            {
                var values = aggregate(intervals[endpoint.Name].ToList());

                setter(endpoint, values);
            }
        }

        static void FillInInstanceData(MonitoredEndpointInstance[] instances, VariableHistoryIntervalStore store,
            Action<MonitoredEndpointInstance, MonitoredEndpointValues> setter,
            Func<List<IntervalsStore.EndpointInstanceIntervals>, MonitoredEndpointValues> aggregate)
        {
            var intervals = store.GetIntervals(HistoryPeriod.FromMinutes(5), DateTime.UtcNow).ToLookup(k => k.Id.InstanceId);

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