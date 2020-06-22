﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.Common;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    public class HystrixRequestCache
    {
        // the String key must be: HystrixRequestCache.prefix + cacheKey
        private static readonly ConcurrentDictionary<RequestCacheKey, HystrixRequestCache> Caches = new ConcurrentDictionary<RequestCacheKey, HystrixRequestCache>();

        private class HystrixRequestCacheVariable : HystrixRequestVariableDefault<ConcurrentDictionary<ValueCacheKey, object>>
        {
            public HystrixRequestCacheVariable()
                : base(() =>
                {
                    return new ConcurrentDictionary<ValueCacheKey, object>();
                })
            {
            }
        }

        private static readonly HystrixRequestCacheVariable RequestVariableForCache = new HystrixRequestCacheVariable();

        public static HystrixRequestCache GetInstance(IHystrixCommandKey key)
        {
            return GetInstance(new RequestCacheKey(key));
        }

        public static HystrixRequestCache GetInstance(IHystrixCollapserKey key)
        {
            return GetInstance(new RequestCacheKey(key));
        }

        private static HystrixRequestCache GetInstance(RequestCacheKey rcKey)
        {
            return Caches.GetOrAddEx(rcKey, (k) => new HystrixRequestCache(rcKey));
        }

        private readonly RequestCacheKey rcKey;

        private HystrixRequestCache(RequestCacheKey rcKey)
        {
            this.rcKey = rcKey;
        }

        public T Get<T>(string cacheKey)
        {
            ValueCacheKey key = GetRequestCacheKey(cacheKey);
            object result = null;
            if (key != null)
            {
                var cacheInstance = RequestVariableForCache.Value;
                /* look for the stored value */
                if (cacheInstance.TryGetValue(key, out result))
                {
                    return (T)result;
                }
            }

            return default(T);
        }

        public void Clear(string cacheKey)
        {
            ValueCacheKey key = GetRequestCacheKey(cacheKey);
            if (key != null)
            {
                /* remove this cache key */
                var cacheInstance = RequestVariableForCache.Value;
                cacheInstance.TryRemove(key, out object removed);
            }
        }

        internal T PutIfAbsent<T>(string cacheKey, T f)
        {
            ValueCacheKey key = GetRequestCacheKey(cacheKey);
            object result = null;
            if (key != null)
            {
                var cacheInstance = RequestVariableForCache.Value;
                result = cacheInstance.GetOrAdd(key, f);
                if (f.Equals(result))
                {
                    return default(T);
                }
                else
                {
                    return (T)result;
                }
            }

            return default(T);
        }

        private ValueCacheKey GetRequestCacheKey(string cacheKey)
        {
            if (cacheKey != null)
            {
                /* create the cache key we will use to retrieve/store that include the type key prefix */
                return new ValueCacheKey(rcKey, cacheKey);
            }

            return null;
        }

        private class ValueCacheKey
        {
            private readonly RequestCacheKey rvKey;
            private readonly string valueCacheKey;

            public ValueCacheKey(RequestCacheKey rvKey, string valueCacheKey)
            {
                this.rvKey = rvKey;
                this.valueCacheKey = valueCacheKey;
            }

            public override int GetHashCode()
            {
                int prime = 31;
                int result = 1;
                result = (prime * result) + ((rvKey == null) ? 0 : rvKey.GetHashCode());
                result = (prime * result) + ((valueCacheKey == null) ? 0 : valueCacheKey.GetHashCode());
                return result;
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }

                if (obj == null)
                {
                    return false;
                }

                if (GetType() != obj.GetType())
                {
                    return false;
                }

                ValueCacheKey other = (ValueCacheKey)obj;
                if (rvKey == null)
                {
                    if (other.rvKey != null)
                    {
                        return false;
                    }
                }
                else if (!rvKey.Equals(other.rvKey))
                {
                    return false;
                }

                if (valueCacheKey == null)
                {
                    if (other.valueCacheKey != null)
                    {
                        return false;
                    }
                }
                else if (!valueCacheKey.Equals(other.valueCacheKey))
                {
                    return false;
                }

                return true;
            }
        }

        private class RequestCacheKey
        {
            private readonly short type; // used to differentiate between Collapser/Command if key is same between them
            private readonly string key;

            public RequestCacheKey(IHystrixCommandKey commandKey)
            {
                type = 1;
                if (commandKey == null)
                {
                    this.key = null;
                }
                else
                {
                    this.key = commandKey.Name;
                }
            }

            public RequestCacheKey(IHystrixCollapserKey collapserKey)
            {
                type = 2;
                if (collapserKey == null)
                {
                    this.key = null;
                }
                else
                {
                    this.key = collapserKey.Name;
                }
            }

            public override int GetHashCode()
            {
                int prime = 31;
                int result = 1;
                result = (prime * result) + ((key == null) ? 0 : key.GetHashCode());
                result = (prime * result) + type;
                return result;
            }

            public override bool Equals(object obj)
            {
                if (this == obj)
                {
                    return true;
                }

                if (obj == null)
                {
                    return false;
                }

                if (GetType() != obj.GetType())
                {
                    return false;
                }

                RequestCacheKey other = (RequestCacheKey)obj;
                if (type != other.type)
                {
                    return false;
                }

                if (key == null)
                {
                    if (other.key != null)
                    {
                        return false;
                    }
                }
                else if (!key.Equals(other.key))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
