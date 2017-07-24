namespace NServiceBus.Metrics.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Features;
    using global::Newtonsoft.Json;
    using global::Newtonsoft.Json.Linq;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Monitoring;
    using Transport;

    public class When_querying_queue_length_data : ApiIntegrationTest
    {
        static string ReceiverEndpointName => Conventions.EndpointNamingConvention(typeof(Receiver));

        [Test]
        public async Task Should_report_via_http()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MonitoredEndpoint>()
                .WithEndpoint<Receiver>()
                .Done(c =>
                {
                    c.Response = GetString("http://localhost:1234/monitored-endpoints");
                    return c.Response != null && c.Response.Contains("10");
                })
                .Run();

            Assert.IsNotNull(context.Response);

            var result = JArray.Parse(context.Response);
            Assert.AreEqual(1, result.Count);

            var endpoint = result[0].Value<JObject>();
            var queueLength = endpoint["queueLength"];

            var expected = new JObject
            {
                { "pointsAxisValues", new JArray(new []{0}) },
                { "average", 10 },
                { "points", new JArray(new []{10}) }
            };

            Assert.AreEqual(expected.ToString(Formatting.None), queueLength.ToString(Formatting.None));
        }

        const string Data = @"{
    ""Version"": ""2"",
    ""Timestamp"": ""2017-05-11T07:13:28.5918Z"",
    ""Context"": ""Not used"",
    ""Counters"": [{
        ""Name"": ""Sent sequence for sendingmessage.receiver1-a328a49b-4212-4a34-8e90-726848230c03"",
        ""Count"": 12,
        ""Unit"": ""Sequence"",
        ""Tags"": [""key:sendingmessage.receiver1-a328a49b-4212-4a34-8e90-726848230c03"",
        ""type:queue-length.sent""]
    }],
    ""Gauges"": [{
        ""Name"": ""Received sequence for sendingmessage.receiver1-a328a49b-4212-4a34-8e90-726848230c03"",
        ""Value"": 2.00,
        ""Unit"": ""Sequence"",
        ""Tags"": [""key:sendingmessage.receiver1-a328a49b-4212-4a34-8e90-726848230c03"",
        ""queue:Receiver@MyMachine"",
        ""type:queue-length.received""]
    }],
    ""Meters"": [],
    ""Timers"": []
}";

        class Context : ScenarioContext
        {
            public string Response { get; set; }
        }

        class MonitoredEndpoint : EndpointConfigurationBuilder
        {
            public MonitoredEndpoint()
            {
                EndpointSetup<DefaultServer>(c => { c.EnableFeature<SenderFeature>(); });
            }

            class SenderFeature : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.RegisterStartupTask(
                        b => new MetricSenderTask(b.Build<IDispatchMessages>(), context.Settings.EndpointName(), string.Empty, Data, ReceiverEndpointName));
                }
            }
        }

        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    EndpointFactory.MakeMetricsReceiver(c, Settings);
                    c.LimitMessageProcessingConcurrencyTo(1);
                });
            }
        }
    }
}