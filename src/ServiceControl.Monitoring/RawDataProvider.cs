﻿namespace ServiceControl.Monitoring
{
    using System.Collections.Concurrent;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using NServiceBus.Metrics;

    public class RawDataProvider
    {
        public JObject CurrentRawData
        {
            get
            {
                var properties = contexts.Select(pair => pair.Value).ToList();
                var endpoints = new JArray(properties);

                return new JObject
                {
                    {"NServiceBus.Endpoints", endpoints}
                };
            }
        }

        internal void Consume(MetricReport report)
        {
            var data = report.Data;
            var name = data["Context"].Value<string>();
            contexts.AddOrUpdate(name, data, (context, currentData) => data);
        }

        ConcurrentDictionary<string, JObject> contexts = new ConcurrentDictionary<string, JObject>();
    }
}