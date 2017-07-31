﻿namespace NServiceBus.Metrics.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using global::Newtonsoft.Json.Linq;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Monitoring;
    using Conventions = AcceptanceTesting.Customization;

    public class When_querying_timings_data : ApiIntegrationTest
    {
        static string ReceiverEndpointName => Conventions.Conventions.EndpointNamingConvention(typeof(MonitoringEndpoint));

        [Test]
        public async Task Should_report_via_http()
        {
            JToken processingTime = null;

            await Scenario.Define<Context>()
                .WithEndpoint<MonitoredEndpoint>(c => c.When(s => s.SendLocal(new SampleMessage())))
                .WithEndpoint<MonitoringEndpoint>()
                .Done(c => MetricReported("processingTime", out processingTime, c))
                .Run();

            Assert.IsTrue(processingTime["average"].Value<int>() > 0);
            Assert.AreEqual(20, processingTime["points"].Value<JArray>().Count);
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
                    return Task.Delay(TimeSpan.FromMilliseconds(10));
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
        }
    }
}