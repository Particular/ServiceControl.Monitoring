namespace ServiceControl.Monitoring.Processing.Snapshot
{
    using NServiceBus;
    using NServiceBus.Features;

    /// <summary>
    /// Registers the <see cref="SnapshotDataProvider"/>
    /// </summary>
    public class SnapshotMetricsFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(typeof(SnapshotDataProvider), DependencyLifecycle.SingleInstance);
        }
    }
}