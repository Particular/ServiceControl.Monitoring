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

    public class When_querying_retries_data : ApiIntegrationTest
    {
        static string ReceiverEndpointName => AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(MonitoringEndpoint));

        [Test]
        public async Task Should_report_via_http()
        {
            string response = null;

            await Scenario.Define<Context>()
                .WithEndpoint<MonitoredEndpoint>(c =>
                {
                    c.DoNotFailOnErrorMessages();
                    c.CustomConfig(ec => ec.Recoverability().Immediate(i => i.NumberOfRetries(5)));
                    c.When(s => s.SendLocal(new SampleMessage()));
                })
                .WithEndpoint<MonitoringEndpoint>()
                .Done(c => c.ReportReceived && (response = GetString(MonitoredEndpointsUrl)) != null)
                .Run();

            Assert.IsNotNull(response);

            var result = JArray.Parse(response);
            Assert.AreEqual(1, result.Count);

            var endpoint = result[0].Value<JObject>();
            var retries = endpoint["retries"].Value<JObject>();

            Assert.IsTrue(retries["average"].Value<double>() > 0);
            Assert.AreEqual(20, retries["points"].Value<JArray>().Count);
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
                    throw new Exception("Boom!");
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

            public class OccurrencesHandler : IHandleMessages<Occurrences>
            {
                Context testContext;

                public OccurrencesHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(Occurrences message, IMessageHandlerContext context)
                {
                    testContext.ReportReceived = true;

                    return Task.FromResult(0);
                }
            }
        }
    }
}