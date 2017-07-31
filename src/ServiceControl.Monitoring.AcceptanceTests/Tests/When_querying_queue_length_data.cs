namespace NServiceBus.Metrics.AcceptanceTests
{
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Features;
    using global::Newtonsoft.Json.Linq;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Monitoring;
    using Transport;

    public class When_querying_queue_length_data : ApiIntegrationTest
    {
        static string ReceiverEndpointName => Conventions.EndpointNamingConvention(typeof(Receiver));

        [Test]
        public async Task When_sending_single_interval_data_Should_report_average_based_on_this_single_interval()
        {
            JToken queueLength = null;

            await Scenario.Define<Context>()
                .WithEndpoint<MonitoredEndpoint>()
                .WithEndpoint<Receiver>()
                .Done(c => MetricReported("queueLength", out queueLength, c))
                .Run();

            Assert.AreEqual(10, queueLength["average"].Value<int>());

            var points = queueLength["points"].Values<int>().ToArray();

            CollectionAssert.IsNotEmpty(points);

            //NOTE: We can get queue-length value 10 in more than one interval even for single report being sent
            //      This is caused by the snaposhotting logic that is storing current estimate for queue length periodically
            //      If the test run crosses interval boundary value 10 can we reported for more than one interval.
            Assert.IsTrue(points.All(v => v == 10 || v == 0));
            Assert.IsTrue(points.Any(v => v == 10));
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