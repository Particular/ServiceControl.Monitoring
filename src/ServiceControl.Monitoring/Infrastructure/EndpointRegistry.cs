namespace ServiceControl.Monitoring.Infrastructure
{
    using System.Collections.Generic;
    using System.Linq;

    public class EndpointRegistry
    {
        HashSet<EndpointInstanceId> endpointInstances = new HashSet<EndpointInstanceId>();
        volatile Dictionary<string, IEnumerable<EndpointInstanceId>> lookup = new Dictionary<string, IEnumerable<EndpointInstanceId>>();
        object @lock = new object();

        public void Record(EndpointInstanceId endpointInstanceId)
        {
            lock (@lock)
            {
                endpointInstances.Add(endpointInstanceId);
                lookup = endpointInstances.ToArray()
                    .GroupBy(instance => instance.EndpointName)
                    .ToDictionary(g => g.Key, g => (IEnumerable<EndpointInstanceId>)g.Select(i => i).ToArray());
            }
        }

        public IReadOnlyDictionary<string, IEnumerable<EndpointInstanceId>> GetAllEndpoints()
        {
            return lookup;
        }

        public IEnumerable<EndpointInstanceId> GetEndpointInstances(string endpointName)
        {
            return lookup[endpointName];
        }
    }
}