namespace ServiceControl.Monitoring.Raw
{
    using NServiceBus;
    using NServiceBus.Features;

    /// <summary>
    /// Registers the <see cref="RawDataProvider"/>
    /// </summary>
    public class RawMetricsFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(typeof(RawDataProvider), DependencyLifecycle.SingleInstance);
        }
    }
}