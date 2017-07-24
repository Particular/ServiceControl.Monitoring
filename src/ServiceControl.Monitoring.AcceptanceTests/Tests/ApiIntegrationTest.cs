namespace NServiceBus.Metrics.AcceptanceTests
{
    using System.Net.Http;
    using System.Net.Http.Headers;
    using NServiceBus.AcceptanceTests;
    using NUnit.Framework;

    public abstract class ApiIntegrationTest : NServiceBusAcceptanceTest
    {
        protected HttpClient httpClient;

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

        protected  string GetString(string url)
        {
            return httpClient.GetStringAsync(url).GetAwaiter().GetResult();
        }
    }
}