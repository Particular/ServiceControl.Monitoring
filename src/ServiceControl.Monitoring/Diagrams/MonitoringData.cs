namespace ServiceControl.Monitoring.Raw
{
    using System.Collections;
    using System.Collections.Generic;

    public class MonitoringData
    {
        public Dictionary<string, Dictionary<string, DiagramData>> Endpoints;

        public MonitoringData()
        {
            Endpoints = new Dictionary<string, Dictionary<string, DiagramData>>();
        }
    }

    public class DiagramData
    {
        public SlidingBuffer<long> Data;

        public DiagramData()
        {
            Data = new SlidingBuffer<long>(10);
        }
    }

    public class Meter
    {
        public string Name;
        public long Count;
    }

    public struct Timer
    {
        public string Name;
        public long TotalTime;
    }

    public class SlidingBuffer<T> : IEnumerable<T>
    {
        readonly Queue<T> queue;
        readonly int maxCount;

        public SlidingBuffer(int maxCount)
        {
            this.maxCount = maxCount;
            queue = new Queue<T>(maxCount);
        }

        public void Add(T item)
        {
            if (queue.Count == maxCount)
                queue.Dequeue();
            queue.Enqueue(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return queue.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}