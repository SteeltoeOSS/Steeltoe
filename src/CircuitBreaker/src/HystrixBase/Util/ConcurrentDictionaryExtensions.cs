// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public static class ConcurrentDictionaryExtensions
    {
        public static V GetOrAddEx<K, V>(this ConcurrentDictionary<K, V> dict, K key, Func<K, V> factory)
        {
            if (dict.TryGetValue(key, out var value))
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
}
