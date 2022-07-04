// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class AsyncCacheableCommand : HystrixCommand<object>
{
    private readonly AtomicBoolean _cancelled = new (false);

    public AsyncCacheableCommand(string arg)
        : base(new HystrixCommandOptions { GroupKey = HystrixCommandGroupKeyDefault.AsKey("ASYNC") })
    {
        CacheKey = arg;
    }

    public bool IsCancelled
    {
        get { return _cancelled.Value; }
    }

    protected override object Run()
    {
        try
        {
            Time.WaitUntil(() => Token.IsCancellationRequested, 500);
            Token.ThrowIfCancellationRequested();
            return true;
        }
        catch (Exception)
        {
            _cancelled.Value = true;
            throw;
        }
    }

    protected override string CacheKey { get; }
}
