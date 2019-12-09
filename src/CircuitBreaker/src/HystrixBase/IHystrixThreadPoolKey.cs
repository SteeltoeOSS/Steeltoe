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

using Steeltoe.CircuitBreaker.Util;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix
{
    /// <summary>
    /// A key to represent a <seealso cref="IHystrixThreadPool"/> for monitoring, metrics publishing, caching and other such uses.
    /// </summary>
    public interface IHystrixThreadPoolKey : IHystrixKey
    {
    }

    /// <summary>
    /// Default implementation of the interface
    /// </summary>
    public class HystrixThreadPoolKeyDefault : HystrixKeyDefault, IHystrixThreadPoolKey
    {
        private static readonly ConcurrentDictionary<string, HystrixThreadPoolKeyDefault> Intern = new ConcurrentDictionary<string, HystrixThreadPoolKeyDefault>();

        internal HystrixThreadPoolKeyDefault(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Retrieve (or create) an interned IHystrixThreadPoolKey instance for a given name.
        /// </summary>
        /// <param name="name"> thread pool name </param>
        /// <returns> IHystrixThreadPoolKey instance that is interned (cached) so a given name will always retrieve the same instance. </returns>
        public static IHystrixThreadPoolKey AsKey(string name)
        {
            return Intern.GetOrAddEx(name, k => new HystrixThreadPoolKeyDefault(k));
        }

        public static int ThreadPoolCount
        {
            get { return Intern.Count; }
        }
    }
}
