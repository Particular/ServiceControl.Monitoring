namespace ServiceControl.Monitoring.QueueLength
{
    using NServiceBus;
    using NServiceBus.Features;

    class QueueLengthFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(b => new QueueLengthDataStore(new QueueLengthCalculator()), DependencyLifecycle.SingleInstance);
        }
    }
}