namespace NServiceBus.Metrics.AcceptanceTests
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using Features;
    using global::Newtonsoft.Json;
    using global::Newtonsoft.Json.Linq;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ObjectBuilder;
    using ServiceControl.Monitoring;
    using ServiceControl.Monitoring.Http;

    public class Endpoint_using_metrics_provider : NServiceBusAcceptanceTest
    {
        static readonly JObject Data = JObject.Parse("{Context: 'a'}");
        const string Testendpointname = "TestEndpointName";

        [Test]
        public async Task Should_report_metrics_via_http()
        {
            var sendOptions = new SendOptions();
            sendOptions.SetDestination(Conventions.EndpointNamingConvention(typeof(Receiver)));

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Receiver>()
                .Done(c => c.CrawlResult != null)
                .Run()
                .ConfigureAwait(false);

            Assert.IsNotNull(context.CrawlResult);

            var expected = new JObject
            {
                {
                    RawDataProvider.EndpointsKey, new JObject
                    {
                        {Testendpointname, Data}
                    }
                }
            };

            Assert.AreEqual(expected.ToString(Formatting.None), context.CrawlResult);
        }

        class Context : ScenarioContext
        {
            public string CrawlResult { get; set; }
        }

        class Receiver : EndpointConfigurationBuilder
        {
            public Receiver()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    EndpointFactory.MakeMetricsReceiver(c, Settings);
                    c.EnableFeature<FakeRawDataReporter>();
                });
            }

            class FakeRawDataReporter : Feature
            {
                public FakeRawDataReporter()
                {
                    DependsOn<MetricsReceiver>();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.RegisterStartupTask(BuildStartupTask);
                }

                HttpCrawler BuildStartupTask(IBuilder builder)
                {
                    var context = builder.Build<Context>();
                    var provider = builder.Build<RawDataProvider>();

                    provider.Consume(new MetricReportWithHeaders(Data, new Dictionary<string, string>
                    {
                        {Headers.OriginatingEndpoint, Testendpointname}
                    }));

                    return new HttpCrawler(context);
                }

                class HttpCrawler : FeatureStartupTask
                {
                    readonly Context context;
                    readonly CancellationTokenSource token;
                    Task crawler;

                    public HttpCrawler(Context context)
                    {
                        this.context = context;
                        token = new CancellationTokenSource();
                    }

                    protected override Task OnStart(IMessageSession session)
                    {
                        var uri = MetricsApiModule.DefaultRawUri;

                        crawler = Task.Factory.StartNew(async () =>
                        {
                            var client = new HttpClient();
                            while (token.IsCancellationRequested == false)
                            {
                                try
                                {
                                    context.CrawlResult = await client.GetStringAsync(uri).ConfigureAwait(false);
                                }
                                catch
                                {
                                    await Task.Delay(TimeSpan.FromSeconds(1));
                                }
                            }
                        });

                        return Task.FromResult(0);
                    }

                    protected override Task OnStop(IMessageSession session)
                    {
                        token.Cancel();
                        return crawler;
                    }
                }
            }
        }
    }
}