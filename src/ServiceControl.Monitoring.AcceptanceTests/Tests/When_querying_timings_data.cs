namespace NServiceBus.Metrics.AcceptanceTests
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Monitoring;
    using ServiceControl.Monitoring.Processing.RawData.NServiceBus.Metrics;
    using Conventions = AcceptanceTesting.Customization;

    public class When_querying_timings_data : NServiceBusAcceptanceTest
    {
        static string ReceiverEndpointName => Conventions.Conventions.EndpointNamingConvention(typeof(MonitoringEndpoint));
        static string MonitoredEndpointName => Conventions.Conventions.EndpointNamingConvention(typeof(MonitoredEndpoint));

        [Test]
        public async Task Should_report_via_http()
        {
            string timingData = null;

            await Scenario.Define<Context>()
                .WithEndpoint<MonitoredEndpoint>(c => c.When(s => s.SendLocal(new SampleMessage())))
                .WithEndpoint<MonitoringEndpoint>()
                .Done(c => c.TimingReportReceived && (timingData = GetTimingData()) != null)
                .Run();

            Assert.IsTrue(timingData.Contains(MonitoredEndpointName));
        }

        static string GetTimingData()
        {
            using (var client = new HttpClient())
            {
                return client.GetStringAsync("http://localhost:1234/diagrams/data").GetAwaiter().GetResult();
            }
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
                EndpointSetup<DefaultServer>(c => { EndpointFactory.MakeMetricsReceiver(c, Settings); });
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