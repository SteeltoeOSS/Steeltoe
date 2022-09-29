// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public class TestCommandBuilder
{
    public IHystrixCommandGroupKey Owner { get; private set; } = CommandGroupForUnitTest.OwnerOne;
    public IHystrixCommandKey DependencyKey { get; private set; }
    public IHystrixThreadPoolKey ThreadPoolKey { get; private set; }
    public ICircuitBreaker CircuitBreaker { get; private set; }
    public IHystrixThreadPool ThreadPool { get; private set; }
    public IHystrixCommandOptions CommandPropertiesDefaults { get; private set; } = HystrixCommandOptionsTest.GetUnitTestOptions();
    public IHystrixThreadPoolOptions ThreadPoolPropertiesDefaults { get; private set; } = HystrixThreadPoolOptionsTest.GetUnitTestPropertiesBuilder();
    public HystrixCommandMetrics Metrics { get; private set; }
    public SemaphoreSlim FallbackSemaphore { get; private set; }
    public SemaphoreSlim ExecutionSemaphore { get; private set; }
    public TestableExecutionHook ExecutionHook { get; private set; } = new();

    public TestCommandBuilder(ExecutionIsolationStrategy isolationStrategy)
    {
        CommandPropertiesDefaults.ExecutionIsolationStrategy = isolationStrategy;
    }

    public TestCommandBuilder SetOwner(IHystrixCommandGroupKey owner)
    {
        Owner = owner;
        return this;
    }

    public TestCommandBuilder SetCommandKey(IHystrixCommandKey dependencyKey)
    {
        DependencyKey = dependencyKey;
        return this;
    }

    public TestCommandBuilder SetThreadPoolKey(IHystrixThreadPoolKey threadPoolKey)
    {
        ThreadPoolKey = threadPoolKey;
        return this;
    }

    public TestCommandBuilder SetCircuitBreaker(TestCircuitBreaker circuitBreaker)
    {
        CircuitBreaker = circuitBreaker;

        if (circuitBreaker != null)
        {
            Metrics = circuitBreaker.Metrics;
        }

        return this;
    }

    public TestCommandBuilder SetThreadPool(IHystrixThreadPool threadPool)
    {
        ThreadPool = threadPool;
        return this;
    }

    public TestCommandBuilder SetCommandOptionDefaults(IHystrixCommandOptions commandPropertiesDefaults)
    {
        CommandPropertiesDefaults = commandPropertiesDefaults;
        return this;
    }

    public TestCommandBuilder SetThreadPoolPropertiesDefaults(IHystrixThreadPoolOptions threadPoolPropertiesDefaults)
    {
        ThreadPoolPropertiesDefaults = threadPoolPropertiesDefaults;
        return this;
    }

    public TestCommandBuilder SetMetrics(HystrixCommandMetrics metrics)
    {
        Metrics = metrics;
        return this;
    }

    public TestCommandBuilder SetFallbackSemaphore(SemaphoreSlim fallbackSemaphore)
    {
        FallbackSemaphore = fallbackSemaphore;
        return this;
    }

    public TestCommandBuilder SetExecutionSemaphore(SemaphoreSlim executionSemaphore)
    {
        ExecutionSemaphore = executionSemaphore;
        return this;
    }

    public TestCommandBuilder SetExecutionHook(TestableExecutionHook executionHook)
    {
        ExecutionHook = executionHook;
        return this;
    }
}
