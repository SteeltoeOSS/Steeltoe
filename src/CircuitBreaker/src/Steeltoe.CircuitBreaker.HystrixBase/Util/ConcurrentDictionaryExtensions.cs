// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.Util
{
    public static class ConcurrentDictionaryExtensions
    {
        public static V GetOrAddEx<K, V>(this ConcurrentDictionary<K, V> dict, K key, Func<K, V> factory)
        {
            if (dict.TryGetValue(key, out V value))
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
