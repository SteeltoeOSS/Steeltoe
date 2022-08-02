// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Steeltoe.Common;

public static class ConcurrentDictionaryExtensions
{
    public static TValue GetOrAddEx<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> factory)
    {
        if (dict.TryGetValue(key, out TValue value))
        {
            return value;
        }

        lock (dict)
        {
            if (dict.TryGetValue(key, out value))
            {
                return value;
            }

            return dict.GetOrAdd(key, factory);
        }
    }
}
