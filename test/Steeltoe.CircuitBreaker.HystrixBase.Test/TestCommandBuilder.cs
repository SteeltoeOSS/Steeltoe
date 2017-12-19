//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Threading;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public static class CommandKeyForUnitTest
    {
        public static IHystrixCommandKey KEY_ONE = new HystrixCommandKeyDefault("KEY_ONE");
        public static IHystrixCommandKey KEY_TWO = new HystrixCommandKeyDefault("KEY_TWO");
    }

    public static class CommandGroupForUnitTest
    {
        public static IHystrixCommandGroupKey OWNER_ONE = new HystrixCommandGroupKeyDefault("OWNER_ONE");
        public static IHystrixCommandGroupKey OWNER_TWO = new HystrixCommandGroupKeyDefault("OWNER_TWO");
    }
    public static class CommandOwnerForUnitTest
    {
        public static IHystrixCommandGroupKey OWNER_ONE = new HystrixCommandGroupKeyDefault("OWNER_ONE");
        public static IHystrixCommandGroupKey OWNER_TWO = new HystrixCommandGroupKeyDefault("OWNER_TWO");
    }
    public static class ThreadPoolKeyForUnitTest 
    {
        public static IHystrixThreadPoolKey THREAD_POOL_ONE = new HystrixThreadPoolKeyDefault("THREAD_POOL_ONE");
        public static IHystrixThreadPoolKey THREAD_POOL_TWO = new HystrixThreadPoolKeyDefault("THREAD_POOL_TWO");
    }

    public class TestCommandBuilder
    {
        public IHystrixCommandGroupKey owner = CommandGroupForUnitTest.OWNER_ONE;
        public IHystrixCommandKey dependencyKey = null;
        public IHystrixThreadPoolKey threadPoolKey = null;
        public IHystrixCircuitBreaker circuitBreaker;
        public IHystrixThreadPool threadPool = null;
        public IHystrixCommandOptions commandPropertiesDefaults = HystrixCommandOptionsTest.GetUnitTestOptions();
        public IHystrixThreadPoolOptions threadPoolPropertiesDefaults = HystrixThreadPoolOptionsTest.GetUnitTestPropertiesBuilder();
        public HystrixCommandMetrics metrics;
        public SemaphoreSlim fallbackSemaphore = null;
        public SemaphoreSlim executionSemaphore = null;
        public TestableExecutionHook executionHook = new TestableExecutionHook();

        public TestCommandBuilder(ExecutionIsolationStrategy isolationStrategy)
        {
            this.commandPropertiesDefaults = HystrixCommandOptionsTest.GetUnitTestOptions();
            this.commandPropertiesDefaults.ExecutionIsolationStrategy = isolationStrategy;
        }

        public TestCommandBuilder SetOwner(IHystrixCommandGroupKey owner)
        {
            this.owner = owner;
            return this;
        }

        public TestCommandBuilder SetCommandKey(IHystrixCommandKey dependencyKey)
        {
            this.dependencyKey = dependencyKey;
            return this;
        }

        public TestCommandBuilder SetThreadPoolKey(IHystrixThreadPoolKey threadPoolKey)
        {
            this.threadPoolKey = threadPoolKey;
            return this;
        }

        public TestCommandBuilder SetCircuitBreaker(TestCircuitBreaker circuitBreaker)
        {
            this.circuitBreaker = circuitBreaker;
            if (circuitBreaker != null)
            {
                this.metrics = circuitBreaker.metrics;
            }
            return this;
        }

        public TestCommandBuilder SetThreadPool(IHystrixThreadPool threadPool)
        {
            this.threadPool = threadPool;
            return this;
        }

        public TestCommandBuilder SetCommandOptionDefaults(IHystrixCommandOptions commandPropertiesDefaults)
        {
            this.commandPropertiesDefaults = commandPropertiesDefaults;
            return this;
        }

        public TestCommandBuilder SetThreadPoolPropertiesDefaults(IHystrixThreadPoolOptions threadPoolPropertiesDefaults)
        {
            this.threadPoolPropertiesDefaults = threadPoolPropertiesDefaults;
            return this;
        }

        public TestCommandBuilder SetMetrics(HystrixCommandMetrics metrics)
        {
            this.metrics = metrics;
            return this;
        }

        public TestCommandBuilder SetFallbackSemaphore(SemaphoreSlim fallbackSemaphore)
        {
            this.fallbackSemaphore = fallbackSemaphore;
            return this;
        }

        public TestCommandBuilder SetExecutionSemaphore(SemaphoreSlim executionSemaphore)
        {
            this.executionSemaphore = executionSemaphore;
            return this;
        }

        public TestCommandBuilder SetExecutionHook(TestableExecutionHook executionHook)
        {
            this.executionHook = executionHook;
            return this;
        }
    }
}
