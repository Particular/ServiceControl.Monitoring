namespace ServiceControl.Monitoring.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public abstract class BreakdownRegistry<BreakdownT>
    {
        HashSet<BreakdownT> breakdowns = new HashSet<BreakdownT>();
        volatile Dictionary<string, IEnumerable<BreakdownT>> lookup = new Dictionary<string, IEnumerable<BreakdownT>>();
        object @lock = new object();

        Func<BreakdownT, string> endpointNameExtractor;

        protected BreakdownRegistry(Func<BreakdownT, string> endpointNameExtractor)
        {
            this.endpointNameExtractor = endpointNameExtractor;
        }

        public void Record(BreakdownT breakdown)
        {
            lock (@lock)
            {
                if (breakdowns.Add(breakdown))
                {
                    lookup = breakdowns.ToArray()
                        .GroupBy(b => endpointNameExtractor(b))
                        .ToDictionary(g => g.Key, g => (IEnumerable<BreakdownT>)g.Select(i => i).ToArray());
                }
            }
        }

        public IReadOnlyDictionary<string, IEnumerable<BreakdownT>> GetGroupedByEndpointName()
        {
            return lookup;
        }

        public IEnumerable<BreakdownT> GetForEndpointName(string endpointName)
        {
            IEnumerable<BreakdownT> endpointBreakdowns;

            if (lookup.TryGetValue(endpointName, out endpointBreakdowns))
            {
                return endpointBreakdowns;
            }

            return emptyResult;
        }

        static IEnumerable<BreakdownT> emptyResult = new BreakdownT[0];
    }
}