﻿using NServiceBus.Features;
using System.Threading.Tasks;
using Nancy;
using Nancy.Hosting.Self;
using Nancy.TinyIoc;
using NServiceBus;
using NServiceBus.ObjectBuilder;

namespace ServiceControl.Monitoring.Http
{
    using System;
    using System.Linq;
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
            return new NancyTask(new Action<TinyIoCContainer>[]
            {
                c => c.Register(typeof(QueueLengthDataStore), builder.Build<QueueLengthDataStore>()),
                c => c.Register(typeof(ProcessingTimeStore), builder.Build<ProcessingTimeStore>()),
                c => c.Register(typeof(CriticalTimeStore), builder.Build<CriticalTimeStore>())
            }, host);
        }

        class NancyTask : FeatureStartupTask
        {
            NancyHost metricsEndpoint;

            public NancyTask(Action<TinyIoCContainer>[] containerRegistrations, Uri host)
            {
                var hostConfiguration = new HostConfiguration { RewriteLocalhost = false };

                metricsEndpoint = new NancyHost(host, new Bootstrapper(containerRegistrations), hostConfiguration);
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
            readonly Action<TinyIoCContainer>[] containerRegistrations;

            public Bootstrapper(Action<TinyIoCContainer>[] containerRegistrations)
            {
                this.containerRegistrations = containerRegistrations;
            }


            protected override void ConfigureApplicationContainer(TinyIoCContainer container)
            {
                base.ConfigureApplicationContainer(container);

                containerRegistrations.ToList().ForEach(cr => cr(container));
            }
            
        }
    }
}