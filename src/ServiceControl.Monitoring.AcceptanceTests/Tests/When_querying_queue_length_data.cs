namespace NServiceBus.Metrics.AcceptanceTests
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Features;
    using global::Newtonsoft.Json;
    using global::Newtonsoft.Json.Linq;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Monitoring;
    using ServiceControl.Monitoring.Processing.Snapshot;
    using ServiceControl.Monitoring.Raw;
    using Transport;

    public class When_querying_queue_length_data : NServiceBusAcceptanceTest
    {
        static string MonitoredEndpointName => Conventions.EndpointNamingConvention(typeof(MonitoredEndpoint));
        static string ReceiverEndpointName => Conventions.EndpointNamingConvention(typeof(Receiver));

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

        [Test]
        public async Task Should_report_via_http()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<MonitoredEndpoint>()
                .WithEndpoint<Receiver>()
                .Done(c =>
                {
                    c.Response = GetRawMetrics();
                    return c.Response != null && c.Response.Contains(MonitoredEndpointName);
                })
                .Run();

            Assert.IsNotNull(context.Response);

            var expected = new JObject
            {
                {
                    "NServiceBus.Endpoints", new JObject
                    {
                        {
                            "Receiver@MyMachine",
                            new JObject
                            {
                                {
                                    "QueueLength", new JObject
                                    {
                                        { "Count", 10 }
                                    }
                                }
                            }
                        },
                        {
                            MonitoredEndpointName,
                            new JObject
                            {
                                {SnapshotDataProvider.Name, JObject.Parse(Data)}
                            }
                        }
                    }
                }
            };

            Assert.AreEqual(expected.ToString(Formatting.None), context.Response);
        }

        static string GetRawMetrics()
        {
            using (var client = new HttpClient())
            {
                return client.GetStringAsync("http://localhost:1234/metrics/raw").GetAwaiter().GetResult();
            }
        }

        class Context : ScenarioContext
        {
            public string Response { get; set; }
        }

        class MonitoredEndpoint : EndpointConfigurationBuilder
        {
            public MonitoredEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<SenderFeature>();
                });
            }

            class SenderFeature : Feature
            {
                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.RegisterStartupTask(
                        b => new MetricSenderTask(b.Build<IDispatchMessages>(), context.Settings.EndpointName(), Data, ReceiverEndpointName));
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
                });
            }
        }
    }
}