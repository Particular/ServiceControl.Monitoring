using NServiceBus.Features;
using System.Threading.Tasks;
using Nancy;
using Nancy.Hosting.Self;
using Nancy.TinyIoc;
using NServiceBus;
using NServiceBus.ObjectBuilder;

namespace ServiceControl.Monitoring.Http
{
    class HttpEndpoint : Feature
    {
        public HttpEndpoint()
        {
            DependsOn<MetricsReceiver>();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.RegisterStartupTask(BuildTask);
        }

        FeatureStartupTask BuildTask(IBuilder builder)
        {
            return new NancyTask(builder);
        }

        class NancyTask : FeatureStartupTask
        {
            NancyHost metricsEndpoint;

            public NancyTask(IBuilder builder)
            {
                var buildstrapper = new Bootstrapper(builder.Build<RawDataProvider>());
                metricsEndpoint = new NancyHost(MetricsApiModule.DefaultHost, buildstrapper);
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