// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration;

internal static class ConfigurationDictionaryExtensions
{
    public static IEnumerable<string> Filter(this IDictionary<string, string?> configurationData, string keyPrefix, string keySuffix, string keyValue)
    {
        var results = new List<string>();

        foreach ((string key, string? value) in configurationData)
        {
            if (key.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase) && key.EndsWith(keySuffix, StringComparison.OrdinalIgnoreCase) &&
                value == keyValue)
            {
                string? parentPath = ConfigurationPath.GetParentPath(key);

                if (parentPath != null)
                {
                    results.Add(parentPath);
                }
            }
        }

        return results;
    }
}
