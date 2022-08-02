// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common;

namespace Steeltoe.CircuitBreaker.Hystrix;

public class HystrixCommandKeyDefault : HystrixKeyDefault, IHystrixCommandKey
{
    private static readonly ConcurrentDictionary<string, HystrixCommandKeyDefault> Intern = new();

    public static int CommandCount => Intern.Count;

    internal HystrixCommandKeyDefault(string name)
        : base(name)
    {
    }

    public static IHystrixCommandKey AsKey(string name)
    {
        return Intern.GetOrAddEx(name, k => new HystrixCommandKeyDefault(k));
    }
}
