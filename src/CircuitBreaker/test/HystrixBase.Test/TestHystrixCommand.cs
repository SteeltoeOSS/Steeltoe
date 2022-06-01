// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using System;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public class TestHystrixCommand<T> : HystrixCommand<T>
{
    public ITestOutputHelper _output;

    public TestHystrixCommand(TestCommandBuilder builder)
        : base(
            builder.Owner,
            builder.DependencyKey,
            builder.ThreadPoolKey,
            builder.CircuitBreaker,
            builder.ThreadPool,
            builder.CommandPropertiesDefaults,
            builder.ThreadPoolPropertiesDefaults,
            builder.Metrics,
            builder.FallbackSemaphore,
            builder.ExecutionSemaphore,
            new TestOptionsFactory(),
            builder.ExecutionHook,
            null,
            null)
    {
        this.Builder = builder;
    }

    public TestHystrixCommand(TestCommandBuilder builder, HystrixCommandExecutionHook executionHook)
        : base(
            builder.Owner,
            builder.DependencyKey,
            builder.ThreadPoolKey,
            builder.CircuitBreaker,
            builder.ThreadPool,
            builder.CommandPropertiesDefaults,
            builder.ThreadPoolPropertiesDefaults,
            builder.Metrics,
            builder.FallbackSemaphore,
            builder.ExecutionSemaphore,
            new TestOptionsFactory(),
            executionHook,
            null,
            null)
    {
        this.Builder = builder;
    }

    public virtual TestCommandBuilder Builder { get; }

    public static TestCommandBuilder TestPropsBuilder()
    {
        return new TestCommandBuilder(ExecutionIsolationStrategy.THREAD);
    }

    public static TestCommandBuilder TestPropsBuilder(TestCircuitBreaker circuitBreaker)
    {
        return new TestCommandBuilder(ExecutionIsolationStrategy.THREAD).SetCircuitBreaker(circuitBreaker);
    }

    public static TestCommandBuilder TestPropsBuilder(ExecutionIsolationStrategy isolationStrategy)
    {
        return new TestCommandBuilder(isolationStrategy);
    }

    protected override T Run()
    {
        throw new NotImplementedException();
    }
}
