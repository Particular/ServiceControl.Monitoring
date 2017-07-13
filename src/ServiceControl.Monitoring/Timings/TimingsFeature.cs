namespace ServiceControl.Monitoring.Timings
{
    using NServiceBus.Features;

    public class TimingsFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.RegisterSingleton(new ProcessingTimeStore());
            context.Container.RegisterSingleton(new CriticalTimeStore());
        }
    }
}