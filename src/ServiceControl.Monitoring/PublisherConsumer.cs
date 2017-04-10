namespace ServiceControl.Monitoring
{
    namespace ServiceControl.Monitoring
    {
        using System;
        using System.Collections.Concurrent;
        using System.Collections.Generic;
        using System.Linq;

        public class PublisherConsumer<T>
        {
            readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();
            volatile List<Action<T>> observers = new List<Action<T>>();

            public void Publish(T o)
            {
                queue.Enqueue(o);
            }

            public void Add(Action<T> observer)
            {
                lock (queue)
                {
                    var newObservers = observers.ToList();
                    newObservers.Add(observer);
                    observers = newObservers;
                }
            }

            public bool TryDispatch()
            {
                T o;
                if (queue.TryDequeue(out o) == false)
                {
                    return false;
                }

                foreach (var observer in observers)
                {
                    observer(o);
                }

                return true;
            }
        }
    }
}