namespace ServiceControl.Monitoring.Raw
{
    using NServiceBus;
    using NServiceBus.Features;

    /// <summary>
    /// Registers the <see cref="DiagramDataProvider"/>
    /// </summary>
    public class DiagramFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(typeof(DiagramDataProvider), DependencyLifecycle.SingleInstance);
        }
    }
}