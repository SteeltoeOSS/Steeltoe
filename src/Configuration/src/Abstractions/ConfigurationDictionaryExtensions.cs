// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Steeltoe.Configuration;
internal static class ConfigurationDictionaryExtensions
{
    public static IEnumerable<string> Filter(this IDictionary<string, string> configData, string keyPrefix, string keySuffix, string keyValue)
    {
        List<string> results = new List<string>();
        foreach (var pair in configData)
        {
            if (pair.Key.StartsWith(keyPrefix) && pair.Key.EndsWith(keySuffix) && pair.Value == keyValue)
            {
                results.Add(ConfigurationPath.GetParentPath(pair.Key));
            }
        }

        return results;
    }

    public static IEnumerable<string> Filter(this IDictionary<string, string> configData, string keyPrefix)
    {
        List<string> results = new List<string>();
        foreach (var pair in configData)
        {
            if (pair.Key.StartsWith(keyPrefix))
            {
                results.Add(ConfigurationPath.GetParentPath(pair.Key));
            }
        }

        return results;
    }

    public static void ForEach(this IEnumerable<string> keys, Action<string> mapping)
    {
        foreach (var key in keys)
        {
            mapping(key);
        }
    }

}
