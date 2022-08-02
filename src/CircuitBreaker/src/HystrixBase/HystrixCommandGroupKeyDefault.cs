// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Steeltoe.Common;

namespace Steeltoe.CircuitBreaker.Hystrix;

public class HystrixCommandGroupKeyDefault : HystrixKeyDefault, IHystrixCommandGroupKey
{
    private static readonly ConcurrentDictionary<string, HystrixCommandGroupKeyDefault> Intern = new();

    public static int GroupCount => Intern.Count;

    internal HystrixCommandGroupKeyDefault(string name)
        : base(name)
    {
    }

    public static IHystrixCommandGroupKey AsKey(string name)
    {
        return Intern.GetOrAddEx(name, k => new HystrixCommandGroupKeyDefault(k));
    }
}
