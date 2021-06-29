// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
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
