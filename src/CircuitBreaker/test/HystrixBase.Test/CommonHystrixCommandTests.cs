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

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public abstract class CommonHystrixCommandTests<C> : HystrixTestBase
        where C : HystrixCommand<int>
    {
        public HystrixOptionsStrategy TEST_OPTIONS_FACTORY = new TestOptionsFactory();

        protected abstract void AssertHooksOnSuccess(Func<C> ctor, Action<C> assertion);

        protected abstract void AssertHooksOnFailure(Func<C> ctor, Action<C> assertion);

        protected abstract void AssertHooksOnFailure(Func<C> ctor, Action<C> assertion, bool failFast);

        protected void AssertHooksOnFailFast(Func<C> ctor, Action<C> assertion)
        {
            AssertHooksOnFailure(ctor, assertion, true);
        }

        protected void AssertBlockingObserve(C command, Action<C> assertion, bool isSuccess)
        {
            if (isSuccess)
            {
                try
                {
                    command.Observe().ToList().SingleAsync().Wait();
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                try
                {
                    command.Observe().ToList().SingleAsync().Wait();
                    Assert.True(false, "Expected a command failure!");
                }
                catch (Exception)
                {
                }
            }

            assertion(command);
        }

        protected void AssertNonBlockingObserve(C command, Action<C> assertion, bool isSuccess)
        {
            CountdownEvent latch = new CountdownEvent(1);

            IObservable<int> o = command.Observe();
            o.Subscribe(
                (n) =>
                {
                },
                (e) =>
                {
                    latch.SignalEx();
                },
                () =>
                {
                    latch.SignalEx();
                });

            try
            {
                latch.Wait(3000);
                assertion(command);
            }
            catch (Exception)
            {
                throw;
            }

            if (isSuccess)
            {
                try
                {
                    o.ToList().SingleAsync().Wait();
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else
            {
                try
                {
                    o.ToList().SingleAsync().Wait();
                    Assert.True(false, "Expected a command failure!");
                }
                catch (Exception)
                {
                }
            }
        }

        protected void AssertSaneHystrixRequestLog(int numCommands)
        {
            HystrixRequestLog currentRequestLog = HystrixRequestLog.CurrentRequestLog;

            try
            {
                Assert.Equal(numCommands, currentRequestLog.AllExecutedCommands.Count);
                Assert.DoesNotContain("Executed", currentRequestLog.GetExecutedCommandsAsString());
                Assert.True(currentRequestLog.AllExecutedCommands.First().ExecutionEvents.Count >= 1);

                // Most commands should have 1 execution event, but fallbacks / responses from cache can cause more than 1.  They should never have 0
            }
            catch (Exception)
            {
                // output.WriteLine("Problematic Request log : " + currentRequestLog.GetExecutedCommandsAsString() + " , expected : " + numCommands);
                throw;
            }
        }

        protected void AssertCommandExecutionEvents(IHystrixInvokableInfo command, params HystrixEventType[] expectedEventTypes)
        {
            bool emitExpected = false;
            int expectedEmitCount = 0;

            bool fallbackEmitExpected = false;
            int expectedFallbackEmitCount = 0;

            List<HystrixEventType> condensedEmitExpectedEventTypes = new List<HystrixEventType>();

            foreach (HystrixEventType expectedEventType in expectedEventTypes)
            {
                if (expectedEventType.Equals(HystrixEventType.EMIT))
                {
                    if (!emitExpected)
                    {
                        // first EMIT encountered, add it to condensedEmitExpectedEventTypes
                        condensedEmitExpectedEventTypes.Add(HystrixEventType.EMIT);
                    }

                    emitExpected = true;
                    expectedEmitCount++;
                }
                else if (expectedEventType.Equals(HystrixEventType.FALLBACK_EMIT))
                {
                    if (!fallbackEmitExpected)
                    {
                        // first FALLBACK_EMIT encountered, add it to condensedEmitExpectedEventTypes
                        condensedEmitExpectedEventTypes.Add(HystrixEventType.FALLBACK_EMIT);
                    }

                    fallbackEmitExpected = true;
                    expectedFallbackEmitCount++;
                }
                else
                {
                    condensedEmitExpectedEventTypes.Add(expectedEventType);
                }
            }

            List<HystrixEventType> actualEventTypes = command.ExecutionEvents;
            Assert.Equal(expectedEmitCount, command.NumberEmissions);
            Assert.Equal(expectedFallbackEmitCount, command.NumberFallbackEmissions);
            Assert.Equal(condensedEmitExpectedEventTypes, actualEventTypes);
        }

        protected C GetCommand(ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult)
        {
            return GetCommand(isolationStrategy, executionResult, FallbackResultTest.UNIMPLEMENTED);
        }

        protected C GetCommand(ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency)
        {
            return GetCommand(isolationStrategy, executionResult, executionLatency, FallbackResultTest.UNIMPLEMENTED);
        }

        protected C GetCommand(ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, FallbackResultTest fallbackResult)
        {
            return GetCommand(isolationStrategy, executionResult, 0, fallbackResult);
        }

        protected C GetCommand(ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult)
        {
            return GetCommand(isolationStrategy, executionResult, executionLatency, fallbackResult, 0, new TestCircuitBreaker(), null, (executionLatency * 2) + 200, CacheEnabledTest.NO, "foo", 10, 10);
        }

        protected C GetCommand(ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult, int fallbackLatency, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int timeout, CacheEnabledTest cacheEnabled, object value, int executionSemaphoreCount, int fallbackSemaphoreCount)
        {
            return GetCommand(isolationStrategy, executionResult, executionLatency, fallbackResult, fallbackLatency, circuitBreaker, threadPool, timeout, cacheEnabled, value, executionSemaphoreCount, fallbackSemaphoreCount, false);
        }

        protected C GetCommand(IHystrixCommandKey key, ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult, int fallbackLatency, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int timeout, CacheEnabledTest cacheEnabled, object value, int executionSemaphoreCount, int fallbackSemaphoreCount)
        {
            SemaphoreSlim executionSemaphore = new SemaphoreSlim(executionSemaphoreCount);
            SemaphoreSlim fallbackSemaphore = new SemaphoreSlim(fallbackSemaphoreCount);

            return GetCommand(key, isolationStrategy, executionResult, executionLatency, fallbackResult, fallbackLatency, circuitBreaker, threadPool, timeout, cacheEnabled, value, executionSemaphore, fallbackSemaphore, false);
        }

        protected C GetCommand(ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult, int fallbackLatency, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int timeout, CacheEnabledTest cacheEnabled, object value, int executionSemaphoreCount, int fallbackSemaphoreCount, bool circuitBreakerDisabled)
        {
            SemaphoreSlim executionSemaphore = new SemaphoreSlim(executionSemaphoreCount);
            SemaphoreSlim fallbackSemaphore = new SemaphoreSlim(fallbackSemaphoreCount);
            return GetCommand(isolationStrategy, executionResult, executionLatency, fallbackResult, fallbackLatency, circuitBreaker, threadPool, timeout, cacheEnabled, value, executionSemaphore, fallbackSemaphore, circuitBreakerDisabled);
        }

        protected C GetCommand(ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult, int timeout)
        {
            return GetCommand(isolationStrategy, executionResult, executionLatency, fallbackResult, 0, new TestCircuitBreaker(), null, timeout, CacheEnabledTest.NO, "foo", 10, 10);
        }

        protected abstract C GetCommand(ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult, int fallbackLatency, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int timeout, CacheEnabledTest cacheEnabled, object value, SemaphoreSlim executionSemaphore, SemaphoreSlim fallbackSemaphore, bool circuitBreakerDisabled);

        protected abstract C GetCommand(IHystrixCommandKey commandKey, ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult, int fallbackLatency, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int timeout, CacheEnabledTest cacheEnabled, object value, SemaphoreSlim executionSemaphore, SemaphoreSlim fallbackSemaphore, bool circuitBreakerDisabled);

        protected C GetLatentCommand(ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult, int timeout)
        {
            return GetCommand(isolationStrategy, executionResult, executionLatency, fallbackResult, 0, new TestCircuitBreaker(), null, timeout, CacheEnabledTest.NO, "foo", 10, 10);
        }

        protected C GetLatentCommand(ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int timeout)
        {
            return GetCommand(isolationStrategy, executionResult, executionLatency, fallbackResult, 0, circuitBreaker, threadPool, timeout, CacheEnabledTest.NO, "foo", 10, 10);
        }

        protected C GetCircuitOpenCommand(ExecutionIsolationStrategy isolationStrategy, FallbackResultTest fallbackResult)
        {
            TestCircuitBreaker openCircuit = new TestCircuitBreaker();
            openCircuit.SetForceShortCircuit(true);
            return GetCommand(isolationStrategy, ExecutionResultTest.SUCCESS, 0, fallbackResult, 0, openCircuit, null, 500, CacheEnabledTest.NO, "foo", 10, 10, false);
        }

        protected C GetSharedCircuitBreakerCommand(IHystrixCommandKey commandKey, ExecutionIsolationStrategy isolationStrategy, FallbackResultTest fallbackResult, TestCircuitBreaker circuitBreaker)
        {
            return GetCommand(commandKey, isolationStrategy, ExecutionResultTest.FAILURE, 0, fallbackResult, 0, circuitBreaker, null, 500, CacheEnabledTest.NO, "foo", 10, 10);
        }

        protected C GetCircuitBreakerDisabledCommand(ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult)
        {
            return GetCommand(isolationStrategy, executionResult, 0, FallbackResultTest.UNIMPLEMENTED, 0, new TestCircuitBreaker(), null, 500, CacheEnabledTest.NO, "foo", 10, 10, true);
        }

        protected C GetRecoverableErrorCommand(ExecutionIsolationStrategy isolationStrategy, FallbackResultTest fallbackResult)
        {
            return GetCommand(isolationStrategy, ExecutionResultTest.RECOVERABLE_ERROR, 0, fallbackResult);
        }

        protected C GetUnrecoverableErrorCommand(ExecutionIsolationStrategy isolationStrategy, FallbackResultTest fallbackResult)
        {
            return GetCommand(isolationStrategy, ExecutionResultTest.UNRECOVERABLE_ERROR, 0, fallbackResult);
        }
    }

    public enum ExecutionResultTest
    {
        SUCCESS, FAILURE, ASYNC_FAILURE, HYSTRIX_FAILURE, ASYNC_HYSTRIX_FAILURE, RECOVERABLE_ERROR, ASYNC_RECOVERABLE_ERROR, UNRECOVERABLE_ERROR, ASYNC_UNRECOVERABLE_ERROR, BAD_REQUEST, ASYNC_BAD_REQUEST, MULTIPLE_EMITS_THEN_SUCCESS, MULTIPLE_EMITS_THEN_FAILURE, NO_EMITS_THEN_SUCCESS
    }

    public enum FallbackResultTest
    {
        UNIMPLEMENTED, SUCCESS, FAILURE, ASYNC_FAILURE, MULTIPLE_EMITS_THEN_SUCCESS, MULTIPLE_EMITS_THEN_FAILURE, NO_EMITS_THEN_SUCCESS
    }

    public enum CacheEnabledTest
    {
        YES, NO
    }
}
