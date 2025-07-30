// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using System.Collections.Concurrent;

namespace Steeltoe.CircuitBreaker.Hystrix;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public interface IHystrixCommandGroupKey : IHystrixKey
{
}

// public class HystrixCommandGroupKeyDefault : HystrixKeyDefault<HystrixCommandGroupKeyDefault>, IHystrixCommandGroupKey
[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class HystrixCommandGroupKeyDefault : HystrixKeyDefault, IHystrixCommandGroupKey
{
    private static readonly ConcurrentDictionary<string, HystrixCommandGroupKeyDefault> Intern = new ();

    internal HystrixCommandGroupKeyDefault(string name)
        : base(name)
    {
    }

    public static IHystrixCommandGroupKey AsKey(string name)
    {
        return Intern.GetOrAddEx(name, k => new HystrixCommandGroupKeyDefault(k));
    }

    public static int GroupCount
    {
        get { return Intern.Count; }
    }
}