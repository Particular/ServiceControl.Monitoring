namespace ServiceControl.Monitoring.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Timings;

    public interface IKnowAboutEndpoints
    {
        IDictionary<string, IEnumerable<EndpointInstanceId>> AllEndpointData();
    }

    public interface IProvideEndpointMonitoringData
    {
        void FillIn(MonitoredEndpoint[] data, DateTime now);
    }

    public interface IProvideEndpointInstanceMonitoringData
    {
        void FillIn(MonitoredEndpointInstance[] data, DateTime now);
    }

    public class EndpointRegistry
    {
        readonly IKnowAboutEndpoints[] endpointDataSources;

        public EndpointRegistry(IKnowAboutEndpoints[] endpointDataSources)
        {
            this.endpointDataSources = endpointDataSources;
        }

        public IDictionary<string, IEnumerable<EndpointInstanceId>> AllEndpoints()
        {
            return (from endpointDataSource in endpointDataSources
                from endpoint in endpointDataSource.AllEndpointData()
                group endpoint by endpoint.Key
                into g
                select new
                {
                    Endpoint = g.Key,
                    Instances = g.SelectMany(x => x.Value).Distinct()
                }).ToDictionary(kvp => kvp.Endpoint, kvp => kvp.Instances);
        }
    }
}
