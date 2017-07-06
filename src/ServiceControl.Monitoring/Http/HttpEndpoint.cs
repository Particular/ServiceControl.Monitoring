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
    using Processing.RawData;
    using Processing.Snapshot;
    using Raw;

    class HttpEndpoint : Feature
    {
        public HttpEndpoint()
        {
            DependsOn<SnapshotMetricsFeature>();
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
                var buildstrapper = new Bootstrapper(
                    builder.BuildAll<ISnapshotDataProvider>(), 
                    builder.Build<DiagramDataProvider>(),
                    builder.Build<DurationsDataStore>());

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
            readonly IEnumerable<ISnapshotDataProvider> providers;
            readonly DiagramDataProvider diagramProvider;
            readonly DurationsDataStore durationsDataStore;

            public Bootstrapper(IEnumerable<ISnapshotDataProvider> providers, DiagramDataProvider diagramProvider, 
                DurationsDataStore durationsDataStore)
            {
                this.providers = providers;
                this.diagramProvider = diagramProvider;
                this.durationsDataStore = durationsDataStore;
            }

            protected override void ConfigureApplicationContainer(TinyIoCContainer container)
            {
                base.ConfigureApplicationContainer(container);
                container.Register(typeof(IEnumerable<ISnapshotDataProvider>), providers);
                container.Register(typeof(DiagramDataProvider), diagramProvider);
                container.Register(typeof(DurationsDataStore), durationsDataStore);
            }
            
        }
    }
}