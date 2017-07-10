namespace ServiceControl.Monitoring.Timings
{
    using NServiceBus;
    using NServiceBus.Features;

    public class TimingsFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(typeof(ProcessingTimeStore), DependencyLifecycle.SingleInstance);
            context.Container.ConfigureComponent(typeof(CriticalTimeStore), DependencyLifecycle.SingleInstance);
        }
    }
}