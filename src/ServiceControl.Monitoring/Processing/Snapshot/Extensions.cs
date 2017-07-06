namespace ServiceControl.Monitoring.Processing.Snapshot
{
    using System.Collections.Generic;
    using NServiceBus;

    static class Extensions
    {
        public static string GetOriginatingEndpoint(this IReadOnlyDictionary<string, string> headers)
        {
            string name;
            headers.TryGetValue(Headers.OriginatingEndpoint, out name);
            return name ?? "";
        }
    }
}