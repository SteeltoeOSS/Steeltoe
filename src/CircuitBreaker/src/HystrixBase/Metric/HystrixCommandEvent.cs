// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Metric;

public abstract class HystrixCommandEvent : IHystrixEvent
{
    protected HystrixCommandEvent(IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey)
    {
        CommandKey = commandKey;
        ThreadPoolKey = threadPoolKey;
    }

    public static Func<HystrixCommandEvent, bool> FilterCompletionsOnly { get; } = commandEvent => commandEvent.IsCommandCompletion;

    public static Func<HystrixCommandEvent, bool> FilterActualExecutions { get; } = commandEvent => commandEvent.DidCommandExecute;

    public virtual IHystrixCommandKey CommandKey { get; }

    public virtual IHystrixThreadPoolKey ThreadPoolKey { get; }

    public abstract bool IsExecutionStart { get; }

    public abstract bool IsExecutedInThread { get; }

    public abstract bool IsResponseThreadPoolRejected { get; }

    public abstract bool IsCommandCompletion { get; }

    public abstract bool DidCommandExecute { get; }
}
