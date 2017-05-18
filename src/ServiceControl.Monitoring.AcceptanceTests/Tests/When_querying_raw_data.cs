﻿namespace NServiceBus.Metrics.AcceptanceTests
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
    using ServiceControl.Monitoring.Raw;
    using Transport;

    public class When_querying_raw_data : NServiceBusAcceptanceTest
    {
        static string MonitoredEndpointName => Conventions.EndpointNamingConvention(typeof(MonitoredEndpoint));
        const string Data = "{Context: 'a'}";

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
                            MonitoredEndpointName, new JObject
                            {
                                {RawDataProvider.Name, JObject.Parse(Data)}
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
                    context.RegisterStartupTask(b => new MetricSenderTask(b.Build<IDispatchMessages>(), context.Settings.EndpointName(), Data, Conventions.EndpointNamingConvention(typeof(Receiver))));
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