namespace ServiceControl.Monitoring.Infrastructure
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;

    public class EndpointRegistry
    {
        ConcurrentDictionary<EndpointInstanceId, bool> endpointInstances = new ConcurrentDictionary<EndpointInstanceId, bool>();

        public void Record(EndpointInstanceId endpointInstanceId)
        {
            endpointInstances.TryAdd(endpointInstanceId, true);    
        }

        public IDictionary<string, IEnumerable<EndpointInstanceId>> GetAllEndpoints()
        {
            return endpointInstances.ToArray()
                .GroupBy(kv => kv.Key.EndpointName)
                .ToDictionary(g => g.Key, g => g.Select(i => i.Key));
        }

        public EndpointInstanceId[] GetEndpointInstances(string endpointName)
        {
            return endpointInstances.Keys
                .Where(instance => instance.EndpointName == endpointName)
                .ToArray();
        }
    }
}