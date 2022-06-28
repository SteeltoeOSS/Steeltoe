// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

internal sealed class EventCommand : HystrixCommand<string>
{
    public EventCommand()
        : base(GetTestOptions())
    {
    }

    protected override string Run()
    {
        // _output?.WriteLine(Thread.CurrentThread.ManagedThreadId + " : In run()");
        throw new Exception("run_exception");
    }

    protected override string RunFallback()
    {
        try
        {
            // _output?.WriteLine(Thread.CurrentThread.ManagedThreadId + " : In fallback => " + ExecutionEvents)
            Time.WaitUntil(() => _token.IsCancellationRequested, 30000);
            _token.ThrowIfCancellationRequested();
        }
        catch (Exception)
        {
            // output.WriteLine(Thread.CurrentThread.ManagedThreadId + " : Interruption occurred")
        }

        // output.WriteLine(Thread.CurrentThread.ManagedThreadId + " : CMD Success Result")
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
