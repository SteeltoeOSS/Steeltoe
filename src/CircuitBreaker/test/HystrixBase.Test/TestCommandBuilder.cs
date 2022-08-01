// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public class TestCommandBuilder
{
    public IHystrixCommandGroupKey Owner = CommandGroupForUnitTest.OwnerOne;
    public IHystrixCommandKey DependencyKey;
    public IHystrixThreadPoolKey ThreadPoolKey;
    public ICircuitBreaker CircuitBreaker;
    public IHystrixThreadPool ThreadPool;
    public IHystrixCommandOptions CommandPropertiesDefaults = HystrixCommandOptionsTest.GetUnitTestOptions();
    public IHystrixThreadPoolOptions ThreadPoolPropertiesDefaults = HystrixThreadPoolOptionsTest.GetUnitTestPropertiesBuilder();
    public HystrixCommandMetrics Metrics;
    public SemaphoreSlim FallbackSemaphore;
    public SemaphoreSlim ExecutionSemaphore;
    public TestableExecutionHook ExecutionHook = new ();

    public TestCommandBuilder(ExecutionIsolationStrategy isolationStrategy)
    {
        CommandPropertiesDefaults.ExecutionIsolationStrategy = isolationStrategy;
    }

    public TestCommandBuilder SetOwner(IHystrixCommandGroupKey owner)
    {
        this.Owner = owner;
        return this;
    }

    public TestCommandBuilder SetCommandKey(IHystrixCommandKey dependencyKey)
    {
        this.DependencyKey = dependencyKey;
        return this;
    }

    public TestCommandBuilder SetThreadPoolKey(IHystrixThreadPoolKey threadPoolKey)
    {
        this.ThreadPoolKey = threadPoolKey;
        return this;
    }

    public TestCommandBuilder SetCircuitBreaker(TestCircuitBreaker circuitBreaker)
    {
        this.CircuitBreaker = circuitBreaker;
        if (circuitBreaker != null)
        {
            Metrics = circuitBreaker.Metrics;
        }

        return this;
    }

    public TestCommandBuilder SetThreadPool(IHystrixThreadPool threadPool)
    {
        this.ThreadPool = threadPool;
        return this;
    }

    public TestCommandBuilder SetCommandOptionDefaults(IHystrixCommandOptions commandPropertiesDefaults)
    {
        this.CommandPropertiesDefaults = commandPropertiesDefaults;
        return this;
    }

    public TestCommandBuilder SetThreadPoolPropertiesDefaults(IHystrixThreadPoolOptions threadPoolPropertiesDefaults)
    {
        this.ThreadPoolPropertiesDefaults = threadPoolPropertiesDefaults;
        return this;
    }

    public TestCommandBuilder SetMetrics(HystrixCommandMetrics metrics)
    {
        this.Metrics = metrics;
        return this;
    }

    public TestCommandBuilder SetFallbackSemaphore(SemaphoreSlim fallbackSemaphore)
    {
        this.FallbackSemaphore = fallbackSemaphore;
        return this;
    }

    public TestCommandBuilder SetExecutionSemaphore(SemaphoreSlim executionSemaphore)
    {
        this.ExecutionSemaphore = executionSemaphore;
        return this;
    }

    public TestCommandBuilder SetExecutionHook(TestableExecutionHook executionHook)
    {
        this.ExecutionHook = executionHook;
        return this;
    }
}