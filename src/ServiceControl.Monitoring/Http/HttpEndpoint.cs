using NServiceBus.Features;
using System.Threading.Tasks;
using Nancy;
using Nancy.Hosting.Self;
using Nancy.TinyIoc;
using NServiceBus;
using NServiceBus.ObjectBuilder;

namespace ServiceControl.Monitoring.Http
{
    using System;
    using System.Configuration;
    using System.Linq;

    class HttpEndpoint : Feature
    {
        public const string AppConfigHostnameKey = "HttpEndpoint/Hostname";
        public const string AppConfigPort = "HttpEndpoint/Port";

        public HttpEndpoint()
        {
            DependsOn<MetricsReceiver>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var host = MetricsApiModule.DefaultHost;
            var hostname = GetValue(AppConfigHostnameKey);
            var port = GetValue(AppConfigPort);

            if (string.IsNullOrWhiteSpace(hostname) == false &&
                string.IsNullOrWhiteSpace(port) == false)
            {
                int portValue;
                if (int.TryParse(port, out portValue))
                {
                    host = new Uri(host + ":" + port);
                }
                else
                {
                    throw new Exception($"Http endpoint port is wrongly formatted. It should be a valid integer but it is '{port}'.");
                }
            }

            context.RegisterStartupTask(builder => BuildTask(builder, host));
        }

        static string GetValue(string key)
        {
            return (ConfigurationManager.AppSettings.GetValues(key) ?? new string[0]).FirstOrDefault();
        }

        FeatureStartupTask BuildTask(IBuilder builder, Uri host)
        {
            return new NancyTask(builder, host);
        }

        class NancyTask : FeatureStartupTask
        {
            NancyHost metricsEndpoint;

            public NancyTask(IBuilder builder, Uri host)
            {
                var buildstrapper = new Bootstrapper(builder.Build<RawDataProvider>());
                metricsEndpoint = new NancyHost(host, buildstrapper);
            }

            protected override Task OnStart(IMessageSession session)
            {
                metricsEndpoint?.Start();
                return Task.FromResult(0);
            }

            protected override Task OnStop(IMessageSession session)
            {
                metricsEndpoint?.Dispose();
                return Task.FromResult(0);
            }
        }

        /// <summary>
        /// Custom bootstrapper providing <see cref="RawDataProvider"/>.
        /// </summary>
        class Bootstrapper : DefaultNancyBootstrapper
        {
            readonly RawDataProvider provider;

            public Bootstrapper(RawDataProvider provider)
            {
                this.provider = provider;
            }

            protected override void ConfigureApplicationContainer(TinyIoCContainer container)
            {
                base.ConfigureApplicationContainer(container);
                container.Register(typeof(RawDataProvider), provider);
            }
        }
    }
}