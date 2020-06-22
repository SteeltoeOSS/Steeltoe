// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
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

        private readonly RequestCacheKey _rcKey;

        private HystrixRequestCache(RequestCacheKey rcKey)
        {
            this._rcKey = rcKey;
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
                return new ValueCacheKey(_rcKey, cacheKey);
            }

            return null;
        }

        private class ValueCacheKey
        {
            private readonly RequestCacheKey _rvKey;
            private readonly string _valueCacheKey;

            public ValueCacheKey(RequestCacheKey rvKey, string valueCacheKey)
            {
                this._rvKey = rvKey;
                this._valueCacheKey = valueCacheKey;
            }

            public override int GetHashCode()
            {
                int prime = 31;
                int result = 1;
                result = (prime * result) + ((_rvKey == null) ? 0 : _rvKey.GetHashCode());
                result = (prime * result) + ((_valueCacheKey == null) ? 0 : _valueCacheKey.GetHashCode());
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
                if (_rvKey == null)
                {
                    if (other._rvKey != null)
                    {
                        return false;
                    }
                }
                else if (!_rvKey.Equals(other._rvKey))
                {
                    return false;
                }

                if (_valueCacheKey == null)
                {
                    if (other._valueCacheKey != null)
                    {
                        return false;
                    }
                }
                else if (!_valueCacheKey.Equals(other._valueCacheKey))
                {
                    return false;
                }

                return true;
            }
        }

        private class RequestCacheKey
        {
            private readonly short _type; // used to differentiate between Collapser/Command if key is same between them
            private readonly string _key;

            public RequestCacheKey(IHystrixCommandKey commandKey)
            {
                _type = 1;
                if (commandKey == null)
                {
                    this._key = null;
                }
                else
                {
                    this._key = commandKey.Name;
                }
            }

            public RequestCacheKey(IHystrixCollapserKey collapserKey)
            {
                _type = 2;
                if (collapserKey == null)
                {
                    this._key = null;
                }
                else
                {
                    this._key = collapserKey.Name;
                }
            }

            public override int GetHashCode()
            {
                int prime = 31;
                int result = 1;
                result = (prime * result) + ((_key == null) ? 0 : _key.GetHashCode());
                result = (prime * result) + _type;
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
                if (_type != other._type)
                {
                    return false;
                }

                if (_key == null)
                {
                    if (other._key != null)
                    {
                        return false;
                    }
                }
                else if (!_key.Equals(other._key))
                {
                    return false;
                }

                return true;
            }
        }
    }
}
