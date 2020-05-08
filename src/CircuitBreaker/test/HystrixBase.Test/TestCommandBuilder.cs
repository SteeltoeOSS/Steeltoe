// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
#pragma warning disable SA1402 // File may only contain a single class
    public class TestCommandBuilder
    {
        public IHystrixCommandGroupKey Owner = CommandGroupForUnitTest.OWNER_ONE;
        public IHystrixCommandKey DependencyKey = null;
        public IHystrixThreadPoolKey ThreadPoolKey = null;
        public ICircuitBreaker CircuitBreaker;
        public IHystrixThreadPool ThreadPool = null;
        public IHystrixCommandOptions CommandPropertiesDefaults = HystrixCommandOptionsTest.GetUnitTestOptions();
        public IHystrixThreadPoolOptions ThreadPoolPropertiesDefaults = HystrixThreadPoolOptionsTest.GetUnitTestPropertiesBuilder();
        public HystrixCommandMetrics Metrics;
        public SemaphoreSlim FallbackSemaphore = null;
        public SemaphoreSlim ExecutionSemaphore = null;
        public TestableExecutionHook ExecutionHook = new TestableExecutionHook();

        public TestCommandBuilder(ExecutionIsolationStrategy isolationStrategy)
        {
            CommandPropertiesDefaults = HystrixCommandOptionsTest.GetUnitTestOptions();
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

    public class CommandKeyForUnitTest
    {
        public static IHystrixCommandKey KEY_ONE = new HystrixCommandKeyDefault("KEY_ONE");
        public static IHystrixCommandKey KEY_TWO = new HystrixCommandKeyDefault("KEY_TWO");
    }

    public class CommandGroupForUnitTest
    {
        public static IHystrixCommandGroupKey OWNER_ONE = new HystrixCommandGroupKeyDefault("OWNER_ONE");
        public static IHystrixCommandGroupKey OWNER_TWO = new HystrixCommandGroupKeyDefault("OWNER_TWO");
    }

    public class CommandOwnerForUnitTest
    {
        public static IHystrixCommandGroupKey OWNER_ONE = new HystrixCommandGroupKeyDefault("OWNER_ONE");
        public static IHystrixCommandGroupKey OWNER_TWO = new HystrixCommandGroupKeyDefault("OWNER_TWO");
    }

    public class ThreadPoolKeyForUnitTest
    {
        public static IHystrixThreadPoolKey THREAD_POOL_ONE = new HystrixThreadPoolKeyDefault("THREAD_POOL_ONE");
        public static IHystrixThreadPoolKey THREAD_POOL_TWO = new HystrixThreadPoolKeyDefault("THREAD_POOL_TWO");
    }

#pragma warning restore SA1402 // File may only contain a single class
}
