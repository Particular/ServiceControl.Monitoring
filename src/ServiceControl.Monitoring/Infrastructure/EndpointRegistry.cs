namespace ServiceControl.Monitoring.Infrastructure
{
    public class EndpointRegistry : BreakdownRegistry<EndpointInstanceId>
    {
        public EndpointRegistry() : base(i => i.EndpointName)
        {
        }
    }
}