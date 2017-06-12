namespace ServiceControl.Monitoring.Tests.Diagrams
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Newtonsoft.Json.Linq;
    using NServiceBus;
    using NUnit.Framework;
    using Raw;

    [TestFixture]
    public class DiagramDataProviderTests
    {
        [SetUp]
        public void SetUp()
        {
            var dataProvider = new DiagramDataProvider();
            var endpointName = "Samples.Metrics.Tracing.Endpoint";

            var headers = new Dictionary<string, string>
            {
                {Headers.OriginatingEndpoint, endpointName}
            };

            store = data => dataProvider.Consume(headers, data);
            query = () => dataProvider.MonitoringData.Get(endpointName);
        }

        [Test]
        public void When_passed_correct_json_the_data_is_recorded()
        {
            const string json = @"{
                                    ""Timestamp"": ""2017-06-09T10:10:03.9429Z"",
                                    ""Timers"": [
                                      {
                                        ""Name"": ""Critical Time"",
                                        ""Histogram"": {
                                          ""Mean"": 564.47,
                                        }
                                      },
                                      {
                                        ""Name"": ""Processing Time"",
                                        ""Histogram"": {
                                          ""Mean"": 6.26,
                                        }
                                      }
                                    ]
                                  }";


            store(JObject.Parse(json));

            var endpointData = query();

            var timestmap = ParseDatetime("2017-06-09T10:10:03.9429Z");

            var index = Array.IndexOf(endpointData.Timestamps, timestmap);

            Assert.AreEqual(564.47f, endpointData.CriticalTime[index]);
            Assert.AreEqual(6.26f, endpointData.ProcessingTime[index]);
        }

        [Test]
        public void When_passed_susbset_of_possible_metrics_data_is_recorded()
        {
            const string json = @"{
                                    ""Timestamp"": ""2017-06-09T10:10:03.9429Z"",
                                    ""Timers"": [
                                      {
                                        ""Name"": ""Critical Time"",
                                        ""Histogram"": {
                                          ""Mean"": 564.47,
                                        }
                                      }
                                    ]
                                  }";

            store(JObject.Parse(json));

            var endpointData = query();

            var timestmap = ParseDatetime("2017-06-09T10:10:03.9429Z");

            var index = Array.IndexOf(endpointData.Timestamps, timestmap);

            Assert.AreEqual(564.47f, endpointData.CriticalTime[index]);
            Assert.AreEqual(null, endpointData.ProcessingTime[index]);
        }

        [Test]
        public void When_passed_metrics_with_unavailable_metric_value_data_is_recorded()
        {
            const string json = @"{
                                    ""Timestamp"": ""2017-06-09T10:10:03.9429Z"",
                                    ""Timers"": [
                                      {
                                        ""Name"": ""Critical Time"",
                                        ""Histogram"": {
                                          ""Mean"": null,
                                        }
                                      }
                                    ]
                                  }";

            store(JObject.Parse(json));

            var endpointData = query();

            var timestmap = ParseDatetime("2017-06-09T10:10:03.9429Z");

            var index = Array.IndexOf(endpointData.Timestamps, timestmap);

            Assert.AreEqual(null, endpointData.CriticalTime[index]);
        }

        static DateTime ParseDatetime(string text)
        {
            return DateTime.ParseExact(text, "yyyy-MM-dd'T'HH:mm:ss.ffff'Z'",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        }

        Action<JObject> store;
        Func<EndpointData> query;
    }
}