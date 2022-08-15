// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common;

namespace Steeltoe.CircuitBreaker.Hystrix;

/// <summary>
/// Default implementation of the interface.
/// </summary>
public class HystrixThreadPoolKeyDefault : HystrixKeyDefault, IHystrixThreadPoolKey
{
    private static readonly ConcurrentDictionary<string, HystrixThreadPoolKeyDefault> Intern = new();

    public static int ThreadPoolCount => Intern.Count;

    internal HystrixThreadPoolKeyDefault(string name)
        : base(name)
    {
    }

    /// <summary>
    /// Retrieve (or create) an interned IHystrixThreadPoolKey instance for a given name.
    /// </summary>
    /// <param name="name">
    /// thread pool name.
    /// </param>
    /// <returns>
    /// IHystrixThreadPoolKey instance that is interned (cached) so a given name will always retrieve the same instance.
    /// </returns>
    public static IHystrixThreadPoolKey AsKey(string name)
    {
        return Intern.GetOrAddEx(name, k => new HystrixThreadPoolKeyDefault(k));
    }
}
