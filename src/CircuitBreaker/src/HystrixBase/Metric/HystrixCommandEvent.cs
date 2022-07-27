// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric;

public abstract class HystrixCommandEvent : IHystrixEvent
{
    private readonly IHystrixCommandKey _commandKey;
    private readonly IHystrixThreadPoolKey _threadPoolKey;

    protected HystrixCommandEvent(IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey)
    {
        _commandKey = commandKey;
        _threadPoolKey = threadPoolKey;
    }

    public static Func<HystrixCommandEvent, bool> FilterCompletionsOnly { get; } = (commandEvent) =>
    {
        return commandEvent.IsCommandCompletion;
    };

    public static Func<HystrixCommandEvent, bool> FilterActualExecutions { get; } = (commandEvent) =>
    {
        return commandEvent.DidCommandExecute;
    };

    public virtual IHystrixCommandKey CommandKey
    {
        get { return _commandKey; }
    }

    public virtual IHystrixThreadPoolKey ThreadPoolKey
    {
        get { return _threadPoolKey; }
    }

    public abstract bool IsExecutionStart { get; }

    public abstract bool IsExecutedInThread { get; }

    public abstract bool IsResponseThreadPoolRejected { get; }

    public abstract bool IsCommandCompletion { get; }

    public abstract bool DidCommandExecute { get; }
}