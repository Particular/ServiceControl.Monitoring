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
    using System.Collections.Generic;
    using Raw;

    class HttpEndpoint : Feature
    {
        public HttpEndpoint()
        {
            DependsOn<RawMetricsFeature>();
            DependsOn<QueueLength.QueueLengthFeature>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            var settings = context.Settings.Get<Settings>();
            var hostname = settings.HttpHostName;
            var port = settings.HttpPort;

            int portValue;
            if (!int.TryParse(port, out portValue))
            {
                throw new Exception($"Http endpoint port is wrongly formatted. It should be a valid integer but it is '{port}'.");
            }

            if (string.IsNullOrEmpty(hostname))
            {
                throw new Exception("No host name provided.");
            }

            var host = new Uri($"http://{hostname}:{port}");
            context.RegisterStartupTask(builder => BuildTask(builder, host));
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
                var buildstrapper = new Bootstrapper(builder.BuildAll<IEndpointDataProvider>(), builder.Build<DiagramDataProvider>());
                var hostConfiguration = new HostConfiguration { RewriteLocalhost = false };
                metricsEndpoint = new NancyHost(host, buildstrapper, hostConfiguration);
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

        class Bootstrapper : DefaultNancyBootstrapper
        {
            readonly IEnumerable<IEndpointDataProvider> providers;
            readonly DiagramDataProvider diagramProvider;

            public Bootstrapper(IEnumerable<IEndpointDataProvider> providers, DiagramDataProvider diagramProvider)
            {
                this.providers = providers;
                this.diagramProvider = diagramProvider;
            }

            protected override void ConfigureApplicationContainer(TinyIoCContainer container)
            {
                base.ConfigureApplicationContainer(container);
                container.Register(typeof(IEnumerable<IEndpointDataProvider>), providers);
                container.Register(typeof(DiagramDataProvider), diagramProvider);
            }
            
        }
    }
}