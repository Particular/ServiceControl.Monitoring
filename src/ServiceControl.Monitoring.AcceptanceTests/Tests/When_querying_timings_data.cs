namespace NServiceBus.Metrics.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using global::Newtonsoft.Json.Linq;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Monitoring;
    using ServiceControl.Monitoring.Metrics.Raw;
    using Conventions = AcceptanceTesting.Customization;

    public class When_querying_timings_data : ApiIntegrationTest
    {
        static string ReceiverEndpointName => Conventions.Conventions.EndpointNamingConvention(typeof(MonitoringEndpoint));
        static string MonitoredEndpointName => Conventions.Conventions.EndpointNamingConvention(typeof(MonitoredEndpoint));

        [Test]
        public async Task Should_report_via_http()
        {
            string response = null;

            await Scenario.Define<Context>()
                .WithEndpoint<MonitoredEndpoint>(c => c.When(s => s.SendLocal(new SampleMessage())))
                .WithEndpoint<MonitoringEndpoint>()
                .Done(c => c.TimingReportReceived && (response = GetString("http://localhost:1234/monitored-endpoints")) != null)
                .Run();

            Assert.IsNotNull(response);

            var result = JArray.Parse(response);
            Assert.AreEqual(1, result.Count);

            var endpoint = result[0].Value<JObject>();
            var processingTime = endpoint["processingTime"].Value<JObject>();

            Assert.IsTrue(processingTime["average"].Value<int>() > 0);
            Assert.AreEqual(20, processingTime["points"].Value<JArray>().Count);
        }

        class Context : ScenarioContext
        {
            public bool TimingReportReceived { get; set; }
        }

        class MonitoredEndpoint : EndpointConfigurationBuilder
        {
            public MonitoredEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
#pragma warning disable 618
                    c.EnableMetrics().SendMetricDataToServiceControl(ReceiverEndpointName, TimeSpan.FromSeconds(5));
#pragma warning restore 618
                });
            }

            class Handler : IHandleMessages<SampleMessage>
            {
                public Task Handle(SampleMessage message, IMessageHandlerContext context)
                {
                    return Task.FromResult(0);
                }
            }
        }

        class MonitoringEndpoint : EndpointConfigurationBuilder
        {
            public MonitoringEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    EndpointFactory.MakeMetricsReceiver(c, Settings);
                    c.LimitMessageProcessingConcurrencyTo(1);
                });
            }

            public class LongValueOccurrenceHandler : IHandleMessages<LongValueOccurrences>
            {
                Context testContext;

                public LongValueOccurrenceHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(LongValueOccurrences message, IMessageHandlerContext context)
                {
                    testContext.TimingReportReceived = true;

                    return Task.FromResult(0);
                }
            }
        }

        public class SampleMessage : IMessage { }
    }
}