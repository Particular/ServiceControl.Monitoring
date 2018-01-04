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

            value = tags.FirstOrDefault(t => t.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

            // ReSharper disable once UseNullPropagation
            if (value != null)
            {
                value = value.Substring(prefix.Length).Trim();
            }

            return value != null;
        }
    }
}
