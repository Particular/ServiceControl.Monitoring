namespace ServiceControl.Monitoring.Raw
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading;

    public class MonitoringData
    {
        public MonitoringData(int historySize)
        {
            this.historySize = historySize;

            Endpoints = new ConcurrentDictionary<string, EndpointData>();
        }

        public EndpointData Get(string endpointName)
        {
            return Endpoints.GetOrAdd(endpointName, new EndpointData(historySize));
        }

        public ConcurrentDictionary<string, EndpointData> Endpoints;
        readonly int historySize;
    }

    public class EndpointData
    {
        public EndpointData(int size)
        {
            Timestamps = new DateTime[size];
            CriticalTime = new float?[size];
            ProcessingTime = new float?[size];

            this.size = size;
            head = 0;
        }

        public void Record(DateTime timestamp, float? criticalTime, float? processingTime)
        {
            Interlocked.Increment(ref head);

            var index = (head - 1) % size;

            Timestamps[index] = timestamp;
            CriticalTime[index] = criticalTime;
            ProcessingTime[index] = processingTime;
        }

        public DateTime[] Timestamps;
        public float?[] CriticalTime;
        public float?[] ProcessingTime;

        int head;
        int size;
    }
}