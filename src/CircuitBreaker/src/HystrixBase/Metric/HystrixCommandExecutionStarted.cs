// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Metric;

public class HystrixCommandExecutionStarted : HystrixCommandEvent
{
    private readonly ExecutionIsolationStrategy _isolationStrategy;

    public override bool IsExecutionStart => true;

    public override bool IsExecutedInThread => _isolationStrategy == ExecutionIsolationStrategy.Thread;

    public override bool IsResponseThreadPoolRejected => false;

    public override bool IsCommandCompletion => false;

    public override bool DidCommandExecute => false;

    public int CurrentConcurrency { get; }

    public HystrixCommandExecutionStarted(IHystrixCommandKey commandKey, IHystrixThreadPoolKey threadPoolKey, ExecutionIsolationStrategy isolationStrategy,
        int currentConcurrency)
        : base(commandKey, threadPoolKey)
    {
        _isolationStrategy = isolationStrategy;
        CurrentConcurrency = currentConcurrency;
    }
}
