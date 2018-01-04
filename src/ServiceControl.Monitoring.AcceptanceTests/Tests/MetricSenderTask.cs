namespace NServiceBus.Metrics.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading.Tasks;
    using Extensibility;
    using Features;
    using Routing;
    using Transport;

    class MetricSenderTask : FeatureStartupTask
    {
        IDispatchMessages dispatcher;
        string endpointName;
        string instanceId;
        string data;
        string destination;

        public MetricSenderTask(IDispatchMessages dispatcher, string endpointName, string instanceId, string data, string destination)
        {
            this.dispatcher = dispatcher;
            this.endpointName = endpointName;
            this.instanceId = instanceId;
            this.data = data;
            this.destination = destination;
        }

        protected override Task OnStart(IMessageSession session)
        {
            var headers = new Dictionary<string, string>();

            var stringBody = $@"{{""Data"" : {data}}}";
            var body = Encoding.UTF8.GetBytes(stringBody);

            headers[Headers.OriginatingEndpoint] = endpointName;
            headers[Headers.EnclosedMessageTypes] = "NServiceBus.Metrics.MetricReport";
            headers["NServiceBus.Metric.InstanceId"] = instanceId;

            var message = new OutgoingMessage(Guid.NewGuid().ToString(), headers, body);
            var operation = new TransportOperation(message, new UnicastAddressTag(destination));

            return dispatcher.Dispatch(new TransportOperations(operation), new TransportTransaction(), new ContextBag());
        }

        protected override Task OnStop(IMessageSession session)
        {
            return Task.FromResult(0);
        }
    }
}