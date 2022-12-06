// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class EventCommand : HystrixCommand<string>
{
    public EventCommand()
        : base(GetTestOptions())
    {
    }

    protected override string Run()
    {
        throw new Exception("run_exception");
    }

    protected override string RunFallback()
    {
        try
        {
            Time.WaitUntil(() => Token.IsCancellationRequested, 30000);
            Token.ThrowIfCancellationRequested();
        }
        catch (Exception)
        {
            // Intentionally left empty.
        }

        return "fallback";
    }

    private static HystrixCommandOptions GetTestOptions()
    {
        var options = new HystrixCommandOptions
        {
            GroupKey = HystrixCommandGroupKeyDefault.AsKey("eventGroup"),
            FallbackIsolationSemaphoreMaxConcurrentRequests = 3
        };

        return options;
    }
}
