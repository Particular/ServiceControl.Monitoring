using System;
using System.Collections.Generic;
using System.Linq;

namespace ServiceControl.Monitoring.QueueLength
{
    static class TagExtensions
    {
        public static string GetTagValue(this IEnumerable<string> tags, string key)
        {
            string result;
            if (!TryGetTagValue(tags, key, out result))
            {
                throw new Exception($"Tag {key} not found.");
            }
            return result;
        }

        public static bool TryGetTagValue(this IEnumerable<string> tags, string key, out string value)
        {
            var prefix = $"{key}:";

            value = tags
                .Where(t => t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Substring(key.Length + 1).Trim())
                .FirstOrDefault();

            return value != null;
        }
    }
}
