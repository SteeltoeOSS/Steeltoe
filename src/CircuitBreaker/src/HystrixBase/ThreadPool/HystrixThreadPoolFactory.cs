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

using Steeltoe.Common;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix.ThreadPool
{
    internal static class HystrixThreadPoolFactory
    {
        internal static IHystrixThreadPool GetInstance(IHystrixThreadPoolKey threadPoolKey, IHystrixThreadPoolOptions propertiesBuilder)
        {
            // get the key to use instead of using the object itself so that if people forget to implement equals/hashcode things will still work
            string key = threadPoolKey.Name;

            // if we get here this is the first time so we need to initialize
            return ThreadPools.GetOrAddEx(key, (k) => new HystrixThreadPoolDefault(threadPoolKey, propertiesBuilder));
        }

        private static object shutdownLock = new object();

        internal static ConcurrentDictionary<string, IHystrixThreadPool> ThreadPools { get; } = new ConcurrentDictionary<string, IHystrixThreadPool>();

        internal static void Shutdown()
        {
            lock (shutdownLock)
            {
                foreach (IHystrixThreadPool pool in ThreadPools.Values)
                {
                    pool.Dispose();
                }

                ThreadPools.Clear();
            }
        }
    }
}
