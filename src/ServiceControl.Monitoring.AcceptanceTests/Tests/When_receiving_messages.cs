namespace NServiceBus.Metrics.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Features;
    using global::Newtonsoft.Json.Linq;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ObjectBuilder;
    using ServiceControl.Monitoring;

    public class When_receiving_messages : NServiceBusAcceptanceTest
    {
        static readonly JObject Data = JObject.Parse("{Context: \"a\"}");

        [Test]
        public async Task Should_properly_dispatch_metrics()
        {
            var sendOptions = new SendOptions();
            sendOptions.SetDestination(Conventions.EndpointNamingConvention(typeof(Receiver)));

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Receiver>(r => r.When(c => c.Send(new MetricReport { Data = Data }, sendOptions))).Done(c => c.Report != null)
                .Done(c => c.Report != null && c.RawDataProvider != null)
                .Run()
                .ConfigureAwait(false);

            Assert.IsNotNull(context.Report);
            Assert.True(JToken.DeepEquals(Data, context.Report.Data));

            var expected = new JObject
            {
                {"NServiceBus.Endpoints", new JArray(Data)}
            };

            Assert.True(JToken.DeepEquals(expected, context.RawDataProvider.CurrentRawData));
        }

        class Context : ScenarioContext
        {
            public MetricReport Report { get; set; }
            public RawDataProvider RawDataProvider { get; set; }
        }

        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    EndpointFactory.MakeMetricsReceiver(c);

                    c.Conventions().DefiningMessagesAs(t => t.FullName == "NServiceBus.Metrics.MetricReport");
                    c.AddActionOnMetricsReceived(o =>
                    {
                        ((Context)ScenarioContext).Report = o;
                    });

                    c.EnableFeature<RawDataConsumer>();
                });
            }

            class RawDataConsumer : Feature
            {
                public RawDataConsumer()
                {
                    DependsOn<MetricsReceiver>();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.RegisterStartupTask(BuildStartupTask);
                }

                DummyStartupTask BuildStartupTask(IBuilder builder)
                {
                    var context = builder.Build<Context>();
                    context.RawDataProvider = builder.Build<RawDataProvider>();
                    return new DummyStartupTask();
                }

                class DummyStartupTask : FeatureStartupTask
                {
                    protected override Task OnStart(IMessageSession session)
                    {
                        return Task.FromResult(0);
                    }

                    protected override Task OnStop(IMessageSession session)
                    {
                        return Task.FromResult(0);
                    }
                }
            }
        }
    }
}