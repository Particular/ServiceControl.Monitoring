namespace ServiceControl.Monitoring
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Features;
    using NServiceBus.Metrics;
    using ServiceControl.Monitoring;

    /// <summary>
    /// Registers a singleton of <see cref="PublisherConsumer{JsonMetricsContext}"/>  as well as a task calling <see cref="PublisherConsumer{T}.TryDispatch"/> periodically.
    /// </summary>
    public class MetricsReceiver : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            var monitor = new PublisherConsumer<MetricReportWithHeaders>();
            context.Container.RegisterSingleton(monitor);
            context.RegisterStartupTask(RunMonitor(monitor));

            List<Action<MetricReportWithHeaders>> actions;
            if (context.Settings.TryGet(out actions))
            {
                foreach (var action in actions)
                {
                    monitor.Add(action);
                }
            }

            var provider = new RawDataProvider();
            context.Container.RegisterSingleton(provider);

            monitor.Add(provider.Consume);
            
            //TODO: register HTTP endpoint here with monitorContext
            // monitor.Add();
        }

        FeatureStartupTask RunMonitor(PublisherConsumer<MetricReportWithHeaders> monitor)
        {
            return new MonitorRunner(monitor);
        }

        class MonitorRunner : FeatureStartupTask
        {
            readonly PublisherConsumer<MetricReportWithHeaders> monitor;
            CancellationTokenSource cancellationTokenSource;
            Task task;

            public MonitorRunner(PublisherConsumer<MetricReportWithHeaders> monitor)
            {
                this.monitor = monitor;
            }

            protected override Task OnStart(IMessageSession session)
            {
                cancellationTokenSource = new CancellationTokenSource();
                task = Task.Factory.StartNew(async () =>
                {
                    while (cancellationTokenSource.IsCancellationRequested == false)
                    {
                        if (monitor.TryDispatch() == false)
                        {
                            await Task.Delay(TimeSpan.FromSeconds(1));
                        }
                    }
                }, cancellationTokenSource.Token);

                return Task.FromResult(0);
            }

            protected override Task OnStop(IMessageSession session)
            {
                cancellationTokenSource.Cancel();
                return task;
            }
        }
    }
}