namespace ServiceControl.Monitoring.QueueLength
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Features;
    public class QueueLength : Feature
    {
        protected override void Setup(FeatureConfigurationContext context)
        {
            context.RegisterStartupTask(b => new QueueLengthSnapshotting(b.Build<QueueLengthStore>()));
        }

    }

    public class QueueLengthSnapshotting : FeatureStartupTask
    {
        QueueLengthStore store;

        public QueueLengthSnapshotting(QueueLengthStore store)
        {
            this.store = store;
        }

        protected override Task OnStart(IMessageSession session)
        {
            snapshotter = Task.Run(async () =>
            {
                while (!stopTokenSource.Token.IsCancellationRequested)
                {
                    store.SnapshotCurrentQueueLengthEstimations(DateTime.UtcNow);

                    await Task.Delay(snapshotInterval, stopTokenSource.Token);
                }
            });

            return TaskEx.Completed;
        }

        protected override Task OnStop(IMessageSession session)
        {
            stopTokenSource.Cancel();

            return snapshotter;
        }

        CancellationTokenSource stopTokenSource = new CancellationTokenSource();
        TimeSpan snapshotInterval = TimeSpan.FromSeconds(1);
        Task snapshotter;
    }
}