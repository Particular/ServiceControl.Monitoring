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
    using QueueLength;
    using Timings;

    class HttpEndpoint : Feature
    {
        public HttpEndpoint()
        {
            DependsOn<TimingsFeature>();
            DependsOn<QueueLengthFeature>();
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
                var bootstrapper = new Bootstrapper(
                    builder.Build<QueueLengthDataStore>(), 
                    builder.Build<ProcessingTimeStore>(),
                    builder.Build<CriticalTimeStore>());

                var hostConfiguration = new HostConfiguration { RewriteLocalhost = false };
                metricsEndpoint = new NancyHost(host, bootstrapper, hostConfiguration);
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
            readonly QueueLengthDataStore providers;
            readonly ProcessingTimeStore processingTimeStore;
            readonly CriticalTimeStore criticalTimeStore;

            public Bootstrapper(QueueLengthDataStore providers, ProcessingTimeStore processingTimeStore, CriticalTimeStore criticalTimeStore)
            {
                this.providers = providers;
                this.processingTimeStore = processingTimeStore;
                this.criticalTimeStore = criticalTimeStore;
            }

            protected override void ConfigureApplicationContainer(TinyIoCContainer container)
            {
                base.ConfigureApplicationContainer(container);
                container.Register(typeof(QueueLengthDataStore), providers);
                container.Register(typeof(ProcessingTimeStore), processingTimeStore);
                container.Register(typeof(CriticalTimeStore), criticalTimeStore);
            }
            
        }
    }
}