namespace ServiceControl.Monitoring.QueueLength
{
    using NServiceBus;
    using NServiceBus.Features;

    class QueueLengthFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.Container.ConfigureComponent(b => new QueueLengthDataConsumer(new QueueLengthCalculator()), DependencyLifecycle.SingleInstance);
        }
    }
}