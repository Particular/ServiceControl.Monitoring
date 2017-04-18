namespace ServiceControl.Monitoring.Http
{
    using System;
    using Nancy;
    using Newtonsoft.Json;

    /// <summary>
    /// Exposes ServiceControl.Monitoring metrics.
    /// </summary>
    public class MetricsApiModule : NancyModule
    {
        internal static readonly Uri DefaultHost = new Uri("http://localhost:1234");

        const string ModuleSubroute = "/metrics";
        const string RawMetricSubroute = "/raw";

        /// <summary>
        /// The default Uri to listen on for Raw data.
        /// </summary>
        public static readonly Uri DefaultRawUri = new Uri(DefaultHost + ModuleSubroute + RawMetricSubroute);

        /// <summary>
        /// Initializes the metric API module.
        /// </summary>
        /// <param name="dataProvider"></param>
        public MetricsApiModule(RawDataProvider dataProvider) : base(ModuleSubroute)
        {
            // consider hypermedia like listing of metrics
            // Get[""] = x => Response

            Get[RawMetricSubroute] = x => Response.AsText(dataProvider.CurrentRawData.ToString(Formatting.None), "application/json");

            //Get["/{metricName}/{aggregation?}"] = x => $"<p>{x.metricName}:{(string.IsNullOrEmpty(x.aggregation) ? "raw" : x.aggregation)}</p>";
        }
    }
}
