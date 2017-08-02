namespace ServiceControl.Monitoring.Infrastructure
{
    using System;
    using System.Collections.Concurrent;

    public class EndpointInstanceActivityTracker
    {
        ConcurrentDictionary<EndpointInstanceId, DateTime> endpointsInstances = new ConcurrentDictionary<EndpointInstanceId, DateTime>();
        public static readonly TimeSpan StalenessThreshold = TimeSpan.FromMinutes(5);

        public void Record(EndpointInstanceId instanceId, DateTime utcNow)
        {
            endpointsInstances.AddOrUpdate(instanceId, utcNow, (_, __) => utcNow);
        }

        public bool IsStale(EndpointInstanceId endpointInstance)
        {
            DateTime lastActivityTime;
            if (endpointsInstances.TryGetValue(endpointInstance, out lastActivityTime))
            {
                var timeSpan = DateTime.UtcNow - lastActivityTime;
                return timeSpan > StalenessThreshold;
            }

            return false;
        }
    }
}