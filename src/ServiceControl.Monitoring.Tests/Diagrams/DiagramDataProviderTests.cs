namespace ServiceControl.Monitoring.Tests.Diagrams
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using Monitoring.QueueLength;
    using Newtonsoft.Json.Linq;
    using NServiceBus;
    using NUnit.Framework;
    using Raw;

    [TestFixture]
    public class DiagramDataProviderTests
    {
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

            var endpointName = "Samples.Metrics.Tracing.Endpoint";
            var headers = new Dictionary<string, string> { { Headers.OriginatingEndpoint, endpointName} };
            var data = JObject.Parse(json);

            var diagramDataProvider = new DiagramDataProvider();

            diagramDataProvider.Consume(headers, data);

            var endpointData = diagramDataProvider.MonitoringData.Get(endpointName);

            var timestmap = DateTime.ParseExact("2017-06-09T10:10:03.9429Z", "yyyy-MM-dd'T'HH:mm:ss.ffff'Z'",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

            var index = Array.IndexOf(endpointData.Timestamps, timestmap);

            Assert.AreEqual(564.47f, endpointData.CriticalTime[index]);
            Assert.AreEqual(6.26f, endpointData.ProcessingTime[index]);
        }
    }
}