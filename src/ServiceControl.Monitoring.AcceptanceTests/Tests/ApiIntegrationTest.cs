namespace NServiceBus.Metrics.AcceptanceTests
{
    using System.Net.Http;
    using System.Net.Http.Headers;
    using AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NUnit.Framework;

    public abstract class ApiIntegrationTest : NServiceBusAcceptanceTest
    {
        [SetUp]
        public void Setup()
        {
            httpClient = new HttpClient();

            httpClient.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        [TearDown]
        public void TearDown()
        {
            httpClient?.Dispose();
        }

        protected string GetString(string url)
        {
            return httpClient.GetStringAsync(url).GetAwaiter().GetResult();
        }

        protected string MonitoredEndpointsUrl = "http://localhost:1234/monitored-endpoints";
        HttpClient httpClient;

        public class SampleMessage : IMessage
        {
        }

        protected class Context : ScenarioContext
        {
            public bool ReportReceived { get; set; }
        }
    }
}