﻿// Copyright 2017 the original author or authors.
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

using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using System;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class TestHystrixCommand<T> : HystrixCommand<T>
    {
        public ITestOutputHelper _output;
        private readonly TestCommandBuilder builder;

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
            this.builder = builder;
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
            this.builder = builder;
        }

        public virtual TestCommandBuilder Builder
        {
            get { return builder; }
        }

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
}
