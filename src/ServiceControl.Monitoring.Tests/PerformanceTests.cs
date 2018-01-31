﻿namespace ServiceControl.Monitoring.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Dynamic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using HdrHistogram;
    using Http.Diagrams;
    using Messaging;
    using Monitoring.Infrastructure;
    using Monitoring.QueueLength;
    using Nancy;
    using NUnit.Framework;
    using QueueLength;
    using Timings;

    [Category("Performance")]
    public class PerformanceTests
    {
        EndpointRegistry endpointRegistry;
        MessageTypeRegistry messageTypeRegistry;
        CriticalTimeStore criticalTimeStore;
        ProcessingTimeStore processingTimeStore;
        RetriesStore retriesStore;
        QueueLengthStore queueLengthStore;
        Func<Task> GetMonitoredEndpoints;
        Func<string, Task> GetMonitoredSingleEndpoint;
        EndpointInstanceActivityTracker activityTracker;

        [SetUp]
        public void Setup()
        {
            endpointRegistry = new EndpointRegistry();
            criticalTimeStore = new CriticalTimeStore();
            processingTimeStore = new ProcessingTimeStore();
            retriesStore = new RetriesStore();
            queueLengthStore = new QueueLengthStore(new QueueLengthCalculator());

            var settings = new Settings { EndpointUptimeGracePeriod = TimeSpan.FromMinutes(5) };
            activityTracker = new EndpointInstanceActivityTracker(settings);

            messageTypeRegistry = new MessageTypeRegistry();

            var instanceMetricStores = new IProvideBreakdownBy<EndpointInstanceId>[]
            {
                criticalTimeStore,
                processingTimeStore,
                retriesStore,
                queueLengthStore
            };

            var messageTypeStores = new IProvideBreakdownBy<EndpointMessageType>[]
            {
                criticalTimeStore,
                processingTimeStore,
                retriesStore
            };

            var monitoredEndpointsModule = new MonitoredEndpointsModule(instanceMetricStores, messageTypeStores, endpointRegistry, activityTracker, messageTypeRegistry)
            {
                Context = new NancyContext() { Request = new Request("Get", "/monitored-endpoints", "HTTP") }
            };

            var dictionary = monitoredEndpointsModule.Routes.ToDictionary(r => r.Description.Path, r => r.Action);

            GetMonitoredEndpoints = () => dictionary["/monitored-endpoints"](new object(), new CancellationToken(false));
            GetMonitoredSingleEndpoint = endpointName => dictionary["/monitored-endpoints/{endpointName}"](new { EndpointName = endpointName }.ToDynamic(), new CancellationToken());
        }

        [TestCase(10, 5, 4)]
        [TestCase(1000, 5, 4)]
        public async Task TestQueueLengthStore(int numberOfEndpoints, int numberOfReceivedSessionsPerEndpoint, int numberOfSentSessionsPerEndpoint)
        {
            const int totalQueryCount = 200;
            var snapshotEvery = TimeSpan.FromMilliseconds(10);

            var sessionCount = numberOfEndpoints * numberOfSentSessionsPerEndpoint;
            var allSessions = Enumerable.Range(0, sessionCount).Select(i => $"Session-{i}").ToArray();
            var allEndpoints = Enumerable.Range(0, numberOfEndpoints).Select(i => $"Endpoint-{i}").ToArray();

            var endpointReporters = new Task<LongHistogram>[allEndpoints.Length];

            var source = new CancellationTokenSource();

            var random = new Random();
            for (var i = 0; i < allEndpoints.Length; i++)
            {
                var sentSessions = Enumerable.Range(i * numberOfSentSessionsPerEndpoint, numberOfSentSessionsPerEndpoint)
                    .Select(index => allSessions[index])
                    .ToArray();

                var receivedSessions = Enumerable.Range(0, numberOfReceivedSessionsPerEndpoint)
                    .Select(_ => random.Next(sessionCount))
                    .Select(si => allSessions[si]).ToArray();

                endpointReporters[i] = BuildQueueLengthReporter(allEndpoints[i], receivedSessions, sentSessions, queueLengthStore, source);
            }

            var snapshoter = Task.Run(async () =>
            {
                for (var i = 0; i < totalQueryCount; i++)
                {
                    queueLengthStore.SnapshotCurrentQueueLengthEstimations(DateTime.UtcNow);
                    await Task.Delay(snapshotEvery);
                }
            });
            var histogram = CreateTimeHistogram();

            while (snapshoter.IsCompleted == false)
            {
                var start = Stopwatch.GetTimestamp();
                await GetMonitoredEndpoints().ConfigureAwait(false);
                var elapsed = Stopwatch.GetTimestamp() - start;
                histogram.RecordValue(elapsed);
            }

            source.Cancel();
            await Task.WhenAll(endpointReporters).ConfigureAwait(false);

            var reportFinalHistogram = MergeHistograms(endpointReporters);

            Report("Querying", histogram, TimeSpan.FromMilliseconds(10));
            Report("Reporters", reportFinalHistogram, TimeSpan.FromMilliseconds(10));
        }

        static Task<LongHistogram> BuildQueueLengthReporter(string endpointName, string[] receivedSessions, string[] sentSessions, QueueLengthStore store, CancellationTokenSource source)
        {
            var counters = sentSessions.Select(ss => new MessageBuilder.Counter(ss, 10)).ToArray();
            var gauges = receivedSessions.Select(ss => new MessageBuilder.Gauge(10, ss)).ToArray();

            var message = MessageBuilder.BuildMessage(counters, gauges);
            var endpointInstanceId = new EndpointInstanceId(endpointName, string.Empty);

            return Task.Run(async () =>
            {
                var histogram = CreateTimeHistogram();
                while (source.IsCancellationRequested == false)
                {
                    var start = Stopwatch.GetTimestamp();
                    store.Store(endpointInstanceId, message);
                    var elapsed = Stopwatch.GetTimestamp() - start;
                    histogram.RecordValue(elapsed);

                    await Task.Delay(TimeSpan.FromMilliseconds(100)).ConfigureAwait(false);
                }
                return histogram;
            });
        }

        [TestCase(10, 10, 100, 1000, 100, 1000)]
        public async Task GetMonitoredEndpointsQueryTest(int numberOfEndpoints, int numberOfInstances, int sendReportEvery, int numberOfEntriesInReport, int queryEveryInMilliseconds, int numberOfQueries)
        {
            var instances = BuildInstances(numberOfEndpoints, numberOfInstances);
            foreach (var instance in instances)
            {
                endpointRegistry.Record(instance);
            }

            var source = new CancellationTokenSource();

            var reporters =
                new[]
                {
                    BuildReporters(sendReportEvery, numberOfEntriesInReport, instances, source, (e, i) => criticalTimeStore.Store(e, i, EndpointMessageType.Unknown(i.EndpointName))),
                    BuildReporters(sendReportEvery, numberOfEntriesInReport, instances, source, (e, i) => processingTimeStore.Store(e, i, EndpointMessageType.Unknown(i.EndpointName))),
                    BuildReporters(sendReportEvery, numberOfEntriesInReport, instances, source, (e, i) => retriesStore.Store(e, i, EndpointMessageType.Unknown(i.EndpointName)))
                }.SelectMany(i => i).ToArray();

            var histogram = CreateTimeHistogram();

            for (var i = 0; i < numberOfQueries; i++)
            {
                var start = Stopwatch.GetTimestamp();
                await GetMonitoredEndpoints().ConfigureAwait(false);
                var elapsed = Stopwatch.GetTimestamp() - start;
                histogram.RecordValue(elapsed);

                await Task.Delay(queryEveryInMilliseconds);
            }

            source.Cancel();
            await Task.WhenAll(reporters).ConfigureAwait(false);

            var reportFinalHistogram = MergeHistograms(reporters);

            Report("Querying", histogram, TimeSpan.FromMilliseconds(150));
            Report("Reporters", reportFinalHistogram, TimeSpan.FromMilliseconds(20));
        }

        [TestCase(10, 100, 100, 1000, 100, 1000)]
        public async Task GetMonitoredSingleEndpointQueryTest(int numberOfInstances, int numberOfMessageTypes, int sendReportEvery, int numberOfEntriesInReport, int queryEveryInMilliseconds, int numberOfQueries)
        {
            var instances = BuildInstances(1, numberOfInstances);
            foreach (var instance in instances)
            {
                endpointRegistry.Record(instance);
            }

            var endpointName = instances.First().EndpointName;

            for (var i = 0; i < numberOfMessageTypes; i++)
            {
                var messageType = new Guid(i, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0).ToString();
                messageTypeRegistry.Record(new EndpointMessageType(endpointName, messageType));
            }

            var source = new CancellationTokenSource();

            var messageTypes = messageTypeRegistry.GetForEndpointName(endpointName).ToArray();
            long counter = 0;
            Func<EndpointMessageType> getter = () =>
            {
                var value = Interlocked.Increment(ref counter) % messageTypes.Length;
                return messageTypes[value];
            };

            var reporters =
                new[]
                {
                    BuildReporters(sendReportEvery, numberOfEntriesInReport, instances, source, (e, i) => criticalTimeStore.Store(e, i, getter())),
                    BuildReporters(sendReportEvery, numberOfEntriesInReport, instances, source, (e, i) => processingTimeStore.Store(e, i, getter())),
                    BuildReporters(sendReportEvery, numberOfEntriesInReport, instances, source, (e, i) => retriesStore.Store(e, i, getter()))
                }.SelectMany(i => i).ToArray();

            var histogram = CreateTimeHistogram();

            for (var i = 0; i < numberOfQueries; i++)
            {
                var start = Stopwatch.GetTimestamp();
                await GetMonitoredSingleEndpoint(endpointName).ConfigureAwait(false);
                var elapsed = Stopwatch.GetTimestamp() - start;
                histogram.RecordValue(elapsed);

                await Task.Delay(queryEveryInMilliseconds).ConfigureAwait(false);
            }

            source.Cancel();
            await Task.WhenAll(reporters).ConfigureAwait(false);

            var reportFinalHistogram = MergeHistograms(reporters);

            Report("Querying", histogram, TimeSpan.FromMilliseconds(25));
            Report("Reporters", reportFinalHistogram, TimeSpan.FromMilliseconds(20));
        }

        static IEnumerable<Task<LongHistogram>> BuildReporters(int sendReportEvery, int numberOfEntriesInReport, EndpointInstanceId[] instances, CancellationTokenSource source, Action<RawMessage.Entry[], EndpointInstanceId> store)
        {
            return instances
                .Select(instance => StartReporter(sendReportEvery, numberOfEntriesInReport, source, instance, store))
                .ToArray();
        }

        static Task<LongHistogram> StartReporter(int sendReportEvery, int numberOfEntriesInReport, CancellationTokenSource source, EndpointInstanceId instance, Action<RawMessage.Entry[], EndpointInstanceId> store)
        {
            return Task.Run(async () =>
            {
                var entries = new RawMessage.Entry[numberOfEntriesInReport];
                var histogram = CreateTimeHistogram();

                while (source.IsCancellationRequested == false)
                {
                    var now = DateTime.UtcNow;

                    for (var i = 0; i < entries.Length; i++)
                    {
                        entries[i].DateTicks = now.AddMilliseconds(100 * i).Ticks;
                        entries[i].Value = i;
                    }

                    var start = Stopwatch.GetTimestamp();
                    store(entries, instance);
                    var elapsed = Stopwatch.GetTimestamp() - start;
                    histogram.RecordValue(elapsed);

                    await Task.Delay(sendReportEvery).ConfigureAwait(false);
                }

                return histogram;
            });
        }

        static EndpointInstanceId[] BuildInstances(int numberOfEndpoints, int numberOfInstances)
        {
            var instances = new List<EndpointInstanceId>();
            for (var i = 0; i < numberOfEndpoints; i++)
            {
                for (var j = 0; j < numberOfInstances; j++)
                {
                    instances.Add(new EndpointInstanceId(i.ToString(), j.ToString()));
                }
            }
            return instances.ToArray();
        }

        static LongHistogram CreateTimeHistogram()
        {
            return new LongHistogram(TimeStamp.Hours(1), 3);
        }

        static LongHistogram MergeHistograms(IEnumerable<Task<LongHistogram>> endpointReporters)
        {
            var result = CreateTimeHistogram();
            foreach (var endpointReporter in endpointReporters)
            {
                result.Add(endpointReporter.Result);
            }
            return result;
        }

        static void Report(string name, LongHistogram histogram, TimeSpan? maximumMean)
        {
            Console.Out.WriteLine($"Histogram for {name}:");
            histogram.OutputPercentileDistribution(Console.Out, outputValueUnitScalingRatio: OutputScalingFactor.TimeStampToMilliseconds, percentileTicksPerHalfDistance: 1);
            Console.Out.WriteLine();

            if (maximumMean != null)
            {
                var max = maximumMean.Value;
                var actualMean = TimeSpan.FromMilliseconds(histogram.GetValueAtPercentile(50) / OutputScalingFactor.TimeStampToMilliseconds);

                Assert.LessOrEqual(actualMean, max, $"The actual mean for {name} was '{actualMean}' and was bigger than maximum allowed mean '{max}'.");
            }
        }
    }

    public static class DynamicExtensions
    {
        public static dynamic ToDynamic<T>(this T obj)
        {
            IDictionary<string, object> expando = new ExpandoObject();

            foreach (var propertyInfo in typeof(T).GetProperties())
            {
                var currentValue = propertyInfo.GetValue(obj);
                expando.Add(propertyInfo.Name, currentValue);
            }
            return (ExpandoObject) expando;
        }
    }
}
