namespace ServiceControl.Monitoring.Processing.RawData
{
    using global::NServiceBus;
    using global::NServiceBus.Features;

    public class RawDataFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(typeof(RawDataProvider), DependencyLifecycle.SingleInstance);
        }
    }
}