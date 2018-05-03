namespace NServiceBus.Metrics.AcceptanceTests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using global::Newtonsoft.Json.Linq;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Monitoring;

    [Category("Integration")]
    public class When_querying_queue_length_data : ApiIntegrationTest
    {
        static string ReceiverEndpointName => Conventions.EndpointNamingConvention(typeof(Receiver));

        [Test]
        public async Task When_sending_single_interval_data_Should_report_average_based_on_this_single_interval()
        {
            JToken queueLength = null;

            await Scenario.Define<Context>()
                .WithEndpoint<MonitoredEndpoint>(c =>
                {
                    c.When(s => s.SendLocal(new SampleMessage()));
                })
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