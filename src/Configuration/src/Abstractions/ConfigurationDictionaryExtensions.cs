// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration;

internal static class ConfigurationDictionaryExtensions
{
    public static IEnumerable<string> Filter(this IDictionary<string, string> configurationData, string keyPrefix, string keySuffix, string keyValue)
    {
        var results = new List<string>();

        foreach ((string key, string value) in configurationData)
        {
            if (key.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase) && key.EndsWith(keySuffix, StringComparison.OrdinalIgnoreCase) &&
                value == keyValue)
            {
                results.Add(ConfigurationPath.GetParentPath(key));
            }
        }

        return results;
    }

    public static IEnumerable<string> Filter(this IDictionary<string, string> configurationData, string keyPrefix)
    {
        return
            from pair in configurationData
            where pair.Key.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase)
            select ConfigurationPath.GetParentPath(pair.Key);
    }

    public static void ForEach(this IEnumerable<string> keys, Action<string> mapping)
    {
        foreach (string key in keys)
        {
            mapping(key);
        }
    }
}
