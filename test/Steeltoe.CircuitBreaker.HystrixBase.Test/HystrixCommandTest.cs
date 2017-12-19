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

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using System.Reactive.Concurrency;
using System.Reactive.Observable.Aliases;
using System.Diagnostics;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixCommandTest : CommonHystrixCommandTests<TestHystrixCommand<int>>, IDisposable
    {
     
        private ITestOutputHelper output;

        public HystrixCommandTest(ITestOutputHelper output) :base()
        {
            this.output = output;

        }

        [Fact]
        public void TestExecutionSuccess()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS);
            Assert.Equal(FlexibleTestHystrixCommand.EXECUTE_VALUE, command.Execute());

            Assert.Null( command.FailedExecutionException);
            Assert.Null(command.ExecutionException);
            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsSuccessfulExecution);

            AssertCommandExecutionEvents(command, HystrixEventType.SUCCESS);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestExecutionMultipleTimes()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS);
            Assert.False(command.IsExecutionComplete);
            // first should succeed
            Assert.Equal(FlexibleTestHystrixCommand.EXECUTE_VALUE, command.Execute());
            Assert.True(command.IsExecutionComplete);
            Assert.True(command.IsExecutedInThread);
            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsSuccessfulExecution);
            Assert.Null(command.ExecutionException);

            try
            {
                // second should fail
                command.Execute();
                Assert.True(false, "we should not allow this ... it breaks the state of request logs");
            }
            catch (HystrixRuntimeException e)
            {
                output.WriteLine(e.ToString());
                // we want to get here
            }

            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
            AssertCommandExecutionEvents(command, HystrixEventType.SUCCESS);
        }
        [Fact]
        public void TestExecutionHystrixFailureWithNoFallback()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.HYSTRIX_FAILURE, FallbackResultTest.UNIMPLEMENTED);
            try
            {
                command.Execute();
                Assert.True(false, "we shouldn't get here");
            }
            catch (HystrixRuntimeException e)
            {
                output.WriteLine(e.ToString());
                Assert.NotNull(e.FallbackException);
                Assert.NotNull(e.ImplementingClass);
            }
            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsFailedExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_MISSING);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestExecutionFailureWithNoFallback()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.FAILURE, FallbackResultTest.UNIMPLEMENTED);
            try
            {
                command.Execute();
                Assert.True(false, "we shouldn't get here");
            }
            catch (HystrixRuntimeException e)
            {
                output.WriteLine(e.ToString());
                Assert.NotNull(e.FallbackException);
                Assert.NotNull(e.ImplementingClass);
            }

            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsFailedExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_MISSING);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestExecutionFailureWithFallback()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.FAILURE, FallbackResultTest.SUCCESS);
            Assert.Equal(FlexibleTestHystrixCommand.FALLBACK_VALUE, command.Execute());
            Assert.Equal("Execution Failure for TestHystrixCommand", command.FailedExecutionException.Message);
            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsFailedExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }

        [Fact]
        public void TestExecutionFailureWithFallbackFailure()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.FAILURE, FallbackResultTest.FAILURE);
            try
            {
                command.Execute();
                Assert.True(false, "we shouldn't get here");
            }
            catch (HystrixRuntimeException e)
            {
                output.WriteLine("------------------------------------------------");
                output.WriteLine(e.ToString());
                output.WriteLine("------------------------------------------------");
                Assert.NotNull(e.FallbackException);
            }

            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsFailedExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_FAILURE);
            Assert.NotNull(command.ExecutionException);

            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }

        [Fact]
        public void TestQueueSuccess()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS);
            Task<int> future = command.ExecuteAsync();
            Assert.Equal(FlexibleTestHystrixCommand.EXECUTE_VALUE, future.Result);
            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsSuccessfulExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.SUCCESS);
            Assert.Null(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestQueueKnownFailureWithNoFallback()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.HYSTRIX_FAILURE, FallbackResultTest.UNIMPLEMENTED);
            try
            {
                var result = command.ExecuteAsync().Result;
                Assert.True(false, "we shouldn't get here");
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                if (e.InnerException is HystrixRuntimeException)
                {
                    HystrixRuntimeException de = (HystrixRuntimeException)e.InnerException;

                    Assert.NotNull(de.FallbackException);
                    Assert.NotNull(de.ImplementingClass);
                }
                else
                {
                    Assert.False(true, "the cause should be HystrixRuntimeException");
                }
            }

            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsFailedExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_MISSING);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestQueueUnknownFailureWithNoFallback()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.FAILURE, FallbackResultTest.UNIMPLEMENTED);
            try
            {
                var result = command.ExecuteAsync().Result;
                Assert.True(false, "we shouldn't get here");
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                if (e.InnerException is HystrixRuntimeException)
                {
                    HystrixRuntimeException de = (HystrixRuntimeException)e.InnerException;

                    Assert.NotNull(de.FallbackException);
                    Assert.NotNull(de.ImplementingClass);
                }
                else
                {
                    Assert.False(true, "the cause should be HystrixRuntimeException");
                }
            }

            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsFailedExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_MISSING);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }

        [Fact]
        public void TestQueueFailureWithFallback()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.FAILURE, FallbackResultTest.SUCCESS);
            try
            {
                Task<int> future = command.ExecuteAsync();
                Assert.Equal(FlexibleTestHystrixCommand.FALLBACK_VALUE, future.Result);
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                Assert.False(true, "We should have received a response from the fallback.");
            }

            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsFailedExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestQueueFailureWithFallbackFailure()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.FAILURE, FallbackResultTest.FAILURE);
            try
            {
                var result = command.ExecuteAsync().Result;
                Assert.True(true, "we shouldn't get here");
            }
            catch (Exception e)
            {
                if (e.InnerException is HystrixRuntimeException)
                {
                    HystrixRuntimeException de = (HystrixRuntimeException)e.InnerException;
                    output.WriteLine(e.ToString());
                    Assert.NotNull(de.FallbackException);
                }
                else
                {
                    Assert.True(false, "the cause should be HystrixRuntimeException");
                }
            }

            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsFailedExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_FAILURE);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }

        [Fact]
        public async void TestObserveSuccess()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS);
            Assert.Equal(FlexibleTestHystrixCommand.EXECUTE_VALUE, await command.Observe().SingleAsync());
            Assert.Null( command.FailedExecutionException);
            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsSuccessfulExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.SUCCESS);
            Assert.Null(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }

        class TestCallbackThreadForThreadIsolation_TestHystrixCommand : TestHystrixCommand<bool>
        {
            AtomicReference<Thread> commandThread;
            public TestCallbackThreadForThreadIsolation_TestHystrixCommand(AtomicReference<Thread> commandThread, TestCommandBuilder builder) : base(builder)
            {
                this.commandThread = commandThread;
            }
            protected override bool Run()
            {
                commandThread.Value = Thread.CurrentThread;
                return true;
            }
        }

        [Fact]
        public void TestCallbackThreadForThreadIsolation()
        {

            AtomicReference<Thread> commandThread = new AtomicReference<Thread>();
            AtomicReference<Thread> subscribeThread = new AtomicReference<Thread>();

            TestHystrixCommand<bool> command = new TestCallbackThreadForThreadIsolation_TestHystrixCommand(commandThread, TestHystrixCommand<bool>.TestPropsBuilder());
            CountdownEvent latch = new CountdownEvent(1);
            command.ToObservable().Subscribe((args) =>
            {
                subscribeThread.Value = Thread.CurrentThread;
            },
            (e) =>
            {
                latch.SignalEx();
                output.WriteLine(e.ToString());
            },
            () =>
            {
                latch.SignalEx();
            });



            if (!latch.Wait(2000))
            {
                Assert.False(true, "timed out");
            }

            Assert.NotNull(commandThread.Value);
            Assert.NotNull(subscribeThread.Value);

            output.WriteLine("Command Thread: " + commandThread.Value);
            output.WriteLine("Subscribe Thread: " + subscribeThread.Value);

            // Threads are threadpool threads and will not have hystrix- names
            //Assert.True(commandThread.Value.Name.StartsWith("hystrix-"));
            //Assert.True(subscribeThread.Value.Name.StartsWith("hystrix-"));

            // Steeltoe Added this check
            Assert.NotEqual(commandThread.Value.ManagedThreadId, subscribeThread.Value.ManagedThreadId);
        }
        class TestCallbackThreadForSemaphoreIsolation_TestHystrixCommand : TestHystrixCommand<bool>
        {
            AtomicReference<Thread> commandThread;
            public TestCallbackThreadForSemaphoreIsolation_TestHystrixCommand(AtomicReference<Thread> commandThread, TestCommandBuilder builder) : base(builder)
            {
                this.commandThread = commandThread;
            }
            protected override bool Run()
            {
                commandThread.Value = Thread.CurrentThread;
                return true;
            }
        }

        [Fact]
        public void TestCallbackThreadForSemaphoreIsolation()
        {

            AtomicReference<Thread> commandThread = new AtomicReference<Thread>();
            AtomicReference<Thread> subscribeThread = new AtomicReference<Thread>();

            var builder = TestHystrixCommand<bool>.TestPropsBuilder();
            var opts = HystrixCommandOptionsTest.GetUnitTestOptions();
            opts.ExecutionIsolationStrategy = ExecutionIsolationStrategy.SEMAPHORE;
            builder.SetCommandOptionDefaults(opts);
            TestHystrixCommand<bool> command = new TestCallbackThreadForSemaphoreIsolation_TestHystrixCommand(commandThread, builder);

            CountdownEvent latch = new CountdownEvent(1);
            command.ToObservable().Subscribe((args) =>
            {
                subscribeThread.Value = Thread.CurrentThread;
            },
            (e) =>
            {
                latch.SignalEx();
                output.WriteLine(e.ToString());
            },
            () =>
            {
                latch.SignalEx();
            });



            if (!latch.Wait(2000))
            {
                Assert.False(true, "timed out");
            }

            Assert.NotNull(commandThread.Value);
            Assert.NotNull(subscribeThread.Value);
            output.WriteLine("Command Thread: " + commandThread.Value);
            output.WriteLine("Subscribe Thread: " + subscribeThread.Value);

            int mainThreadId = Thread.CurrentThread.ManagedThreadId;

            // semaphore should be on the calling thread
            Assert.True(commandThread.Value.ManagedThreadId.Equals(mainThreadId));
            Assert.True(subscribeThread.Value.ManagedThreadId.Equals(mainThreadId));
        }


        [Fact]
        public void TestCircuitBreakerReportsOpenIfForcedOpen()
        {
            HystrixCommandOptions opts = new HystrixCommandOptions()
            {
                GroupKey = HystrixCommandGroupKeyDefault.AsKey("GROUP"),
                CircuitBreakerForceOpen = true
            };

            HystrixCommand<bool> cmd = new HystrixCommand<bool>(opts, () => true, () => false);

            Assert.False(cmd.Execute()); //fallback should fire
            output.WriteLine("RESULT : " + cmd.ExecutionEvents);
            Assert.True(cmd.IsCircuitBreakerOpen);
        }

        [Fact]
        public void TestCircuitBreakerReportsClosedIfForcedClosed()
        {
            HystrixCommandOptions opts = new HystrixCommandOptions()
            {
                GroupKey = HystrixCommandGroupKeyDefault.AsKey("GROUP"),
                CircuitBreakerForceOpen = false,
                CircuitBreakerForceClosed = true
            };
            HystrixCommand<bool> cmd = new HystrixCommand<bool>(opts, () => true, () => false);

            Assert.True(cmd.Execute()); //fallback should fire
            output.WriteLine("RESULT : " + cmd.ExecutionEvents);
            Assert.False(cmd.IsCircuitBreakerOpen);

        }
        [Fact]
        public void TestCircuitBreakerAcrossMultipleCommandsButSameCircuitBreaker()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("SharedCircuitBreaker");
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker(key);

            /* fail 3 times and then it should trip the circuit and stop executing */
            // failure 1
            TestHystrixCommand<int> attempt1 = GetSharedCircuitBreakerCommand(key, ExecutionIsolationStrategy.THREAD, FallbackResultTest.SUCCESS, circuitBreaker);
            output.WriteLine("COMMAND KEY (from cmd): " + attempt1.CommandKey.Name);
            attempt1.Execute();
            Time.Wait( 100);
            Assert.True(attempt1.IsResponseFromFallback);
            Assert.False(attempt1.IsCircuitBreakerOpen);
            Assert.False(attempt1.IsResponseShortCircuited);

            // failure 2 with a different command, same circuit breaker
            TestHystrixCommand<int> attempt2 = GetSharedCircuitBreakerCommand(key, ExecutionIsolationStrategy.THREAD, FallbackResultTest.SUCCESS, circuitBreaker);
            attempt2.Execute();
            Time.Wait( 100);
            Assert.True(attempt2.IsFailedExecution);
            Assert.True(attempt2.IsResponseFromFallback);
            Assert.False(attempt2.IsCircuitBreakerOpen);
            Assert.False(attempt2.IsResponseShortCircuited);

            // failure 3 of the Hystrix, 2nd for this particular HystrixCommand
            TestHystrixCommand<int> attempt3 = GetSharedCircuitBreakerCommand(key, ExecutionIsolationStrategy.THREAD, FallbackResultTest.SUCCESS, circuitBreaker);
            attempt3.Execute();
            Time.Wait( 150);
            Assert.True(attempt3.IsFailedExecution);
            Assert.True(attempt3.IsResponseFromFallback);
            Assert.False(attempt3.IsResponseShortCircuited);

            // it should now be 'open' and prevent further executions
            // after having 3 failures on the Hystrix that these 2 different HystrixCommand objects are for
            Assert.True(attempt3.IsCircuitBreakerOpen);

            // attempt 4
            TestHystrixCommand<int> attempt4 = GetSharedCircuitBreakerCommand(key, ExecutionIsolationStrategy.THREAD, FallbackResultTest.SUCCESS, circuitBreaker);
            attempt4.Execute();
            Time.Wait( 100);
            Assert.True(attempt4.IsResponseFromFallback);
            // this should now be true as the response will be short-circuited
            Assert.True(attempt4.IsResponseShortCircuited);
            // this should remain open
            Assert.True(attempt4.IsCircuitBreakerOpen);

            AssertSaneHystrixRequestLog(4);
            AssertCommandExecutionEvents(attempt1, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS);
            AssertCommandExecutionEvents(attempt2, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS);
            AssertCommandExecutionEvents(attempt3, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS);
            AssertCommandExecutionEvents(attempt4, HystrixEventType.SHORT_CIRCUITED, HystrixEventType.FALLBACK_SUCCESS);
        }

        [Fact]
        public void TestExecutionSuccessWithCircuitBreakerDisabled()
        {
            TestHystrixCommand<int> command = GetCircuitBreakerDisabledCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS);
            Assert.Equal(FlexibleTestHystrixCommand.EXECUTE_VALUE, command.Execute());

            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
            // we'll still get metrics ... just not the circuit breaker opening/closing
            AssertCommandExecutionEvents(command, HystrixEventType.SUCCESS);
        }
        [Fact]
        public void TestExecutionTimeoutWithNoFallback()
        {
            TestHystrixCommand<int> command = GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 200, FallbackResultTest.UNIMPLEMENTED, 50);
            try
            {
                command.Execute();
                Assert.True(false, "we shouldn't get here");
            }
            catch (Exception e)
            {
                //                e.printStackTrace();
                if (e is HystrixRuntimeException)
                {
                    HystrixRuntimeException de = (HystrixRuntimeException)e;
                    Assert.NotNull(de.FallbackException);
                    Assert.True(de.FallbackException is InvalidOperationException);
                    Assert.NotNull(de.ImplementingClass);
                    Assert.NotNull(de.InnerException);
                    Assert.True(de.InnerException is TimeoutException);
                }
                else
                {
                    Assert.False(true, "the exception should be HystrixRuntimeException");
                }
            }

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            // the time should be 50+ since we timeout at 50ms
            Assert.True(command.ExecutionTimeInMilliseconds >= 50);

            Assert.True(command.IsResponseTimedOut);
            Assert.False(command.IsResponseFromFallback);
            Assert.False(command.IsResponseRejected);
            AssertCommandExecutionEvents(command, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_MISSING);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }

        [Fact]
        public void TestExecutionTimeoutWithFallback()
        {
            TestHystrixCommand<int> command = GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 200, FallbackResultTest.SUCCESS, 50);
            Assert.Equal(FlexibleTestHystrixCommand.FALLBACK_VALUE, command.Execute());
            // the time should be 50+ since we timeout at 50ms
            Assert.True(command.ExecutionTimeInMilliseconds >= 50);
            Assert.False(command.IsCircuitBreakerOpen);
            Assert.False(command.IsResponseShortCircuited);
            Assert.True(command.IsResponseTimedOut);
            Assert.True(command.IsResponseFromFallback);
            AssertCommandExecutionEvents(command, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_SUCCESS);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestExecutionTimeoutFallbackFailure()
        {
            TestHystrixCommand<int> command = GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 200, FallbackResultTest.FAILURE, 50);
            try
            {
                command.Execute();
                Assert.True(false, "we shouldn't get here");
            }
            catch (Exception e)
            {
                if (e is HystrixRuntimeException)
                {
                    HystrixRuntimeException de = (HystrixRuntimeException)e;
                    Assert.NotNull(de.FallbackException);
                    Assert.False(de.FallbackException is InvalidOperationException);
                    Assert.NotNull(de.ImplementingClass);
                    Assert.NotNull(de.InnerException);
                    Assert.True(de.InnerException is TimeoutException);
                }
                else
                {
                    Assert.True(false, "the exception should be HystrixRuntimeException");
                }
            }

            Assert.NotNull(command.ExecutionException);

            // the time should be 50+ since we timeout at 50ms
            Assert.True(command.ExecutionTimeInMilliseconds >= 50);
            AssertCommandExecutionEvents(command, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_FAILURE);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestCountersOnExecutionTimeout()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 200, FallbackResultTest.SUCCESS, 50);
            command.Execute();

            /* wait long enough for the command to have finished */
            Time.Wait(200);

            /* response should still be the same as 'testCircuitBreakerOnExecutionTimeout' */
            Assert.True(command.IsResponseFromFallback);
            Assert.False(command.IsCircuitBreakerOpen);
            Assert.False(command.IsResponseShortCircuited);

            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsResponseTimedOut);
            Assert.False(command.IsSuccessfulExecution);
            Assert.NotNull(command.ExecutionException);

            AssertCommandExecutionEvents(command, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_SUCCESS);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }

        [Fact]
        public void TestQueuedExecutionTimeoutWithNoFallback()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 200, FallbackResultTest.UNIMPLEMENTED, 50);
            try
            {
                var result = command.ExecuteAsync().Result;
                Assert.True(false, "we shouldn't get here");
            }
            catch (Exception e)
            {
                //e.printStackTrace();
                if (e is AggregateException && e.InnerException is HystrixRuntimeException)
                {
                    HystrixRuntimeException de = (HystrixRuntimeException)e.InnerException;
                    Assert.NotNull(de.FallbackException);
                    Assert.True(de.FallbackException is InvalidOperationException);
                    Assert.NotNull(de.ImplementingClass);
                    Assert.NotNull(de.InnerException);
                    Assert.True(de.InnerException is TimeoutException);
                }
                else
                {
                    Assert.False(true, "the exception should be AggregateException with cause as HystrixRuntimeException");
                }
            }

            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsResponseTimedOut);
            AssertCommandExecutionEvents(command, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_MISSING);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestQueuedExecutionTimeoutWithFallback()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 200, FallbackResultTest.SUCCESS, 50);
            Assert.Equal(FlexibleTestHystrixCommand.FALLBACK_VALUE, command.ExecuteAsync().Result);
            AssertCommandExecutionEvents(command, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_SUCCESS);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestQueuedExecutionTimeoutFallbackFailure()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 200, FallbackResultTest.FAILURE, 50);
            try
            {
                var result = command.ExecuteAsync().Result;
                Assert.True(false, "we shouldn't get here");
            }
            catch (Exception e)
            {
                if (e is AggregateException && e.InnerException is HystrixRuntimeException)
                {
                    HystrixRuntimeException de = (HystrixRuntimeException)e.InnerException;
                    Assert.NotNull(de.FallbackException);
                    Assert.False(de.FallbackException is InvalidOperationException);
                    Assert.NotNull(de.ImplementingClass);
                    Assert.NotNull(de.InnerException);
                    Assert.True(de.InnerException is TimeoutException);
                }
                else
                {
                    Assert.False(true, "the exception should be AggregateException with cause as HystrixRuntimeException");
                }

            }

            AssertCommandExecutionEvents(command, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_FAILURE);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }

        [Fact]
        public void TestObservedExecutionTimeoutWithNoFallback()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 200, FallbackResultTest.UNIMPLEMENTED, 50);
            try
            {
                command.Observe().Single();
                Assert.False(true, "we shouldn't get here");
            }
            catch (Exception e)
            {
                if (e is HystrixRuntimeException)
                {
                    HystrixRuntimeException de = (HystrixRuntimeException)e;
                    Assert.NotNull(de.FallbackException);
                    Assert.True(de.FallbackException is InvalidOperationException);
                    Assert.NotNull(de.ImplementingClass);
                    Assert.NotNull(de.InnerException);
                    Assert.True(de.InnerException is TimeoutException);
                }
                else
                {
                    Assert.False(true, "the exception should be AggregateException with cause as HystrixRuntimeException");
                }
            }

            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsResponseTimedOut);
            AssertCommandExecutionEvents(command, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_MISSING);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }

        [Fact]
        public void TestObservedExecutionTimeoutWithFallback()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 200, FallbackResultTest.SUCCESS, 50);
            Assert.Equal(FlexibleTestHystrixCommand.FALLBACK_VALUE, command.Observe().Single());

            AssertCommandExecutionEvents(command, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_SUCCESS);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestObservedExecutionTimeoutFallbackFailure()
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 200, FallbackResultTest.FAILURE, 50);
            try
            {
                command.Observe().Single();
                Assert.False(true, "we shouldn't get here");
            }
            catch (Exception e)
            {
                if (e is HystrixRuntimeException)
                {
                    HystrixRuntimeException de = (HystrixRuntimeException)e;
                    Assert.NotNull(de.FallbackException);
                    Assert.False(de.FallbackException is InvalidOperationException);
                    Assert.NotNull(de.ImplementingClass);
                    Assert.NotNull(de.InnerException);
                    Assert.True(de.InnerException is TimeoutException);
                }
                else
                {
                    Assert.True(false, "the exception should be AggregateException with cause as HystrixRuntimeException");
                }


            }
            AssertCommandExecutionEvents(command, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_FAILURE);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);

        }

        [Fact]
        public void TestShortCircuitFallbackCounter()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker().SetForceShortCircuit(true);
            KnownFailureTestCommandWithFallback command1 = new KnownFailureTestCommandWithFallback(circuitBreaker);
            command1.Execute();

            KnownFailureTestCommandWithFallback command2 = new KnownFailureTestCommandWithFallback(circuitBreaker);
            command2.Execute();

            // will be -1 because it never attempted execution
            Assert.True(command1.ExecutionTimeInMilliseconds == -1);
            Assert.True(command1.IsResponseShortCircuited);
            Assert.False(command1.IsResponseTimedOut);
            Assert.NotNull(command1.ExecutionException);


            AssertCommandExecutionEvents(command1, HystrixEventType.SHORT_CIRCUITED, HystrixEventType.FALLBACK_SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.SHORT_CIRCUITED, HystrixEventType.FALLBACK_SUCCESS);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(2);
        }

        [Fact]
        public async void TestRejectedThreadWithNoFallback()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Rejection-NoFallback");
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            SingleThreadedPoolWithQueue pool = new SingleThreadedPoolWithQueue(1);

            Task<bool> f = null;
            Task<bool> f2 = null;
            TestCommandRejection command1 = null;
            TestCommandRejection command2 = null;
            TestCommandRejection command3 = null;
            try
            {
                command1 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_NOT_IMPLEMENTED);
                command2 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_NOT_IMPLEMENTED);
                command3 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_NOT_IMPLEMENTED);
                f = command1.ExecuteAsync(); // Running
                Time.Wait(20); // Let first start
                f2 = command2.ExecuteAsync(); // In Queue
                await command3.ExecuteAsync(); // Start, queue rejected
                Assert.True(false, "we shouldn't get here");
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());
                output.WriteLine("command.getExecutionTimeInMilliseconds(): " + command3.ExecutionTimeInMilliseconds);
                // will be -1 because it never attempted execution
                Assert.True(command3.IsResponseRejected);
                Assert.False(command3.IsResponseShortCircuited);
                Assert.False(command3.IsResponseTimedOut);
                Assert.NotNull(command3.ExecutionException);

                if (e is HystrixRuntimeException && e.InnerException is RejectedExecutionException)
                {
                    HystrixRuntimeException de = (HystrixRuntimeException)e;
                    Assert.NotNull(de.FallbackException);
                    Assert.True(de.FallbackException is InvalidOperationException);
                    Assert.NotNull(de.ImplementingClass);
                    Assert.NotNull(de.InnerException);
                    Assert.True(de.InnerException is RejectedExecutionException);
                }
                else
                {
                    Assert.False(true, "the exception should be HystrixRuntimeException with cause as RejectedExecutionException");
                }
            }

            // Make sure finished
            var result = f.Result;
            var result2 = f2.Result;

            AssertCommandExecutionEvents(command1, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command3, HystrixEventType.THREAD_POOL_REJECTED, HystrixEventType.FALLBACK_MISSING);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(3);
            pool.Dispose();
        }
        [Fact]
        public void TestRejectedThreadWithFallback()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Rejection-Fallback");
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            SingleThreadedPoolWithQueue pool = new SingleThreadedPoolWithQueue(1);

            //command 1 will execute in threadpool (passing through the queue)
            //command 2 will execute after spending time in the queue (after command1 completes)
            //command 3 will get rejected, since it finds pool and queue both full
            TestCommandRejection command1 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_SUCCESS);
            TestCommandRejection command2 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_SUCCESS);
            TestCommandRejection command3 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_SUCCESS);

            IObservable<bool> result1 = command1.Observe();
            Time.Wait(5);  // Let cmd1 get running
            IObservable<bool> result2 = command2.Observe();

            Time.Wait(100);
            //command3 should find queue filled, and get rejected
            Assert.False(command3.Execute());
            Assert.True(command3.IsResponseRejected);
            Assert.False(command1.IsResponseRejected);
            Assert.False(command2.IsResponseRejected);
            Assert.True(command3.IsResponseFromFallback);
            Assert.NotNull(command3.ExecutionException);

            AssertCommandExecutionEvents(command3, HystrixEventType.THREAD_POOL_REJECTED, HystrixEventType.FALLBACK_SUCCESS);
            Observable.Merge(result1, result2).ToList().Single(); //await the 2 latent commands

            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            AssertSaneHystrixRequestLog(3);
            pool.Dispose();
        }
        [Fact]
        public void TestRejectedThreadWithFallbackFailure()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            SingleThreadedPoolWithQueue pool = new SingleThreadedPoolWithQueue(1);
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Rejection-A");

            TestCommandRejection command1 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_FAILURE); //this should pass through the queue and sit in the pool
            TestCommandRejection command2 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_SUCCESS); //this should sit in the queue
            TestCommandRejection command3 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_FAILURE); //this should observe full queue and get rejected
            Task<bool> f1 = null;
            Task<bool> f2 = null;
            try
            {
                f1 = command1.ExecuteAsync();
                Time.Wait(10); // Let first one get in and off queue
                f2 = command2.ExecuteAsync();
                Assert.False( command3.Execute()); //should get thread-pool rejected
                Assert.True(false, "we shouldn't get here");
            }
            catch (Exception e)
            {
                //e.printStackTrace();
                if (e is HystrixRuntimeException && e.InnerException is RejectedExecutionException)
                {
                    HystrixRuntimeException de = (HystrixRuntimeException)e;
                    Assert.NotNull(de.FallbackException);
                    Assert.False(de.FallbackException is InvalidOperationException);
                    Assert.NotNull(de.ImplementingClass);
                    Assert.NotNull(de.InnerException);
                    Assert.True(de.InnerException is RejectedExecutionException);
                }
                else
                {
                    Assert.False(true, "the exception should be HystrixRuntimeException with cause as RejectedExecutionException");
                }
            }

            AssertCommandExecutionEvents(command1); //still in-flight, no events yet
            AssertCommandExecutionEvents(command2); //still in-flight, no events yet
            AssertCommandExecutionEvents(command3, HystrixEventType.THREAD_POOL_REJECTED, HystrixEventType.FALLBACK_FAILURE);
            int numInFlight = circuitBreaker.metrics.CurrentConcurrentExecutionCount;
            Assert.True(numInFlight <= 1); //pool-filler still going
                                           //This is a case where we knowingly walk away from executing Hystrix threads. They should have an in-flight status ("Executed").  You should avoid this in a production environment
            HystrixRequestLog requestLog = HystrixRequestLog.CurrentRequestLog;
            Assert.Equal(3, requestLog.AllExecutedCommands.Count);
            Assert.Contains("Executed", requestLog.GetExecutedCommandsAsString());

            //block on the outstanding work, so we don't inadvertently affect any other tests
            long startTime = DateTime.Now.Ticks / 10000;
            var result1 = f1.Result;
            var result2 = f2.Result;
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            output.WriteLine("Time blocked : " + ((DateTime.Now.Ticks / 10000) - startTime));
            pool.Dispose();
        }

        [Fact]
        public async void TestRejectedThreadUsingQueueSize()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Rejection-B");
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            SingleThreadedPoolWithQueue pool = new SingleThreadedPoolWithQueue(10, 1);

            // Schedule 2 items, one will be taken off and start running, the second will get queued
            // the thread pool won't pick it up because we're bypassing the pool and adding to the queue directly so this will keep the queue full
            Task t = new Task(() => Time.Wait( 500));
            t.Start(pool.GetTaskScheduler());

            Time.Wait( 10);

            Task t2 = new Task(() => Time.Wait( 500));
            t2.Start(pool.GetTaskScheduler());

            TestCommandRejection command = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_NOT_IMPLEMENTED);
            try
            {
                // this should fail as we already have 1 in the queue
                await command.ExecuteAsync();
                Assert.False(true, "we shouldn't get here");
            }
            catch (Exception e)
            {

                //e.printStackTrace();
                output.WriteLine("command.getExecutionTimeInMilliseconds(): " + command.ExecutionTimeInMilliseconds);
                // will be -1 because it never attempted execution
                Assert.True(command.IsResponseRejected);
                Assert.False(command.IsResponseShortCircuited);
                Assert.False(command.IsResponseTimedOut);
                Assert.NotNull(command.ExecutionException);

                if (e is HystrixRuntimeException && e.InnerException is RejectedExecutionException)
                {
                    HystrixRuntimeException de = (HystrixRuntimeException)e;
                    Assert.NotNull(de.FallbackException);
                    Assert.True(de.FallbackException is InvalidOperationException);
                    Assert.NotNull(de.ImplementingClass);
                    Assert.NotNull(de.InnerException);
                    Assert.True(de.InnerException is RejectedExecutionException);
                }
                else
                {
                    Assert.False(true, "the exception should be HystrixRuntimeException with cause as RejectedExecutionException");
                }

            }

            AssertCommandExecutionEvents(command, HystrixEventType.THREAD_POOL_REJECTED, HystrixEventType.FALLBACK_MISSING);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
            pool.Dispose();
        }

        [Fact]
        public void TestDisabledTimeoutWorks()
        {
            CommandWithDisabledTimeout cmd = new CommandWithDisabledTimeout(100, 900);
            bool result = cmd.Execute();

            Assert.True( result);
            Assert.False(cmd.IsResponseTimedOut);
            Assert.Null(cmd.ExecutionException);
            output.WriteLine("CMD : " + cmd._currentRequestLog.GetExecutedCommandsAsString());
            Assert.True(cmd._executionResult.ExecutionLatency >= 900);
            AssertCommandExecutionEvents(cmd, HystrixEventType.SUCCESS);
        }

        [Fact]
        public void TestFallbackSemaphore()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            // single thread should work
            TestSemaphoreCommandWithSlowFallback command1 = new TestSemaphoreCommandWithSlowFallback(circuitBreaker, 1, 200);
            bool result = command1.Execute();
            Assert.True(result);

            // 2 threads, the second should be rejected by the fallback semaphore
            bool exceptionReceived = false;
            Task<bool> result2 = null;
            TestSemaphoreCommandWithSlowFallback command2 = null;
            TestSemaphoreCommandWithSlowFallback command3 = null;
            try
            {
                output.WriteLine("c2 start: " + DateTime.Now.Ticks / 10000);
                command2 = new TestSemaphoreCommandWithSlowFallback(circuitBreaker, 1, 800);
                result2 = command2.ExecuteAsync();
                output.WriteLine("c2 after queue: " + DateTime.Now.Ticks / 10000);
                // make sure that thread gets a chance to run before queuing the next one
                Time.Wait(50);
                output.WriteLine("c3 start: " + DateTime.Now.Ticks / 10000);
                command3 = new TestSemaphoreCommandWithSlowFallback(circuitBreaker, 1, 200);
                Task<bool> result3 = command3.ExecuteAsync();
                output.WriteLine("c3 after queue: " + DateTime.Now.Ticks / 10000);
                var res3 = result3.Result;
            }
            catch (Exception )
            {
                exceptionReceived = true;
            }

            Assert.True(result2.Result);

            if (!exceptionReceived)
            {
                Assert.False(true, "We expected an exception on the 2nd get");
            }

            AssertCommandExecutionEvents(command1, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS);
            AssertCommandExecutionEvents(command3, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_REJECTION);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(3);
        }
        [Fact]
        public void TestExecutionSemaphoreWithQueue()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            // single thread should work
            TestSemaphoreCommand command1 = new TestSemaphoreCommand(circuitBreaker, 1, 200, TestSemaphoreCommand.RESULT_SUCCESS, TestSemaphoreCommand.FALLBACK_NOT_IMPLEMENTED);
            bool result = command1.Execute();
            Assert.True(result);

            AtomicBoolean exceptionReceived = new AtomicBoolean();

            SemaphoreSlim semaphore = new SemaphoreSlim(1);

            TestSemaphoreCommand command2 = new TestSemaphoreCommand(circuitBreaker, semaphore, 200, TestSemaphoreCommand.RESULT_SUCCESS, TestSemaphoreCommand.FALLBACK_NOT_IMPLEMENTED);
            ThreadStart command2Action = new ThreadStart(() =>
            {
                try
                {
                    var res = command2.ExecuteAsync().Result;
                }
                catch (Exception )
                {
                    exceptionReceived.Value = true;
                }
            });


            TestSemaphoreCommand command3 = new TestSemaphoreCommand(circuitBreaker, semaphore, 200, TestSemaphoreCommand.RESULT_SUCCESS, TestSemaphoreCommand.FALLBACK_NOT_IMPLEMENTED);
            ThreadStart command3Action = new ThreadStart(() =>
            {
                try
                {
                    var res = command3.ExecuteAsync().Result;
                }
                catch (Exception )
                {
                    exceptionReceived.Value = true;
                }
            });


            // 2 threads, the second should be rejected by the semaphore

            Thread t2 = new Thread(command2Action);
            Thread t3 = new Thread(command3Action);

            t2.Start();
            // make sure that t2 gets a chance to run before queuing the next one
            Time.Wait(50);
            t3.Start();
            t2.Join();
            t3.Join();

            if (!exceptionReceived.Value)
            {
                Assert.True(false, "We expected an exception on the 2nd get");
            }

            AssertCommandExecutionEvents(command1, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command3, HystrixEventType.SEMAPHORE_REJECTED, HystrixEventType.FALLBACK_MISSING);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(3);
        }
        [Fact]
        public void TestExecutionSemaphoreWithExecution()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            // single thread should work
            TestSemaphoreCommand command1 = new TestSemaphoreCommand(circuitBreaker, 1, 200, TestSemaphoreCommand.RESULT_SUCCESS, TestSemaphoreCommand.FALLBACK_NOT_IMPLEMENTED);
            bool result = command1.Execute();
            Assert.False(command1.IsExecutedInThread);
            Assert.True(result);

            BlockingCollection<bool> results = new BlockingCollection<bool>(2);

            AtomicBoolean exceptionReceived = new AtomicBoolean();

            SemaphoreSlim semaphore = new SemaphoreSlim(1);

            TestSemaphoreCommand command2 = new TestSemaphoreCommand(circuitBreaker, semaphore, 200, TestSemaphoreCommand.RESULT_SUCCESS, TestSemaphoreCommand.FALLBACK_NOT_IMPLEMENTED);
            ThreadStart command2Action = new ThreadStart(() =>
            {
                try
                {
                    results.Add(command2.Execute());
                }
                catch (Exception )
                {
                    exceptionReceived.Value = true;
                }
            });


            TestSemaphoreCommand command3 = new TestSemaphoreCommand(circuitBreaker, semaphore, 200, TestSemaphoreCommand.RESULT_SUCCESS, TestSemaphoreCommand.FALLBACK_NOT_IMPLEMENTED);
            ThreadStart command3Action = new ThreadStart(() =>
            {
                try
                {
                    results.Add(command3.Execute());
                }
                catch (Exception )
                {
                    exceptionReceived.Value = true;
                }
            });



            // 2 threads, the second should be rejected by the semaphore
            Thread t2 = new Thread(command2Action);
            Thread t3 = new Thread(command3Action);

            t2.Start();
            // make sure that t2 gets a chance to run before queuing the next one
            Time.Wait(50);
            t3.Start();
            t2.Join();
            t3.Join();

            if (!exceptionReceived.Value)
            {
                Assert.False(true, "We expected an exception on the 2nd get");
            }

            // only 1 value is expected as the other should have thrown an exception
            Assert.Single(results);
            // should contain only a true result
            Assert.Contains(true, results);
            Assert.DoesNotContain(false, results);
            AssertCommandExecutionEvents(command1, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command3, HystrixEventType.SEMAPHORE_REJECTED, HystrixEventType.FALLBACK_MISSING);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(3);
        }
        [Fact]
        public void TestRejectedExecutionSemaphoreWithFallbackViaExecute()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            BlockingCollection<bool> results = new BlockingCollection<bool>(2);

            AtomicBoolean exceptionReceived = new AtomicBoolean();

            TestSemaphoreCommandWithFallback command1 = new TestSemaphoreCommandWithFallback(circuitBreaker, 1, 200, false);
            ThreadStart command1Action = new ThreadStart(() =>
            {
                try
                {
                    results.Add(command1.Execute());
                }
                catch (Exception )
                {
                    exceptionReceived.Value = true;
                }
            });


            TestSemaphoreCommandWithFallback command2 = new TestSemaphoreCommandWithFallback(circuitBreaker, 1, 200, false);
            ThreadStart command2Action = new ThreadStart(() =>
            {
                try
                {
                    results.Add(command2.Execute());
                }
                catch (Exception )
                {
                    exceptionReceived.Value = true;
                }
            });


            // 2 threads, the second should be rejected by the semaphore and return fallback
            Thread t1 = new Thread(command1Action);
            Thread t2 = new Thread(command2Action);

            t1.Start();
            // make sure that t2 gets a chance to run before queuing the next one
            Time.Wait(50);
            t2.Start();
            t1.Join();
            t2.Join();

            if (exceptionReceived.Value)
            {
                Assert.False(true, "We should have received a fallback response");
            }

            // both threads should have returned values
            Assert.Equal(2, results.Count);
            // should contain both a true and false result
            Assert.Contains(true, results);
            Assert.Contains(false, results);
            AssertCommandExecutionEvents(command1, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.SEMAPHORE_REJECTED, HystrixEventType.FALLBACK_SUCCESS);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(2);
        }
        [Fact]
        public void TestRejectedExecutionSemaphoreWithFallbackViaObserve()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();

            BlockingCollection<IObservable<bool>> results = new BlockingCollection<IObservable<bool>>(2);

            AtomicBoolean exceptionReceived = new AtomicBoolean();

            TestSemaphoreCommandWithFallback command1 = new TestSemaphoreCommandWithFallback(circuitBreaker, 1, 200, false);
            ThreadStart command1Action = new ThreadStart(() =>

            {
                try
                {
                    results.Add(command1.Observe());
                }
                catch (Exception )
                {
                    exceptionReceived.Value = true;
                }
            });

            TestSemaphoreCommandWithFallback command2 = new TestSemaphoreCommandWithFallback(circuitBreaker, 1, 200, false);
            ThreadStart command2Action = new ThreadStart(() =>
            {
                try
                {
                    results.Add(command2.Observe());
                }
                catch (Exception )
                {
                    exceptionReceived.Value = true;
                }
            });



            // 2 threads, the second should be rejected by the semaphore and return fallback
            Thread t1 = new Thread(command1Action);
            Thread t2 = new Thread(command2Action);

            t1.Start();
            // make sure that t2 gets a chance to run before queuing the next one
            Time.Wait(50);
            t2.Start();
            t1.Join();
            t2.Join();

            if (exceptionReceived.Value)
            {
                Assert.False(true, "We should have received a fallback response");
            }

            IList<bool> blockingList = Observable.Merge(results).ToList().Single();

            // both threads should have returned values
            Assert.Equal(2, blockingList.Count);
            // should contain both a true and false result
            Assert.True(blockingList.Contains(true));
            Assert.True(blockingList.Contains(false));
            AssertCommandExecutionEvents(command1, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.SEMAPHORE_REJECTED, HystrixEventType.FALLBACK_SUCCESS);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(2);
        }

        [Fact]
        public void TestSemaphorePermitsInUse()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();

            // this semaphore will be shared across multiple command instances
            SemaphoreSlim sharedSemaphore = new SemaphoreSlim(3);

            // used to wait until all commands have started
            CountdownEvent startLatch = new CountdownEvent((sharedSemaphore.CurrentCount * 2) + 1);

            // used to signal that all command can finish
            CountdownEvent sharedLatch = new CountdownEvent(1);

            // tracks failures to obtain semaphores
            AtomicInteger failureCount = new AtomicInteger();
            ThreadStart sharedSemaphoreRunnable = new ThreadStart(() =>
            {
                try
                {
                    new LatchedSemaphoreCommand("Command-Shared", circuitBreaker, sharedSemaphore, startLatch, sharedLatch).Execute();
                }
                catch (Exception )
                {
                    startLatch.SignalEx();
                    //e.printStackTrace();
                    failureCount.IncrementAndGet();
                }
            });

            // creates group of threads each using command sharing a single semaphore
            // I create extra threads and commands so that I can verify that some of them fail to obtain a semaphore
            int sharedThreadCount = sharedSemaphore.CurrentCount * 2;
            Thread[] sharedSemaphoreThreads = new Thread[sharedThreadCount];
            for (int i = 0; i < sharedThreadCount; i++)
            {
                sharedSemaphoreThreads[i] = new Thread(sharedSemaphoreRunnable);
            }

            // creates thread using isolated semaphore
            SemaphoreSlim isolatedSemaphore = new SemaphoreSlim(1);

            CountdownEvent isolatedLatch = new CountdownEvent(1);

            Thread isolatedThread = new Thread(new ThreadStart(() =>
           {
               try
               {
                   new LatchedSemaphoreCommand("Command-Isolated", circuitBreaker, isolatedSemaphore, startLatch, isolatedLatch).Execute();
               }
               catch (Exception )
               {
                   startLatch.SignalEx();
                   //e.printStackTrace();
                   failureCount.IncrementAndGet();
               }
           }));

            // verifies no permits in use before starting threads
            Assert.Equal(3, sharedSemaphore.CurrentCount);
            Assert.Equal(1, isolatedSemaphore.CurrentCount);

            for (int i = 0; i < sharedThreadCount; i++)
            {
                sharedSemaphoreThreads[i].Start();
            }
            isolatedThread.Start();

            // waits until all commands have started
            startLatch.Wait(1000);

            // verifies that all semaphores are in use
            Assert.Equal(0, sharedSemaphore.CurrentCount);
            Assert.Equal(0, isolatedSemaphore.CurrentCount);

            // signals commands to finish
            sharedLatch.SignalEx();
            isolatedLatch.SignalEx();

            for (int i = 0; i < sharedThreadCount; i++)
            {
                sharedSemaphoreThreads[i].Join();
            }
            isolatedThread.Join();

            // verifies no permits in use after finishing threads
            output.WriteLine("REQLOG : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            Assert.Equal(3, sharedSemaphore.CurrentCount);
            Assert.Equal(1, isolatedSemaphore.CurrentCount);

            // verifies that some executions failed
            //Assert.Equal(sharedSemaphore.numberOfPermits.get().longValue(), failureCount.get());
            HystrixRequestLog requestLog = HystrixRequestLog.CurrentRequestLog;
            Assert.Contains("SEMAPHORE_REJECTED", requestLog.GetExecutedCommandsAsString());
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
        }
        [Fact]
        public void TestDynamicOwner()
        {
            TestHystrixCommand<bool> command = new DynamicOwnerTestCommand(CommandGroupForUnitTest.OWNER_ONE);
            Assert.True( command.Execute());
            AssertCommandExecutionEvents(command, HystrixEventType.SUCCESS);
        }

        [Fact]
        public void TestDynamicOwnerFails()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => new DynamicOwnerTestCommand(null));
        }

        [Fact]
        public void TestDynamicKey()
        {
            DynamicOwnerAndKeyTestCommand command1 = new DynamicOwnerAndKeyTestCommand(CommandGroupForUnitTest.OWNER_ONE, CommandKeyForUnitTest.KEY_ONE);
            Assert.True( command1.Execute());
            DynamicOwnerAndKeyTestCommand command2 = new DynamicOwnerAndKeyTestCommand(CommandGroupForUnitTest.OWNER_ONE, CommandKeyForUnitTest.KEY_TWO);
            Assert.True( command2.Execute());

            // 2 different circuit breakers should be created
            Assert.True(command1.CircuitBreaker != command2.CircuitBreaker);
        }

        [Fact]
        public void TestRequestCache1()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            SuccessfulCacheableCommand<string> command1 = new SuccessfulCacheableCommand<string>(circuitBreaker, true, "A");
            SuccessfulCacheableCommand<string> command2 = new SuccessfulCacheableCommand<string>(circuitBreaker, true, "A");

            Assert.True(command1.IsCommandRunningInThread);

            Task<string> f1 = command1.ExecuteAsync();
            Task<string> f2 = command2.ExecuteAsync();

            Assert.Equal("A", f1.Result);
            Assert.Equal("A", f2.Result);

            Assert.True(command1.executed);
            // the second one should not have executed as it should have received the cached value instead
            Assert.False(command2.executed);
            Assert.True(command1.ExecutionTimeInMilliseconds > -1);
            Assert.False(command1.IsResponseFromCache);
            Assert.True(command2.IsResponseFromCache);
            AssertCommandExecutionEvents(command1, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.SUCCESS, HystrixEventType.RESPONSE_FROM_CACHE);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(2);
        }
        [Fact]
        public void TestRequestCache2()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            SuccessfulCacheableCommand<string> command1 = new SuccessfulCacheableCommand<string>(circuitBreaker, true, "A");
            SuccessfulCacheableCommand<string> command2 = new SuccessfulCacheableCommand<string>(circuitBreaker, true, "B");

            Assert.True(command1.IsCommandRunningInThread);

            Task<string> f1 = command1.ExecuteAsync();
            Task<string> f2 = command2.ExecuteAsync();

            Assert.Equal("A", f1.Result);
            Assert.Equal("B", f2.Result);

            Assert.True(command1.executed);
            // both should execute as they are different
            Assert.True(command2.executed);
            Assert.True(command2.ExecutionTimeInMilliseconds > -1);
            Assert.False(command2.IsResponseFromCache);
            AssertCommandExecutionEvents(command1, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.SUCCESS);
            Assert.Null(command1.ExecutionException);
            Assert.False(command2.IsResponseFromCache);
            Assert.Null(command2.ExecutionException);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(2);
        }
        [Fact]
        public void TestRequestCache3()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            SuccessfulCacheableCommand<string> command1 = new SuccessfulCacheableCommand<string>(circuitBreaker, true, "A");
            SuccessfulCacheableCommand<string> command2 = new SuccessfulCacheableCommand<string>(circuitBreaker, true, "B");
            SuccessfulCacheableCommand<string> command3 = new SuccessfulCacheableCommand<string>(circuitBreaker, true, "A");

            Assert.True(command1.IsCommandRunningInThread);

            Task<string> f1 = command1.ExecuteAsync();
            Task<string> f2 = command2.ExecuteAsync();
            Task<string> f3 = command3.ExecuteAsync();
            Assert.Equal("A", f1.Result);
            Assert.Equal("B", f2.Result);
            Assert.Equal("A", f3.Result);

            Assert.True(command1.executed);
            // both should execute as they are different
            Assert.True(command2.executed);
            // but the 3rd should come from cache
            Assert.False(command3.executed);
            Assert.True(command3.IsResponseFromCache);
            AssertCommandExecutionEvents(command1, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command3, HystrixEventType.SUCCESS, HystrixEventType.RESPONSE_FROM_CACHE);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            AssertSaneHystrixRequestLog(3);
        }
        [Fact]
        public void TestRequestCacheWithSlowExecution()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            SlowCacheableCommand command1 = new SlowCacheableCommand(circuitBreaker, "A", 200);
            SlowCacheableCommand command2 = new SlowCacheableCommand(circuitBreaker, "A", 100);
            SlowCacheableCommand command3 = new SlowCacheableCommand(circuitBreaker, "A", 100);
            SlowCacheableCommand command4 = new SlowCacheableCommand(circuitBreaker, "A", 100);

            Task<string> f1 = command1.ExecuteAsync();
            Task<string> f2 = command2.ExecuteAsync();
            Task<string> f3 = command3.ExecuteAsync();
            Task<string> f4 = command4.ExecuteAsync();

            Assert.Equal("A", f2.Result);
            Assert.Equal("A", f3.Result);
            Assert.Equal("A", f4.Result);
            Assert.Equal("A", f1.Result);

            Assert.True(command1.executed);
            // the second one should not have executed as it should have received the cached value instead
            Assert.False(command2.executed);
            Assert.False(command3.executed);
            Assert.False(command4.executed);

            Assert.True(command1.ExecutionTimeInMilliseconds > -1);
            Assert.False(command1.IsResponseFromCache);
            Assert.True(command2.ExecutionTimeInMilliseconds == -1);
            Assert.True(command2.IsResponseFromCache);
            Assert.True(command3.IsResponseFromCache);
            Assert.True(command3.ExecutionTimeInMilliseconds == -1);
            Assert.True(command4.IsResponseFromCache);
            Assert.True(command4.ExecutionTimeInMilliseconds == -1);
            AssertCommandExecutionEvents(command1, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.SUCCESS, HystrixEventType.RESPONSE_FROM_CACHE);
            AssertCommandExecutionEvents(command3, HystrixEventType.SUCCESS, HystrixEventType.RESPONSE_FROM_CACHE);
            AssertCommandExecutionEvents(command4, HystrixEventType.SUCCESS, HystrixEventType.RESPONSE_FROM_CACHE);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(4);
            output.WriteLine("HystrixRequestLog: " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        }
        [Fact]
        public void TestNoRequestCache3()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            SuccessfulCacheableCommand<String> command1 = new SuccessfulCacheableCommand<String>(circuitBreaker, false, "A");
            SuccessfulCacheableCommand<String> command2 = new SuccessfulCacheableCommand<String>(circuitBreaker, false, "B");
            SuccessfulCacheableCommand<String> command3 = new SuccessfulCacheableCommand<String>(circuitBreaker, false, "A");

            Assert.True(command1.IsCommandRunningInThread);

            Task<string> f1 = command1.ExecuteAsync();
            Task<string> f2 = command2.ExecuteAsync();
            Task<string> f3 = command3.ExecuteAsync();

            Assert.Equal("A", f1.Result);
            Assert.Equal("B", f2.Result);
            Assert.Equal("A", f3.Result);

            Assert.True(command1.executed);
            // both should execute as they are different
            Assert.True(command2.executed);
            // this should also execute since we disabled the cache
            Assert.True(command3.executed);

            AssertCommandExecutionEvents(command1, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command3, HystrixEventType.SUCCESS);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(3);
        }
        [Fact]
        public void TestRequestCacheViaQueueSemaphore1()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            SuccessfulCacheableCommandViaSemaphore command1 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "A");
            SuccessfulCacheableCommandViaSemaphore command2 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "B");
            SuccessfulCacheableCommandViaSemaphore command3 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "A");

            Assert.False(command1.IsCommandRunningInThread);

            Task<string> f1 = command1.ExecuteAsync();
            Task<string> f2 = command2.ExecuteAsync();
            Task<string> f3 = command3.ExecuteAsync();

            Assert.Equal("A", f1.Result);
            Assert.Equal("B", f2.Result);
            Assert.Equal("A", f3.Result);

            Assert.True(command1.executed);
            // both should execute as they are different
            Assert.True(command2.executed);
            // but the 3rd should come from cache
            Assert.False(command3.executed);
            Assert.True(command3.IsResponseFromCache);
            Assert.True(command3.ExecutionTimeInMilliseconds == -1);
            AssertCommandExecutionEvents(command1, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command3, HystrixEventType.SUCCESS, HystrixEventType.RESPONSE_FROM_CACHE);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(3);
        }
        [Fact]
        public void TestNoRequestCacheViaQueueSemaphore1()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            SuccessfulCacheableCommandViaSemaphore command1 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "A");
            SuccessfulCacheableCommandViaSemaphore command2 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "B");
            SuccessfulCacheableCommandViaSemaphore command3 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "A");

            Assert.False(command1.IsCommandRunningInThread);

            Task<string> f1 = command1.ExecuteAsync();
            Task<string> f2 = command2.ExecuteAsync();
            Task<string> f3 = command3.ExecuteAsync();

            Assert.Equal("A", f1.Result);
            Assert.Equal("B", f2.Result);
            Assert.Equal("A", f3.Result);

            Assert.True(command1.executed);
            // both should execute as they are different
            Assert.True(command2.executed);
            // this should also execute because caching is disabled
            Assert.True(command3.executed);
            AssertCommandExecutionEvents(command1, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command3, HystrixEventType.SUCCESS);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(3);
        }
        [Fact]
        public void TestRequestCacheViaExecuteSemaphore1()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            SuccessfulCacheableCommandViaSemaphore command1 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "A");
            SuccessfulCacheableCommandViaSemaphore command2 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "B");
            SuccessfulCacheableCommandViaSemaphore command3 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "A");

            Assert.False(command1.IsCommandRunningInThread);

            String f1 = command1.Execute();
            String f2 = command2.Execute();
            String f3 = command3.Execute();

            Assert.Equal("A", f1);
            Assert.Equal("B", f2);
            Assert.Equal("A", f3);

            Assert.True(command1.executed);
            // both should execute as they are different
            Assert.True(command2.executed);
            // but the 3rd should come from cache
            Assert.False(command3.executed);
            AssertCommandExecutionEvents(command1, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command3, HystrixEventType.SUCCESS, HystrixEventType.RESPONSE_FROM_CACHE);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(3);
        }
        [Fact]
        public void TestNoRequestCacheViaExecuteSemaphore1()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            SuccessfulCacheableCommandViaSemaphore command1 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "A");
            SuccessfulCacheableCommandViaSemaphore command2 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "B");
            SuccessfulCacheableCommandViaSemaphore command3 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "A");

            Assert.False(command1.IsCommandRunningInThread);

            String f1 = command1.Execute();
            String f2 = command2.Execute();
            String f3 = command3.Execute();

            Assert.Equal("A", f1);
            Assert.Equal("B", f2);
            Assert.Equal("A", f3);

            Assert.True(command1.executed);
            // both should execute as they are different
            Assert.True(command2.executed);
            // this should also execute because caching is disabled
            Assert.True(command3.executed);
            AssertCommandExecutionEvents(command1, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command3, HystrixEventType.SUCCESS);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(3);
        }

        [Fact]
        public void TestNoRequestCacheOnTimeoutThrowsException()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            NoRequestCacheTimeoutWithoutFallback r1 = new NoRequestCacheTimeoutWithoutFallback(circuitBreaker);
            try
            {
                output.WriteLine("r1 value: " + r1.Execute());
                // we should have thrown an exception
                Assert.True(false, "expected a timeout");
            }
            catch (HystrixRuntimeException )
            {
                Assert.True(r1.IsResponseTimedOut);
                // what we want
            }

            NoRequestCacheTimeoutWithoutFallback r2 = new NoRequestCacheTimeoutWithoutFallback(circuitBreaker);
            try
            {
                r2.Execute();
                // we should have thrown an exception
                Assert.True(false, "expected a timeout");
            }
            catch (HystrixRuntimeException )
            {
                Assert.True(r2.IsResponseTimedOut);
                // what we want
            }

            NoRequestCacheTimeoutWithoutFallback r3 = new NoRequestCacheTimeoutWithoutFallback(circuitBreaker);
            Task<bool> f3 = r3.ExecuteAsync();
            try
            {
                var res = f3.Result;
                // we should have thrown an exception
                Assert.True(false, "expected a timeout");
            }
            catch (Exception )
            {
                //e.printStackTrace();
                Assert.True(r3.IsResponseTimedOut);
                // what we want
            }

            Time.Wait(500); // timeout on command is set to 200ms

            NoRequestCacheTimeoutWithoutFallback r4 = new NoRequestCacheTimeoutWithoutFallback(circuitBreaker);
            try
            {
                r4.Execute();
                // we should have thrown an exception
                Assert.True(false, "expected a timeout");
            }
            catch (HystrixRuntimeException )
            {
                Assert.True(r4.IsResponseTimedOut);
                Assert.False(r4.IsResponseFromFallback);
                // what we want
            }

            AssertCommandExecutionEvents(r1, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_MISSING);
            AssertCommandExecutionEvents(r2, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_MISSING);
            AssertCommandExecutionEvents(r3, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_MISSING);
            AssertCommandExecutionEvents(r4, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_MISSING);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(4);
        }

        [Fact]
        public void TestRequestCacheOnTimeoutCausesNullPointerException()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            RequestCacheNullPointerExceptionCase command1 = new RequestCacheNullPointerExceptionCase(circuitBreaker);
            RequestCacheNullPointerExceptionCase command2 = new RequestCacheNullPointerExceptionCase(circuitBreaker);
            RequestCacheNullPointerExceptionCase command3 = new RequestCacheNullPointerExceptionCase(circuitBreaker);

            // Expect it to time out - all results should be false
            Assert.False(command1.Execute());
            Assert.False(command2.Execute()); // return from cache #1
            Assert.False(command3.Execute()); // return from cache #2
            Time.Wait( 500); // timeout on command is set to 200ms

            RequestCacheNullPointerExceptionCase command4 = new RequestCacheNullPointerExceptionCase(circuitBreaker);
            Boolean value = command4.Execute(); // return from cache #3
            Assert.False(value);
            RequestCacheNullPointerExceptionCase command5 = new RequestCacheNullPointerExceptionCase(circuitBreaker);
            Task<bool> f = command5.ExecuteAsync(); // return from cache #4
                                                    // the bug is that we're getting a null Future back, rather than a Future that returns false
            Assert.NotNull(f);
            Assert.False(f.Result);

            Assert.True(command5.IsResponseFromFallback);
            Assert.True(command5.IsResponseTimedOut);
            Assert.False(command5.IsFailedExecution);
            Assert.False(command5.IsResponseShortCircuited);
            Assert.NotNull(command5.ExecutionException);

            AssertCommandExecutionEvents(command1, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_SUCCESS, HystrixEventType.RESPONSE_FROM_CACHE);
            AssertCommandExecutionEvents(command3, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_SUCCESS, HystrixEventType.RESPONSE_FROM_CACHE);
            AssertCommandExecutionEvents(command4, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_SUCCESS, HystrixEventType.RESPONSE_FROM_CACHE);
            AssertCommandExecutionEvents(command5, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_SUCCESS, HystrixEventType.RESPONSE_FROM_CACHE);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(5);
        }

        [Fact]
        public void TestRequestCacheOnTimeoutThrowsException()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            RequestCacheTimeoutWithoutFallback r1 = new RequestCacheTimeoutWithoutFallback(circuitBreaker);
            try
            {
                output.WriteLine("r1 value: " + r1.Execute());
                // we should have thrown an exception
                Assert.True(false, "expected a timeout");
            }
            catch (HystrixRuntimeException )
            {
                Assert.True(r1.IsResponseTimedOut);
                // what we want
            }

            RequestCacheTimeoutWithoutFallback r2 = new RequestCacheTimeoutWithoutFallback(circuitBreaker);
            try
            {
                r2.Execute();
                // we should have thrown an exception
                Assert.True(false, "expected a timeout");
            }
            catch (HystrixRuntimeException )
            {
                Assert.True(r2.IsResponseTimedOut);
                // what we want
            }

            RequestCacheTimeoutWithoutFallback r3 = new RequestCacheTimeoutWithoutFallback(circuitBreaker);
            Task<bool> f3 = r3.ExecuteAsync();
            try
            {
                var res = f3.Result;
                // we should have thrown an exception
                Assert.True(false, "expected a timeout");
            }
            catch (Exception )
            {
                //e.printStackTrace();
                Assert.True(r3.IsResponseTimedOut);
                // what we want
            }

            Time.Wait( 500); // timeout on command is set to 200ms

            RequestCacheTimeoutWithoutFallback r4 = new RequestCacheTimeoutWithoutFallback(circuitBreaker);
            try
            {
                r4.Execute();
                // we should have thrown an exception
                Assert.False(true, "expected a timeout");
            }
            catch (HystrixRuntimeException )
            {
                Assert.True(r4.IsResponseTimedOut);
                Assert.False(r4.IsResponseFromFallback);
                // what we want
            }

            AssertCommandExecutionEvents(r1, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_MISSING);
            AssertCommandExecutionEvents(r2, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_MISSING, HystrixEventType.RESPONSE_FROM_CACHE);
            AssertCommandExecutionEvents(r3, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_MISSING, HystrixEventType.RESPONSE_FROM_CACHE);
            AssertCommandExecutionEvents(r4, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_MISSING, HystrixEventType.RESPONSE_FROM_CACHE);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(4);
        }
        [Fact]
        public void TestRequestCacheOnThreadRejectionThrowsException()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            CountdownEvent completionLatch = new CountdownEvent(1);
            RequestCacheThreadRejectionWithoutFallback r1 = new RequestCacheThreadRejectionWithoutFallback(circuitBreaker, completionLatch);
            try
            {
                output.WriteLine("r1: " + r1.Execute());
                // we should have thrown an exception
                Assert.True(false, "expected a rejection");
            }
            catch (HystrixRuntimeException )
            {
                Assert.True(r1.IsResponseRejected);
                // what we want
            }

            RequestCacheThreadRejectionWithoutFallback r2 = new RequestCacheThreadRejectionWithoutFallback(circuitBreaker, completionLatch);
            try
            {
                output.WriteLine("r2: " + r2.Execute());
                // we should have thrown an exception
                Assert.True(false, "expected a rejection");
            }
            catch (HystrixRuntimeException )
            {
                //                e.printStackTrace();
                Assert.True(r2.IsResponseRejected);
                // what we want
            }

            RequestCacheThreadRejectionWithoutFallback r3 = new RequestCacheThreadRejectionWithoutFallback(circuitBreaker, completionLatch);
            try
            {
                output.WriteLine("f3: " + r3.ExecuteAsync().Result);
                // we should have thrown an exception
                Assert.True(false, "expected a rejection");
            }
            catch (AggregateException )
            {
                //} catch (HystrixRuntimeException e) {
                //                e.printStackTrace();
                Assert.True(r3.IsResponseRejected);
                // what we want
            }

            // let the command finish (only 1 should actually be blocked on this due to the response cache)
            completionLatch.SignalEx();

            // then another after the command has completed
            RequestCacheThreadRejectionWithoutFallback r4 = new RequestCacheThreadRejectionWithoutFallback(circuitBreaker, completionLatch);
            try
            {
                output.WriteLine("r4: " + r4.Execute());
                // we should have thrown an exception
                Assert.True(false, "expected a rejection");
            }
            catch (HystrixRuntimeException )
            {
                //                e.printStackTrace();
                Assert.True(r4.IsResponseRejected);
                Assert.False(r4.IsResponseFromFallback);
                // what we want
            }

            AssertCommandExecutionEvents(r1, HystrixEventType.THREAD_POOL_REJECTED, HystrixEventType.FALLBACK_MISSING);
            AssertCommandExecutionEvents(r2, HystrixEventType.THREAD_POOL_REJECTED, HystrixEventType.FALLBACK_MISSING, HystrixEventType.RESPONSE_FROM_CACHE);
            AssertCommandExecutionEvents(r3, HystrixEventType.THREAD_POOL_REJECTED, HystrixEventType.FALLBACK_MISSING, HystrixEventType.RESPONSE_FROM_CACHE);
            AssertCommandExecutionEvents(r4, HystrixEventType.THREAD_POOL_REJECTED, HystrixEventType.FALLBACK_MISSING, HystrixEventType.RESPONSE_FROM_CACHE);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(4);
        }

        [Fact]
        public void TestBasicExecutionWorksWithoutRequestVariable()
        {
            /* force the RequestVariable to not be initialized */
            HystrixRequestContext.SetContextOnCurrentThread(null);

            TestHystrixCommand<Boolean> command = new SuccessfulTestCommand();
            Assert.True( command.Execute());

            TestHystrixCommand<Boolean> command2 = new SuccessfulTestCommand();
            Assert.True( command2.ExecuteAsync().Result);
        }
        [Fact]
        public void TestCacheKeyExecutionRequiresRequestVariable()
        {
            /* force the RequestVariable to not be initialized */
            HystrixRequestContext.SetContextOnCurrentThread(null);

            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();

            SuccessfulCacheableCommand<String> command = new SuccessfulCacheableCommand<String>(circuitBreaker, true, "one");
            Assert.Throws<HystrixRuntimeException>(() => command.Execute());

            SuccessfulCacheableCommand<String> command2 = new SuccessfulCacheableCommand<String>(circuitBreaker, true, "two");
            Assert.Throws<HystrixRuntimeException>(() => command.ExecuteAsync().Result);

        }
        [Fact]
        public void TestBadRequestExceptionViaExecuteInThread()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            BadRequestCommand command1 = null;
            try
            {
                command1 = new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.THREAD);
                command1.Execute();
                Assert.True(false, "we expect to receive a " + typeof(HystrixBadRequestException).Name);
            }
            catch (HystrixBadRequestException )
            {
                // success
                //e.printStackTrace();
            }

            AssertCommandExecutionEvents(command1, HystrixEventType.BAD_REQUEST);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }

        [Fact]
        public void TestBadRequestExceptionViaQueueInThread()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            BadRequestCommand command1 = null;
            try
            {
                command1 = new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.THREAD);
                var res = command1.ExecuteAsync().Result;
                Assert.True(false, "we expect to receive a " + typeof(HystrixBadRequestException).Name);
            }
            catch (AggregateException e)
            {
                //e.printStackTrace();
                if (e.InnerException is HystrixBadRequestException)
                {
                    // success
                }
                else
                {
                    Assert.True(false, "We expect a " + typeof(HystrixBadRequestException).Name + " but got a " + e.GetType().Name);
                }
            }

            AssertCommandExecutionEvents(command1, HystrixEventType.BAD_REQUEST);
            Assert.NotNull(command1.ExecutionException);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestBadRequestExceptionViaQueueInThreadOnResponseFromCache()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();

            // execute once to cache the value
            BadRequestCommand command1 = null;
            try
            {
                command1 = new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.THREAD);
                command1.Execute();
            }
            catch (Exception )
            {
                // ignore
            }

            BadRequestCommand command2 = null;
            try
            {
                command2 = new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.THREAD);
                var res = command2.ExecuteAsync().Result;
                Assert.True(false, "we expect to receive a " + typeof(HystrixBadRequestException).Name);
            }
            catch (AggregateException e)
            {
                //e.printStackTrace();
                if (e.InnerException is HystrixBadRequestException)
                {
                    // success
                }
                else
                {
                    Assert.False(true, "We expect a " + typeof(HystrixBadRequestException).Name + " but got a " + e.GetType().Name);
                }
            }

            AssertCommandExecutionEvents(command1, HystrixEventType.BAD_REQUEST);
            AssertCommandExecutionEvents(command2, HystrixEventType.BAD_REQUEST, HystrixEventType.RESPONSE_FROM_CACHE);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(2);
        }
        [Fact]
        public void TestBadRequestExceptionViaExecuteInSemaphore()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            BadRequestCommand command1 = new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.SEMAPHORE);
            try
            {
                command1.Execute();
                Assert.True(false, "we expect to receive a " + typeof(HystrixBadRequestException).Name);
            }
            catch (HystrixBadRequestException )
            {
                // success
                //e.printStackTrace();
            }

            AssertCommandExecutionEvents(command1, HystrixEventType.BAD_REQUEST);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestCheckedExceptionViaExecute()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            CommandWithCheckedException command = new CommandWithCheckedException(circuitBreaker);
            try
            {
                command.Execute();
                Assert.True(false, "we expect to receive a " + typeof(Exception).Name);
            }
            catch (Exception e)
            {
                Assert.Equal("simulated checked exception message", e.InnerException.Message);
            }

            Assert.Equal("simulated checked exception message", command.FailedExecutionException.Message);

            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsFailedExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_MISSING);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestCheckedExceptionViaObserve()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            CommandWithCheckedException command = new CommandWithCheckedException(circuitBreaker);
            AtomicReference<Exception> t = new AtomicReference<Exception>();
            CountdownEvent latch = new CountdownEvent(1);
            try
            {
                command.Observe().Subscribe(
                    (n) => { },
                    (e) =>
                    {
                        t.Value = e;
                        latch.SignalEx();
                    },
                    () =>
                    {
                        latch.SignalEx();
                    });

            }
            catch (Exception )
            {
                //e.printStackTrace();
                Assert.True(false, "we should not get anything thrown, it should be emitted via the Observer#onError method");
            }

            latch.Wait(1000);
            Assert.NotNull(t.Value);
            //t.get().printStackTrace();

            Assert.True(t.Value is HystrixRuntimeException);
            Assert.Equal("simulated checked exception message", t.Value.InnerException.Message);
            Assert.Equal("simulated checked exception message", command.FailedExecutionException.Message);
            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsFailedExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_MISSING);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestSemaphoreExecutionWithTimeout()
        {
            TestHystrixCommand<bool> cmd = new InterruptibleCommand(new TestCircuitBreaker(), false);

            output.WriteLine("Starting command");
            long timeMillis = DateTime.Now.Ticks / 10000;
            try
            {
                cmd.Execute();
                Assert.True(false, "Should throw");
            }
            catch (Exception )
            {
                Assert.NotNull(cmd.ExecutionException);

                output.WriteLine("Unsuccessful Execution took : " + ((DateTime.Now.Ticks / 10000) - timeMillis));
                AssertCommandExecutionEvents(cmd, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_MISSING);
                Assert.Equal(0, cmd._metrics.CurrentConcurrentExecutionCount);
                AssertSaneHystrixRequestLog(1);
            }
        }
        [Fact]
        public void TestRecoverableErrorWithNoFallbackThrowsError()
        {
            TestHystrixCommand<int> command = GetRecoverableErrorCommand(ExecutionIsolationStrategy.THREAD, FallbackResultTest.UNIMPLEMENTED);
            try
            {
                command.Execute();
                Assert.False(true, "we expect to receive a " + typeof(Exception).Name);
            }
            catch (Exception e)
            {

                Assert.Equal("Execution ERROR for TestHystrixCommand", e.InnerException.Message);
            }

            Assert.Equal("Execution ERROR for TestHystrixCommand", command.FailedExecutionException.Message);

            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsFailedExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_MISSING);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command._metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestRecoverableErrorMaskedByFallbackButLogged()
        {
            TestHystrixCommand<int> command = GetRecoverableErrorCommand(ExecutionIsolationStrategy.THREAD, FallbackResultTest.SUCCESS);
            Assert.Equal(FlexibleTestHystrixCommand.FALLBACK_VALUE, command.Execute());

            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsFailedExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command._metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }

        [Fact]
        public void TestUnrecoverableErrorThrownWithNoFallback()
        {
            TestHystrixCommand<int> command = GetUnrecoverableErrorCommand(ExecutionIsolationStrategy.THREAD, FallbackResultTest.UNIMPLEMENTED);
            try
            {
                command.Execute();
                Assert.True(false, "we expect to receive a " + typeof(Exception).Name);
            }
            catch (Exception e)
            {

                Assert.Equal("Unrecoverable Error for TestHystrixCommand", e.InnerException.Message);
            }

            Assert.Equal("Unrecoverable Error for TestHystrixCommand", command.FailedExecutionException.Message);

            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsFailedExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.FAILURE);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command._metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }

        [Fact] //even though fallback is implemented, that logic never fires, as this is an unrecoverable error and should be directly propagated to the caller
        public void TestUnrecoverableErrorThrownWithFallback()
        {
            TestHystrixCommand<int> command = GetUnrecoverableErrorCommand(ExecutionIsolationStrategy.THREAD, FallbackResultTest.SUCCESS);
            try
            {
                command.Execute();
                Assert.False(true, "we expect to receive a " + typeof(Exception).Name);
            }
            catch (Exception e)
            {

                Assert.Equal("Unrecoverable Error for TestHystrixCommand", e.InnerException.Message);
            }

            Assert.Equal("Unrecoverable Error for TestHystrixCommand", command.FailedExecutionException.Message);

            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsFailedExecution);
            AssertCommandExecutionEvents(command, HystrixEventType.FAILURE);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command._metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestNonBlockingCommandQueueFiresTimeout()
        {
            //see https://github.com/Netflix/Hystrix/issues/514
            TestHystrixCommand<int> cmd = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 200, FallbackResultTest.SUCCESS, 50);
            cmd.IsFallbackUserDefined = true;

            //await cmd.ExecuteAsync();
            Task t = cmd.ExecuteAsync();
            //t.Start();
            Time.Wait( 200);

            //timeout should occur in 50ms, and underlying thread should run for 500ms
            //therefore, after 200ms, the command should have finished with a fallback on timeout
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            Assert.True(cmd.IsExecutionComplete);
            Assert.True(cmd.IsResponseTimedOut);

            Assert.Equal(0, cmd._metrics.CurrentConcurrentExecutionCount);
        }
        [Fact]
        public void TestExecutionFailureWithFallbackImplementedButDisabled()
        {
            TestHystrixCommand<bool> commandEnabled = new KnownFailureTestCommandWithFallback(new TestCircuitBreaker(), true);
            try
            {
                Assert.False( commandEnabled.Execute());
            }
            catch (Exception )
            {
                //e.printStackTrace();
                Assert.True(false, "We should have received a response from the fallback.");
            }

            TestHystrixCommand<Boolean> commandDisabled = new KnownFailureTestCommandWithFallback(new TestCircuitBreaker(), false);
            try
            {
                Assert.False( commandDisabled.Execute());
                Assert.False(true, "expect exception thrown");
            }
            catch (Exception )
            {
                // expected
            }

            Assert.Equal("we failed with a simulated issue", commandDisabled.FailedExecutionException.Message);

            Assert.True(commandDisabled.IsFailedExecution);
            AssertCommandExecutionEvents(commandEnabled, HystrixEventType.FAILURE, HystrixEventType.FALLBACK_SUCCESS);
            AssertCommandExecutionEvents(commandDisabled, HystrixEventType.FAILURE);
            Assert.NotNull(commandDisabled.ExecutionException);
            Assert.Equal(0, commandDisabled.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(2);
        }
        class TestExecutionTimeoutValueHystrixCommand : HystrixCommand<string>
        {
            public TestExecutionTimeoutValueHystrixCommand(HystrixCommandOptions commandOptions)
                : base(commandOptions)
            {
            }

            protected override string Run()
            {
                Time.WaitUntil(() => { return _token.IsCancellationRequested; }, 3000);
                _token.ThrowIfCancellationRequested();
                return "hello";
            }
            protected override string RunFallback()
            {
                if (IsResponseTimedOut)
                {
                    return "timed-out";
                }
                else
                {
                    return "abc";
                }
            }
        }

        [Fact]
        public void TestExecutionTimeoutValue()
        {

            HystrixCommandOptions properties = new HystrixCommandOptions()
            {
                GroupKey = HystrixCommandGroupKeyDefault.AsKey("TestKey"),
                ExecutionTimeoutInMilliseconds = 50
            };

            HystrixCommand<string> command = new TestExecutionTimeoutValueHystrixCommand(properties);
            command.IsFallbackUserDefined = true;

            string value = command.Execute();
            Assert.True(command.IsResponseTimedOut);
            Assert.Equal("timed-out", value);

        }
        [Fact]
        public void TestObservableTimeoutNoFallbackThreadContext()
        {
            CountdownEvent latch = new CountdownEvent(1);
            AtomicReference<Thread> onErrorThread = new AtomicReference<Thread>();
            AtomicBoolean isRequestContextInitialized = new AtomicBoolean();

            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 200, FallbackResultTest.UNIMPLEMENTED, 50);
            Exception onErrorEvent = null;
            command.ToObservable().Subscribe(
                (n) =>
                {

                },
                (ex) =>
                {
                    onErrorEvent = ex;
                    output.WriteLine("onError: " + ex);
                    output.WriteLine("onError Thread: " + Thread.CurrentThread);
                    output.WriteLine("ThreadContext in onError: " + HystrixRequestContext.IsCurrentThreadInitialized);
                    onErrorThread.Value = Thread.CurrentThread;
                    isRequestContextInitialized.Value = HystrixRequestContext.IsCurrentThreadInitialized;
                    latch.SignalEx();
                },
                () =>
                {
                    latch.SignalEx();
                });

            latch.Wait(5000);

            Assert.True(isRequestContextInitialized.Value);
            Assert.True(onErrorThread.Value != null);

            if (onErrorEvent is HystrixRuntimeException)
            {
                HystrixRuntimeException de = (HystrixRuntimeException)onErrorEvent;
                Assert.NotNull(de.FallbackException);
                Assert.True(de.FallbackException is InvalidOperationException);
                Assert.NotNull(de.ImplementingClass);
                Assert.NotNull(de.InnerException);
                Assert.True(de.InnerException is TimeoutException);
            }
            else
            {
                Assert.False(true, "the exception should be ExecutionException with cause as HystrixRuntimeException");
            }

            Assert.True(command.ExecutionTimeInMilliseconds > -1);
            Assert.True(command.IsResponseTimedOut);
            AssertCommandExecutionEvents(command, HystrixEventType.TIMEOUT, HystrixEventType.FALLBACK_MISSING);
            Assert.NotNull(command.ExecutionException);
            Assert.Equal(0, command.Builder.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestExceptionConvertedToBadRequestExceptionInExecutionHookBypassesCircuitBreaker()
        {
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            ExceptionToBadRequestByExecutionHookCommand command = new ExceptionToBadRequestByExecutionHookCommand(circuitBreaker, ExecutionIsolationStrategy.THREAD);
            try
            {
                command.Execute();
                Assert.False(true, "we expect to receive a " + typeof(HystrixBadRequestException).Name);
            }
            catch (HystrixBadRequestException )
            {
                // success
                //e.printStackTrace();
            }
            catch (Exception e)
            {
                //e.printStackTrace();
                Assert.False(true, "We expect a " + typeof(HystrixBadRequestException).Name + " but got a " + e.GetType().Name);
            }

            AssertCommandExecutionEvents(command, HystrixEventType.BAD_REQUEST);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        [Fact]
        public void TestInterruptFutureOnTimeout()
        {
            // given
            InterruptibleCommand cmd = new InterruptibleCommand(new TestCircuitBreaker(), true);

            // when
            Task<bool> f = cmd.ExecuteAsync();

            // then
            Time.Wait( 500);
            Assert.True(cmd.hasBeenInterrupted);
        }
        [Fact]
        public void TestInterruptObserveOnTimeout()
        {
            // given
            InterruptibleCommand cmd = new InterruptibleCommand(new TestCircuitBreaker(), true);

            // when
            cmd.Observe().Subscribe();

            // then
            Time.Wait( 500);
            Assert.True(cmd.hasBeenInterrupted);
        }
        [Fact]
        public void TestInterruptToObservableOnTimeout()
        {
            // given
            InterruptibleCommand cmd = new InterruptibleCommand(new TestCircuitBreaker(), true);

            // when
            cmd.ToObservable().Subscribe();

            // then
            Time.Wait( 500);
            Assert.True(cmd.hasBeenInterrupted);
        }
        //[Fact]
        //public void TestDoNotInterruptFutureOnTimeoutIfPropertySaysNotTo()
        //{
        //    // given
        //    InterruptibleCommand cmd = new InterruptibleCommand(new TestCircuitBreaker(), false);

        //    // when
        //    Task<bool> f = cmd.ExecuteAsync();

        //    // then
        //    Task.Delay(500).Wait();
        //    Assert.False(cmd.hasBeenInterrupted);
        //}
        //[Fact]
        //public void TestDoNotInterruptObserveOnTimeoutIfPropertySaysNotTo()
        //{
        //    // given
        //    InterruptibleCommand cmd = new InterruptibleCommand(new TestCircuitBreaker(), false);

        //    // when
        //    cmd.Observe().Subscribe();

        //    // then
        //    Task.Delay(500).Wait();
        //    Assert.False(cmd.hasBeenInterrupted);
        //}
        //[Fact]
        //public void TestDoNotInterruptToObservableOnTimeoutIfPropertySaysNotTo()
        //{
        //    // given
        //    InterruptibleCommand cmd = new InterruptibleCommand(new TestCircuitBreaker(), false);

        //    // when
        //    cmd.ToObservable().Subscribe();

        //    // then
        //    Task.Delay(500).Wait();
        //    Assert.False(cmd.hasBeenInterrupted);
        //}
        //[Fact]
        //public void TestCancelFutureWithInterruptionWhenPropertySaysNotTo()
        //{
        //    // given
        //    InterruptibleCommand cmd = new InterruptibleCommand(new TestCircuitBreaker(), true, false, 1000);

        //    // when
        //    CancellationTokenSource cts = new CancellationTokenSource();
        //    Task<bool> f = cmd.ExecuteAsync(cts.Token);
        //    Task.Delay(500).Wait();
        //    cts.Cancel(true);
        //    Task.Delay(500).Wait();

        //    // then
        //    try
        //    {
        //        var result = f.Result;

        //        Assert.True(false, "Should have thrown a CancellationException");
        //    }
        //    catch (Exception e)
        //    {

        //        Assert.False(cmd.hasBeenInterrupted);
        //    }
        //}

        [Fact]
        public void TestCancelFutureWithInterruption()
        {
            // given
            InterruptibleCommand cmd = new InterruptibleCommand(new TestCircuitBreaker(), true, true, 1000);

            // when
            CancellationTokenSource cts = new CancellationTokenSource();
            Task<bool> f = cmd.ExecuteAsync(cts.Token);
            Time.Wait(500);
            cts.Cancel(true);
            Time.Wait(500);

            // then
            try
            {
                var result = f.Result;

                Assert.True(false, "Should have thrown a CancellationException");
            }
            catch (Exception )
            {

                Assert.True(cmd.hasBeenInterrupted);
            }
        }
        //[Fact]
        //public void TestCancelFutureWithoutInterruption()
        //{
        //    // given
        //    InterruptibleCommand cmd = new InterruptibleCommand(new TestCircuitBreaker(), true, true, 1000);

        //    // when
        //    Task<bool> f = cmd.ExecuteAsync();
        //    Task.Delay(500).Wait();
        //    cts.Cancel(false);
        //    Task.Delay(500).Wait();

        //    // then
        //    try
        //    {
        //        var result = f.Result;

        //        Assert.True(false, "Should have thrown a CancellationException");
        //    }
        //    catch (Exception e)
        //    {

        //        Assert.False(cmd.hasBeenInterrupted);
        //    }
        //}


        [Fact]
        public void TestChainedCommand()
        {
            Assert.True(2 == new TestChainedCommandPrimaryCommand(new TestCircuitBreaker()).Execute());
        }


        [Fact]
        public void TestSlowFallback()
        {

            Assert.True(1 == new TestSlowFallbackPrimaryCommand(new TestCircuitBreaker()).Execute());
        }

        [Fact]
        public void TestSemaphoreThreadSafety()
        {
            int NUM_PERMITS = 1;
            SemaphoreSlim s = new SemaphoreSlim(NUM_PERMITS);

            int NUM_THREADS = 10;

            int NUM_TRIALS = 100;

            for (int t = 0; t < NUM_TRIALS; t++)
            {

                output.WriteLine("TRIAL : " + t);

                AtomicInteger numAcquired = new AtomicInteger(0);
                CountdownEvent latch = new CountdownEvent(NUM_THREADS);

                for (int i = 0; i < NUM_THREADS; i++)
                {
                    Task task = new Task(() =>
                        {
                            bool acquired = s.TryAcquire();
                            if (acquired)
                            {
                                try
                                {
                                    numAcquired.IncrementAndGet();
                                    Time.Wait( 100);
                                }
                                catch (Exception ex)
                                {
                                    output.WriteLine(ex.ToString());
                                }
                                finally
                                {
                                    s.Release();
                                }
                            }
                            latch.SignalEx();
                        });
                    task.Start();
                }

                try
                {
                    Assert.True(latch.Wait(10000));
                }
                catch (Exception ex)
                {
                    Assert.True(false, ex.Message);
                }

                Assert.Equal(NUM_PERMITS, numAcquired.Value);
                Assert.Equal(NUM_PERMITS, s.CurrentCount);

            }
        }

        [Fact]
        public void TestCancelledTasksInQueueGetRemoved()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Cancellation-A");
            TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
            SingleThreadedPoolWithQueue pool = new SingleThreadedPoolWithQueue(10, 1);
            TestCommandRejection command1 = new TestCommandRejection(output, key, circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_NOT_IMPLEMENTED);
            TestCommandRejection command2 = new TestCommandRejection(output, key, circuitBreaker, pool, 500, 600, TestCommandRejection.FALLBACK_NOT_IMPLEMENTED);

            // this should go through the queue and into the thread pool
            Task<bool> poolFiller = command1.ExecuteAsync();
            Time.Wait(10); // Let it start
            // this command will stay in the queue until the thread pool is empty
            IObservable<bool> cmdInQueue = command2.Observe();
            IDisposable s = cmdInQueue.Subscribe();
            Time.Wait(10); // Let it get in queue
            Assert.Equal(1, pool.CurrentQueueSize);
            s.Dispose();
            Assert.True(command2._token.IsCancellationRequested);
            //Assert.Equal(0, pool.CurrentQueueSize);
            //make sure we wait for the command to finish so the state is clean for next test
            var result = poolFiller.Result;
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            Time.Wait( 100);

            AssertCommandExecutionEvents(command1, HystrixEventType.SUCCESS);
            AssertCommandExecutionEvents(command2, HystrixEventType.CANCELLED);
            Assert.Equal(0, circuitBreaker.metrics.CurrentConcurrentExecutionCount);
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            AssertSaneHystrixRequestLog(2);
            pool.Dispose();
        }

        [Fact]
        public void TestOnRunStartHookThrowsSemaphoreIsolated()
        {
            AtomicBoolean exceptionEncountered = new AtomicBoolean(false);
            AtomicBoolean onThreadStartInvoked = new AtomicBoolean(false);
            AtomicBoolean onThreadCompleteInvoked = new AtomicBoolean(false);
            AtomicBoolean executionAttempted = new AtomicBoolean(false);

            TestOnRunStartHookThrowsSemaphoreIsolatedFailureInjectionHook failureInjectionHook = new TestOnRunStartHookThrowsSemaphoreIsolatedFailureInjectionHook(onThreadStartInvoked, onThreadCompleteInvoked);

            TestHystrixCommand<int> semaphoreCmd = new TestOnRunStartHookThrowsSemaphoreIsolatedFailureInjectedCommand(ExecutionIsolationStrategy.SEMAPHORE, executionAttempted, failureInjectionHook);
            try
            {
                int result = semaphoreCmd.Execute();
                output.WriteLine("RESULT : " + result);
            }
            catch (Exception )
            {
                //ex.printStackTrace();
                exceptionEncountered.Value = true;
            }
            Assert.True(exceptionEncountered.Value);
            Assert.False(onThreadStartInvoked.Value);
            Assert.False(onThreadCompleteInvoked.Value);
            Assert.False(executionAttempted.Value);
            Assert.Equal(0, semaphoreCmd._metrics.CurrentConcurrentExecutionCount);

        }
        [Fact]
        public void TestOnRunStartHookThrowsThreadIsolated()
        {
            AtomicBoolean exceptionEncountered = new AtomicBoolean(false);
            AtomicBoolean onThreadStartInvoked = new AtomicBoolean(false);
            AtomicBoolean onThreadCompleteInvoked = new AtomicBoolean(false);
            AtomicBoolean executionAttempted = new AtomicBoolean(false);


            TestOnRunStartHookThrowsThreadIsolatedFailureInjectionHook failureInjectionHook = new TestOnRunStartHookThrowsThreadIsolatedFailureInjectionHook(onThreadStartInvoked, onThreadCompleteInvoked);


            TestHystrixCommand<int> threadCmd = new TestOnRunStartHookThrowsThreadIsolatedFailureInjectedCommand(ExecutionIsolationStrategy.THREAD, executionAttempted, failureInjectionHook);
            try
            {
                int result = threadCmd.Execute();
                output.WriteLine("RESULT : " + result);
            }
            catch (Exception )
            {
                //ex.printStackTrace();
                exceptionEncountered.Value = true;
            }
            Assert.True(exceptionEncountered.Value);
            Assert.True(onThreadStartInvoked.Value);
            Assert.True(onThreadCompleteInvoked.Value);
            Assert.False(executionAttempted.Value);
            Assert.Equal(0, threadCmd._metrics.CurrentConcurrentExecutionCount);

        }

        [Fact]
        public void TestEarlyUnsubscribeDuringExecutionViaToObservable()
        {


            HystrixCommand<bool> cmd = new TestEarlyUnsubscribeDuringExecutionViaToObservableAsyncCommand();

            CountdownEvent latch = new CountdownEvent(1);

            IObservable<bool> o = cmd.ToObservable();
            IDisposable s = o.
                    Finally(() =>
                    {
                        output.WriteLine("OnUnsubscribe");
                        latch.SignalEx();
                    }).Subscribe(
                    (b) =>
                    {
                        output.WriteLine("OnNext : " + b);
                    },
                    (e) =>
                    {
                        output.WriteLine("OnError : " + e);
                    },
                    () =>
                    {
                        output.WriteLine("OnCompleted");
                    });


            try
            {
                Time.Wait( 10);
                s.Dispose();
                Assert.True(latch.Wait(200));
                output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                Assert.Equal(cmd.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, cmd.GetExecutionSemaphore().CurrentCount);
                Assert.Equal(cmd.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, cmd.GetFallbackSemaphore().CurrentCount);
                Assert.False(cmd.IsExecutionComplete);
                Assert.Null( cmd.FailedExecutionException);
                Assert.Null(cmd.ExecutionException);
                output.WriteLine("Execution time : " + cmd.ExecutionTimeInMilliseconds);
                Assert.True(cmd.ExecutionTimeInMilliseconds > -1);
                Assert.False(cmd.IsSuccessfulExecution);
                AssertCommandExecutionEvents(cmd, HystrixEventType.CANCELLED);
                Assert.Equal(0, cmd._metrics.CurrentConcurrentExecutionCount);
                AssertSaneHystrixRequestLog(1);
            }
            catch (Exception ex)
            {
                //ex.printStackTrace();
                output.WriteLine(ex.ToString());
            }
        }

        [Fact]
        public void TestEarlyUnsubscribeDuringExecutionViaObserve()
        {
            HystrixCommand<Boolean> cmd = new TestEarlyUnsubscribeDuringExecutionViaObserveAsyncCommand();
            CountdownEvent latch = new CountdownEvent(1);

            IObservable<bool> o = cmd.Observe();
            IDisposable s = o.
                    Finally(() =>
                    {
                        output.WriteLine("OnUnsubscribe");
                        latch.SignalEx();
                    }).Subscribe(
                    (b) =>
                    {
                        output.WriteLine("OnNext : " + b);
                    },
                    (e) =>
                    {
                        output.WriteLine("OnError : " + e);
                    },
                    () =>
                    {
                        output.WriteLine("OnCompleted");
                    });


            try
            {
                Time.Wait( 10);
                s.Dispose();
                Assert.True(latch.Wait(200));
                output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                Assert.Equal(cmd.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, cmd.GetExecutionSemaphore().CurrentCount);
                Assert.Equal(cmd.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, cmd.GetFallbackSemaphore().CurrentCount);
                Assert.False(cmd.IsExecutionComplete);
                Assert.Null( cmd.FailedExecutionException);
                Assert.Null(cmd.ExecutionException);
                output.WriteLine("Execution time : " + cmd.ExecutionTimeInMilliseconds);
                Assert.True(cmd.ExecutionTimeInMilliseconds > -1);
                Assert.False(cmd.IsSuccessfulExecution);
                AssertCommandExecutionEvents(cmd, HystrixEventType.CANCELLED);
                Assert.Equal(0, cmd._metrics.CurrentConcurrentExecutionCount);
                AssertSaneHystrixRequestLog(1);
            }
            catch (Exception ex)
            {
                //ex.printStackTrace();
                output.WriteLine(ex.ToString());
            }


        }

        [Fact]
        public void TestEarlyUnsubscribeDuringFallback()
        {
            HystrixCommand<Boolean> cmd = new TestEarlyUnsubscribeDuringFallbackAsyncCommand();
            CountdownEvent latch = new CountdownEvent(1);

            IObservable<bool> o = cmd.ToObservable();
            IDisposable s = o.
                    Finally(() =>
                    {
                        output.WriteLine("OnUnsubscribe");
                        latch.SignalEx();
                    }).Subscribe(
                    (b) =>
                    {
                        output.WriteLine("OnNext : " + b);
                    },
                    (e) =>
                    {
                        output.WriteLine("OnError : " + e);
                    },
                    () =>
                    {
                        output.WriteLine("OnCompleted");
                        latch.SignalEx();
                    });

            try
            {
                Time.Wait( 10);
                s.Dispose();
                Assert.True(latch.Wait(200));
                output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                Assert.Equal(cmd.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, cmd.GetExecutionSemaphore().CurrentCount);
                Assert.Equal(cmd.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, cmd.GetFallbackSemaphore().CurrentCount);
                Assert.False(cmd.IsExecutionComplete);

                Assert.Equal(0, cmd._metrics.CurrentConcurrentExecutionCount);
                AssertSaneHystrixRequestLog(1);

            }
            catch (Exception ex)
            {
                //ex.printStackTrace();
                output.WriteLine(ex.ToString());
            }
        }
        [Fact]
        public void TestRequestThenCacheHitAndCacheHitUnsubscribed()
        {
            AsyncCacheableCommand original = new AsyncCacheableCommand("foo");
            AsyncCacheableCommand fromCache = new AsyncCacheableCommand("foo");

            AtomicReference<object> originalValue = new AtomicReference<object>(null);
            AtomicReference<object> fromCacheValue = new AtomicReference<object>(null);

            CountdownEvent originalLatch = new CountdownEvent(1);
            CountdownEvent fromCacheLatch = new CountdownEvent(1);

            IObservable<object> originalObservable = original.ToObservable();
            IObservable<object> fromCacheObservable = fromCache.ToObservable();

            IDisposable originalSubscription = originalObservable.Finally(() =>

        {
            output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original Unsubscribe");
            originalLatch.SignalEx();
        }).Subscribe(
                    (b) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnNext : " + b);
                        originalValue.Value = b;
                    },
                    (e) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnError : " + e);
                        originalLatch.SignalEx();
                    },
                    () =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnCompleted");
                        originalLatch.SignalEx();
                    });


            IDisposable fromCacheSubscription = fromCacheObservable.Finally(() =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache Unsubscribe");
                    fromCacheLatch.SignalEx();
                }).Subscribe(
                    (b) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache OnNext : " + b);
                        fromCacheValue.Value = b;
                    },
                    (e) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache OnError : " + e);
                        fromCacheLatch.SignalEx();
                    },
                    () =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache OnCompleted");
                        fromCacheLatch.SignalEx();
                    });


            try
            {
                fromCacheSubscription.Dispose();
                Assert.True(fromCacheLatch.Wait(600));
                Assert.True(originalLatch.Wait(600));
                Assert.Equal(original.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, original.GetExecutionSemaphore().CurrentCount);
                Assert.Equal(original.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, original.GetFallbackSemaphore().CurrentCount);
                Assert.True(original.IsExecutionComplete);
                Assert.True(original.IsExecutedInThread);
                Assert.Null( original.FailedExecutionException);
                Assert.Null(original.ExecutionException);
                Assert.True(original.ExecutionTimeInMilliseconds > -1);
                Assert.True(original.IsSuccessfulExecution);
                AssertCommandExecutionEvents(original, HystrixEventType.SUCCESS);
                Assert.NotNull(originalValue.Value);
                Assert.True((bool)originalValue.Value);
                Assert.Equal(0, original._metrics.CurrentConcurrentExecutionCount);


                Assert.Equal(fromCache.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache.GetExecutionSemaphore().CurrentCount);
                Assert.Equal(fromCache.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache.GetFallbackSemaphore().CurrentCount);
                Assert.False(fromCache.IsExecutionComplete);
                Assert.False(fromCache.IsExecutedInThread);
                Assert.Null( fromCache.FailedExecutionException);
                Assert.Null(fromCache.ExecutionException);
                AssertCommandExecutionEvents(fromCache, HystrixEventType.RESPONSE_FROM_CACHE, HystrixEventType.CANCELLED);
                Assert.True(fromCache.ExecutionTimeInMilliseconds == -1);
                Assert.False(fromCache.IsSuccessfulExecution);
                Assert.Equal(0, fromCache._metrics.CurrentConcurrentExecutionCount);

                Assert.False(original.IsCancelled);  //underlying work
                output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                AssertSaneHystrixRequestLog(2);
            }
            catch (Exception ex)
            {
                output.WriteLine(ex.ToString());

            }
        }

        [Fact]
        public void TestRequestThenCacheHitAndOriginalUnsubscribed()
        {
            AsyncCacheableCommand original = new AsyncCacheableCommand("foo");
            AsyncCacheableCommand fromCache = new AsyncCacheableCommand("foo");

            AtomicReference<object> originalValue = new AtomicReference<object>(null);
            AtomicReference<object> fromCacheValue = new AtomicReference<object>(null);

            CountdownEvent originalLatch = new CountdownEvent(1);
            CountdownEvent fromCacheLatch = new CountdownEvent(1);

            IObservable<object> originalObservable = original.ToObservable();
            IObservable<object> fromCacheObservable = fromCache.ToObservable();

            IDisposable originalSubscription = originalObservable.Finally(() =>
            {
                output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original Unsubscribe");
                originalLatch.SignalEx();

            }).Subscribe(
                (b) =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnNext : " + b);
                    originalValue.Value = b;
                },
                (e) =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnError : " + e);
                    originalLatch.SignalEx();
                },
                () =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnCompleted");
                    originalLatch.SignalEx();
                });

            IDisposable fromCacheSubscription = fromCacheObservable.Finally(() =>
            {
                output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache Unsubscribe");
                fromCacheLatch.SignalEx();

            }).Subscribe(
                    (b) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache OnNext : " + b);
                        fromCacheValue.Value = b;
                    },
                    (e) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache OnError : " + e);
                        fromCacheLatch.SignalEx();
                    },
                    () =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache OnCompleted");
                        fromCacheLatch.SignalEx();
                    });


            try
            {
                Time.Wait( 10);
                originalSubscription.Dispose();
                Assert.True(originalLatch.Wait(600));
                Assert.True(fromCacheLatch.Wait(600));
                Assert.Equal(original.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, original.GetExecutionSemaphore().CurrentCount);
                Assert.Equal(original.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, original.GetFallbackSemaphore().CurrentCount);
                Assert.False(original.IsExecutionComplete);
                Assert.True(original.IsExecutedInThread);
                Assert.Null( original.FailedExecutionException);
                Assert.Null(original.ExecutionException);
                Assert.True(original.ExecutionTimeInMilliseconds > -1);
                Assert.False(original.IsSuccessfulExecution);
                AssertCommandExecutionEvents(original, HystrixEventType.CANCELLED);
                Assert.Null(originalValue.Value);
                Assert.Equal(0, original._metrics.CurrentConcurrentExecutionCount);

                Assert.Equal(fromCache.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache.GetExecutionSemaphore().CurrentCount);
                Assert.Equal(fromCache.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache.GetFallbackSemaphore().CurrentCount);
                Assert.True(fromCache.IsExecutionComplete);
                Assert.False(fromCache.IsExecutedInThread);
                Assert.Null( fromCache.FailedExecutionException);
                Assert.Null(fromCache.ExecutionException);
                AssertCommandExecutionEvents(fromCache, HystrixEventType.SUCCESS, HystrixEventType.RESPONSE_FROM_CACHE);
                Assert.True(fromCache.ExecutionTimeInMilliseconds == -1);
                Assert.True(fromCache.IsSuccessfulExecution);
                Assert.Equal(0, fromCache._metrics.CurrentConcurrentExecutionCount);

                Assert.False(original.IsCancelled);  //underlying work
                output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                AssertSaneHystrixRequestLog(2);
            }
            catch (Exception ex)
            {
                output.WriteLine(ex.ToString());
            }
        }
        [Fact]
        public void TestRequestThenTwoCacheHitsOriginalAndOneCacheHitUnsubscribed()
        {
            AsyncCacheableCommand original = new AsyncCacheableCommand("foo");
            AsyncCacheableCommand fromCache1 = new AsyncCacheableCommand("foo");
            AsyncCacheableCommand fromCache2 = new AsyncCacheableCommand("foo");

            AtomicReference<object> originalValue = new AtomicReference<object>(null);
            AtomicReference<object> fromCache1Value = new AtomicReference<object>(null);
            AtomicReference<object> fromCache2Value = new AtomicReference<object>(null);

            CountdownEvent originalLatch = new CountdownEvent(1);
            CountdownEvent fromCache1Latch = new CountdownEvent(1);
            CountdownEvent fromCache2Latch = new CountdownEvent(1);

            IObservable<object> originalObservable = original.ToObservable();
            IObservable<object> fromCache1Observable = fromCache1.ToObservable();
            IObservable<object> fromCache2Observable = fromCache2.ToObservable();

            IDisposable originalSubscription = originalObservable.Finally(() =>
            {
                output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original Unsubscribe");
                originalLatch.SignalEx();

            }).Subscribe(
                (b) =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnNext : " + b);
                    originalValue.Value = b;
                },
                (e) =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnError : " + e);
                    originalLatch.SignalEx();
                },
                () =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnCompleted");
                    originalLatch.SignalEx();
                });

            IDisposable fromCache1Subscription = fromCache1Observable.Finally(() =>
            {
                output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 Unsubscribe");
                fromCache1Latch.SignalEx();

            }).Subscribe(
                    (b) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 OnNext : " + b);
                        fromCache1Value.Value = b;
                    },
                    (e) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 OnError : " + e);
                        fromCache1Latch.SignalEx();
                    },
                    () =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 OnCompleted");
                        fromCache1Latch.SignalEx();
                    });

            IDisposable fromCache2Subscription = fromCache2Observable.Finally(() =>
            {
                output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 Unsubscribe");
                fromCache2Latch.SignalEx();

            }).Subscribe(
                    (b) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 OnNext : " + b);
                        fromCache2Value.Value = b;
                    },
                    (e) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 OnError : " + e);
                        fromCache2Latch.SignalEx();
                    },
                    () =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 OnCompleted");
                        fromCache2Latch.SignalEx();
                    });


            try
            {
                Time.Wait( 10);
                originalSubscription.Dispose();
                //fromCache1Subscription.Dispose();
                fromCache2Subscription.Dispose();
                Assert.True(originalLatch.Wait(600));
                Assert.True(fromCache1Latch.Wait(600));
                Assert.True(fromCache2Latch.Wait(600));
                output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

                Assert.Equal(original.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, original.GetExecutionSemaphore().CurrentCount);
                Assert.Equal(original.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, original.GetFallbackSemaphore().CurrentCount);
                Assert.False(original.IsExecutionComplete);
                Assert.True(original.IsExecutedInThread);
                Assert.Null( original.FailedExecutionException);
                Assert.Null(original.ExecutionException);
                Assert.True(original.ExecutionTimeInMilliseconds > -1);
                Assert.False(original.IsSuccessfulExecution);
                AssertCommandExecutionEvents(original, HystrixEventType.CANCELLED);
                Assert.Null(originalValue.Value);
                Assert.False(original.IsCancelled);   //underlying work
                Assert.Equal(0, original._metrics.CurrentConcurrentExecutionCount);

                Assert.Equal(fromCache1.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache1.GetExecutionSemaphore().CurrentCount);
                Assert.Equal(fromCache1.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache1.GetFallbackSemaphore().CurrentCount);

                Assert.True(fromCache1.IsExecutionComplete);
                Assert.False(fromCache1.IsExecutedInThread);
                Assert.Null( fromCache1.FailedExecutionException);
                Assert.Null(fromCache1.ExecutionException);
                AssertCommandExecutionEvents(fromCache1, HystrixEventType.SUCCESS, HystrixEventType.RESPONSE_FROM_CACHE);
                Assert.True(fromCache1.ExecutionTimeInMilliseconds == -1);
                Assert.True(fromCache1.IsSuccessfulExecution);
                Assert.True((bool)fromCache1Value.Value);
                Assert.Equal(0, fromCache1._metrics.CurrentConcurrentExecutionCount);

                Assert.Equal(fromCache2.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache2.GetExecutionSemaphore().CurrentCount);
                Assert.Equal(fromCache2.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache2.GetFallbackSemaphore().CurrentCount);

                Assert.False(fromCache2.IsExecutionComplete);
                Assert.False(fromCache2.IsExecutedInThread);
                Assert.Null( fromCache2.FailedExecutionException);
                Assert.Null(fromCache2.ExecutionException);
                AssertCommandExecutionEvents(fromCache2, HystrixEventType.RESPONSE_FROM_CACHE, HystrixEventType.CANCELLED);
                Assert.True(fromCache2.ExecutionTimeInMilliseconds == -1);
                Assert.False(fromCache2.IsSuccessfulExecution);
                Assert.Null(fromCache2Value.Value);
                Assert.Equal(0, fromCache2._metrics.CurrentConcurrentExecutionCount);

                AssertSaneHystrixRequestLog(3);
            }
            catch (Exception ex)
            {
                output.WriteLine(ex.ToString());
            }
        }

        [Fact]
        public void TestRequestThenTwoCacheHitsAllUnsubscribed()
        {
            AsyncCacheableCommand original = new AsyncCacheableCommand("foo");
            AsyncCacheableCommand fromCache1 = new AsyncCacheableCommand("foo");
            AsyncCacheableCommand fromCache2 = new AsyncCacheableCommand("foo");

            CountdownEvent originalLatch = new CountdownEvent(1);
            CountdownEvent fromCache1Latch = new CountdownEvent(1);
            CountdownEvent fromCache2Latch = new CountdownEvent(1);

            IObservable<object> originalObservable = original.ToObservable();
            IObservable<object> fromCache1Observable = fromCache1.ToObservable();
            IObservable<object> fromCache2Observable = fromCache2.ToObservable();

            IDisposable originalSubscription = originalObservable.Finally(() =>
            {
                output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original Unsubscribe");
                originalLatch.SignalEx();

            }).Subscribe(
                (b) =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnNext : " + b);
                },
                (e) =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnError : " + e);
                    originalLatch.SignalEx();
                },
                () =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnCompleted");
                    originalLatch.SignalEx();
                });

            IDisposable fromCache1Subscription = fromCache1Observable.Finally(() =>
            {
                output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 Unsubscribe");
                fromCache1Latch.SignalEx();

            }).Subscribe(
                    (b) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 OnNext : " + b);
                    },
                    (e) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 OnError : " + e);
                        fromCache1Latch.SignalEx();
                    },
                    () =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 OnCompleted");
                        fromCache1Latch.SignalEx();
                    });

            IDisposable fromCache2Subscription = fromCache2Observable.Finally(() =>
            {
                output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 Unsubscribe");
                fromCache2Latch.SignalEx();

            }).Subscribe(
                    (b) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 OnNext : " + b);
                    },
                    (e) =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 OnError : " + e);
                        fromCache2Latch.SignalEx();
                    },
                    () =>
                    {
                        output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 OnCompleted");
                        fromCache2Latch.SignalEx();
                    });



            try
            {
                Time.Wait( 10);
                originalSubscription.Dispose();
                fromCache1Subscription.Dispose();
                fromCache2Subscription.Dispose();
                Assert.True(originalLatch.Wait(200));
                Assert.True(fromCache1Latch.Wait(200));
                Assert.True(fromCache2Latch.Wait(200));
                output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                Assert.Equal(original.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, original.GetExecutionSemaphore().CurrentCount);
                Assert.Equal(original.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, original.GetFallbackSemaphore().CurrentCount);

                Assert.False(original.IsExecutionComplete);
                Assert.True(original.IsExecutedInThread);
                output.WriteLine("FEE : " + original.FailedExecutionException);
                if (original.FailedExecutionException != null)
                {
                    output.WriteLine(original.FailedExecutionException.ToString());
                }
                Assert.Null(original.FailedExecutionException);
                Assert.Null(original.ExecutionException);
                Assert.True(original.ExecutionTimeInMilliseconds > -1);
                Assert.False(original.IsSuccessfulExecution);
                AssertCommandExecutionEvents(original, HystrixEventType.CANCELLED);
                Assert.True(original.IsCancelled);
                Assert.Equal(0, original._metrics.CurrentConcurrentExecutionCount);

                Assert.Equal(fromCache1.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache1.GetExecutionSemaphore().CurrentCount);
                Assert.Equal(fromCache1.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache1.GetFallbackSemaphore().CurrentCount);

                Assert.False(fromCache1.IsExecutionComplete);
                Assert.False(fromCache1.IsExecutedInThread);
                Assert.Null( fromCache1.FailedExecutionException);
                Assert.Null(fromCache1.ExecutionException);
                AssertCommandExecutionEvents(fromCache1, HystrixEventType.RESPONSE_FROM_CACHE, HystrixEventType.CANCELLED);
                Assert.True(fromCache1.ExecutionTimeInMilliseconds == -1);
                Assert.False(fromCache1.IsSuccessfulExecution);
                Assert.Equal(0, fromCache1._metrics.CurrentConcurrentExecutionCount);

                Assert.Equal(fromCache2.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache2.GetExecutionSemaphore().CurrentCount);
                Assert.Equal(fromCache2.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache2.GetFallbackSemaphore().CurrentCount);

                Assert.False(fromCache2.IsExecutionComplete);
                Assert.False(fromCache2.IsExecutedInThread);
                Assert.Null( fromCache2.FailedExecutionException);
                Assert.Null(fromCache2.ExecutionException);
                AssertCommandExecutionEvents(fromCache2, HystrixEventType.RESPONSE_FROM_CACHE, HystrixEventType.CANCELLED);
                Assert.True(fromCache2.ExecutionTimeInMilliseconds == -1);
                Assert.False(fromCache2.IsSuccessfulExecution);
                Assert.Equal(0, fromCache2._metrics.CurrentConcurrentExecutionCount);

                AssertSaneHystrixRequestLog(3);
            }
            catch (Exception ex)
            {
                output.WriteLine(ex.ToString());
            }
        }

        /**
         * Some RxJava operators like take(n), zip receive data in an onNext from upstream and immediately unsubscribe.
         * When upstream is a HystrixCommand, Hystrix may get that unsubscribe before it gets to its onCompleted.
         * This should still be marked as a HystrixEventType.SUCCESS.
         */
        [Fact]
        public void TestUnsubscribingDownstreamOperatorStillResultsInSuccessEventType()
        {
            HystrixCommand<int> cmd = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 100, FallbackResultTest.UNIMPLEMENTED);

            IObservable<int> o = cmd.ToObservable()
                .Do<int>((i) =>
                {

                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " CMD OnNext : " + i);
                },
                (throwable) =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " CMD OnError : " + throwable);
                },
                () =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " CMD OnCompleted");
                })
                .OnSubscribe(() =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " CMD OnSubscribe");
                })
                .OnDispose(() =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " CMD OnUnsubscribe");
                })
                .Take(1)

                .ObserveOn(DefaultScheduler.Instance)
                .Map((i) =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : Doing some more computation in the onNext!!");

                    try
                    {
                        Time.Wait( 100);
                    }
                    catch (Exception ex)
                    {
                        output.WriteLine(ex.ToString());
                    }
                    return i;

                });

            CountdownEvent latch = new CountdownEvent(1);

            o.OnSubscribe(() =>
            {
                output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnSubscribee");

            }).OnDispose(() =>
            {
                output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnUnsubscribe");
            }).Subscribe
                ((i) =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnNext : " + i);
                },
                (e) =>
                {
                    latch.SignalEx();
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnError : " + e);
                },
                () =>
                {
                    latch.SignalEx();
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnCompleted");
                });


            latch.Wait(1000);

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.True(cmd.IsExecutedInThread);
            AssertCommandExecutionEvents(cmd, HystrixEventType.SUCCESS);
        }

        [Fact]
        public void TestUnsubscribeBeforeSubscribe()
        {
            //this may happen in Observable chain, so Hystrix should make sure that command never executes/allocates in this situation
            IObservable<string> error = Observable.Throw<string>(new Exception("foo"));
            HystrixCommand<int> cmd = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 100);
            IObservable<int> cmdResult = cmd.ToObservable()
                    .Do(
                (integer) =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnNext : " + integer);
                },
                (ex) =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnError : " + ex);
                },
                () =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnCompleted");
                })
                .OnSubscribe(() =>
               {
                   output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnSubscribe");
               })
                .OnDispose(() =>
               {
                   output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnUnsubscribe");
               });


            //the zip operator will subscribe to each observable.  there is a race between the error of the first
            //zipped observable terminating the zip and the subscription to the command's observable
            IObservable<string> zipped = Observable.Zip(error, cmdResult, (s, integer) =>
            {
                return s + integer;
            });

            CountdownEvent latch = new CountdownEvent(1);

            zipped.Subscribe(
                (s) =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnNext : " + s);
                },
                (e) =>
                {
                    latch.SignalEx();
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnError : " + e);
                },
                () =>
                {
                    latch.SignalEx();
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnCompleted");
                });


            latch.Wait(1000);
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        }

        [Fact]
        public void TestRxRetry()
        {
            // see https://github.com/Netflix/Hystrix/issues/1100
            // Since each command instance is single-use, the expectation is that applying the .retry() operator
            // results in only a single execution and propagation out of that error
            HystrixCommand<int> cmd = GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.FAILURE, 300, FallbackResultTest.UNIMPLEMENTED, 100);

            CountdownEvent latch = new CountdownEvent(1);

            output.WriteLine(DateTime.Now.Ticks / 10000 + " : Starting");
            IObservable<int> o = cmd.ToObservable().Retry(2);
            output.WriteLine(DateTime.Now.Ticks / 10000 + " Created retried command : " + o);

            o.Subscribe(
                (integer) =>
                {
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnNext : " + integer);
                },
                (e) =>
                {
                    latch.SignalEx();
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnError : " + e);
                },
                () =>
                {
                    latch.SignalEx();
                    output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : OnCompleted");
                });


            latch.Wait(1000);
            output.WriteLine(DateTime.Now.Ticks / 10000 + " ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        }

        /**
         *********************** THREAD-ISOLATED Execution Hook Tests **************************************
         */

        /**
         * Short-circuit? : NO
         * Thread/semaphore: THREAD
         * Thread Pool full? : NO
         * Thread Pool Queue full?: NO
         * Timeout: NO
         * Execution Result: SUCCESS
         */
        [Fact]
        public void TestExecutionHookThreadSuccess()
        {
            AssertHooksOnSuccess(() =>
            {
                return GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS);
            },
            (command) =>
            {

                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(1, 0, 1));
                Assert.True(hook.ExecutionEventsMatch(1, 0, 1));
                Assert.True(hook.FallbackEventsMatch(0, 0, 0));
                string result = hook.executionSequence.ToString();
                // Steeltoe - remove deprecated!
                //Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onExecutionEmit - !onRunSuccess - !onComplete - onEmit - onExecutionSuccess - onThreadComplete - onSuccess - ", result);
                Assert.Equal("onStart - onThreadStart - onExecutionStart - onExecutionEmit - onEmit - onExecutionSuccess - onThreadComplete - onSuccess - ", result);

            });
        }

        /**
         * Short-circuit? : NO
         * Thread/semaphore: THREAD
         * Thread Pool full? : NO
         * Thread Pool Queue full?: NO
         * Timeout: NO
         * Execution Result: synchronous HystrixBadRequestException
         */
        [Fact]
        public void TestExecutionHookThreadBadRequestException()
        {
            AssertHooksOnFailure(() =>
            {
                return GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.BAD_REQUEST);
            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 1, 0));
                Assert.True(hook.FallbackEventsMatch(0, 0, 0));
                Assert.Equal(typeof(HystrixBadRequestException), hook.GetCommandException().GetType());
                Assert.Equal(typeof(HystrixBadRequestException), hook.GetExecutionException().GetType());
                // Steeltoe - remove deprecated!
                //Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onExecutionError - !onRunError - onThreadComplete - onError - ", hook.executionSequence.toString());
                Assert.Equal("onStart - onThreadStart - onExecutionStart - onExecutionError - onThreadComplete - onError - ", hook.executionSequence.ToString());

            });
        }
        /**
          * Short-circuit? : NO
          * Thread/semaphore: THREAD
          * Thread Pool full? : NO
          * Thread Pool Queue full?: NO
          * Timeout: NO
          * Execution Result: synchronous HystrixRuntimeException
          * Fallback: UnsupportedOperationException
          */
        [Fact]
        public void TestExecutionHookThreadExceptionNoFallback()
        {
            AssertHooksOnFailure(() =>
            {
                return GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.FAILURE, 0, FallbackResultTest.UNIMPLEMENTED);

            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 1, 0));
                Assert.True(hook.FallbackEventsMatch(0, 0, 0));
                Assert.Equal(typeof(Exception), hook.GetCommandException().GetType());
                Assert.Equal(typeof(Exception), hook.GetExecutionException().GetType());
                Assert.Null(hook.GetFallbackException());
                // Steeltoe - remove deprecated!
                //Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onExecutionError - !onRunError - onThreadComplete - onError - ", hook.executionSequence.ToString());
                Assert.Equal("onStart - onThreadStart - onExecutionStart - onExecutionError - onThreadComplete - onError - ", hook.executionSequence.ToString());

            });
        }
        /**
         * Short-circuit? : NO
         * Thread/semaphore: THREAD
         * Thread Pool full? : NO
         * Thread Pool Queue full?: NO
         * Timeout: NO
         * Execution Result: synchronous HystrixRuntimeException
         * Fallback: SUCCESS
         */
        [Fact]
        public void TestExecutionHookThreadExceptionSuccessfulFallback()
        {
            AssertHooksOnSuccess(() =>

            {
                var command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.FAILURE, 0, FallbackResultTest.SUCCESS);
                command.IsFallbackUserDefined = true;
                return command;

            },
             (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(1, 0, 1));
                Assert.True(hook.ExecutionEventsMatch(0, 1, 0));
                Assert.True(hook.FallbackEventsMatch(1, 0, 1));
                Assert.Equal(typeof(Exception), hook.GetExecutionException().GetType());
                // Steeltoe - remove deprecated!
                //Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onExecutionError - !onRunError - onThreadComplete - onFallbackStart - onFallbackEmit - !onFallbackSuccess - !onComplete - onEmit - onFallbackSuccess - onSuccess - ", hook.executionSequence.toString());
                Assert.Equal("onStart - onThreadStart - onExecutionStart - onExecutionError - onThreadComplete - onFallbackStart - onFallbackEmit - onEmit - onFallbackSuccess - onSuccess - ", hook.executionSequence.ToString());

            });
        }
        /**
          * Short-circuit? : NO
          * Thread/semaphore: THREAD
          * Thread Pool full? : NO
          * Thread Pool Queue full?: NO
          * Timeout: NO
          * Execution Result: synchronous HystrixRuntimeException
          * Fallback: HystrixRuntimeException
          */
        [Fact]
        public void TestExecutionHookThreadExceptionUnsuccessfulFallback()
        {
            AssertHooksOnFailure(() =>

            {
                var command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.FAILURE, 0, FallbackResultTest.FAILURE);
                command.IsFallbackUserDefined = true;
                return command;

            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 1, 0));
                Assert.True(hook.FallbackEventsMatch(0, 1, 0));
                Assert.Equal(typeof(Exception), hook.GetCommandException().GetType());
                Assert.Equal(typeof(Exception), hook.GetExecutionException().GetType());
                Assert.Equal(typeof(Exception), hook.GetFallbackException().GetType());
                // Steeltoe - remove deprecated!
                //Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onExecutionError - !onRunError - onThreadComplete - onFallbackStart - onFallbackError - onError - ", hook.executionSequence.toString());
                Assert.Equal("onStart - onThreadStart - onExecutionStart - onExecutionError - onThreadComplete - onFallbackStart - onFallbackError - onError - ", hook.executionSequence.ToString());

            });
        }
        /**
         * Short-circuit? : NO
         * Thread/semaphore: THREAD
         * Thread Pool full? : NO
         * Thread Pool Queue full?: NO
         * Timeout: YES
         * Execution Result: SUCCESS (but timeout prior)
         * Fallback: UnsupportedOperationException
         */
        [Fact]
        public void TestExecutionHookThreadTimeoutNoFallbackRunSuccess()
        {
            AssertHooksOnFailure(() =>

            {
                return GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.UNIMPLEMENTED, 200);

            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(0, 0, 0));
                Assert.Equal(typeof(TimeoutException), hook.GetCommandException().GetType());
                Assert.Null(hook.GetFallbackException());
                output.WriteLine("RequestLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                // Steeltoe - remove deprecated!
                //Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onThreadComplete - onError - ", hook.executionSequence.toString());
                Assert.Equal("onStart - onThreadStart - onExecutionStart - onThreadComplete - onError - ", hook.executionSequence.ToString());

            });
        }
        /**
         * Short-circuit? : NO
         * Thread/semaphore: THREAD
         * Thread Pool full? : NO
         * Thread Pool Queue full?: NO
         * Timeout: YES
         * Execution Result: SUCCESS (but timeout prior)
         * Fallback: SUCCESS
         */
        [Fact]
        public void TestExecutionHookThreadTimeoutSuccessfulFallbackRunSuccess()
        {
            AssertHooksOnSuccess(() =>
            {
                var command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.SUCCESS, 200);
                command.IsFallbackUserDefined = true;
                return command;
            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(1, 0, 1));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(1, 0, 1));
                output.WriteLine("RequestLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                // Steeltoe - remove deprecated!
                //Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackEmit - !onFallbackSuccess - !onComplete - onEmit - onFallbackSuccess - onSuccess - ", hook.executionSequence.toString());
                Assert.Equal("onStart - onThreadStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackEmit - onEmit - onFallbackSuccess - onSuccess - ", hook.executionSequence.ToString());
            });
        }
        /**
         * Short-circuit? : NO
         * Thread/semaphore: THREAD
         * Thread Pool full? : NO
         * Thread Pool Queue full?: NO
         * Timeout: YES
         * Execution Result: SUCCESS (but timeout prior)
         * Fallback: synchronous HystrixRuntimeException
         */
        [Fact]
        public void TestExecutionHookThreadTimeoutUnsuccessfulFallbackRunSuccess()
        {
            AssertHooksOnFailure(() =>
            {
                var command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.FAILURE, 200);
                command.IsFallbackUserDefined = true;
                return command;
            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(0, 1, 0));
                Assert.Equal(typeof(TimeoutException), hook.GetCommandException().GetType());
                Assert.Equal(typeof(Exception), hook.GetFallbackException().GetType());
                // Steeltoe - remove deprecated!
                // Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackError - onError - ", hook.executionSequence.toString());
                Assert.Equal("onStart - onThreadStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackError - onError - ", hook.executionSequence.ToString());
            });
        }
        /**
         * Short-circuit? : NO
         * Thread/semaphore: THREAD
         * Thread Pool full? : NO
         * Thread Pool Queue full?: NO
         * Timeout: YES
         * Execution Result: HystrixRuntimeException (but timeout prior)
         * Fallback: UnsupportedOperationException
         */
        [Fact]
        public void TestExecutionHookThreadTimeoutNoFallbackRunFailure()
        {
            AssertHooksOnFailure(() =>
            {
                return GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.FAILURE, 500, FallbackResultTest.UNIMPLEMENTED, 200);

            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(0, 0, 0));
                Assert.Equal(typeof(TimeoutException), hook.GetCommandException().GetType());
                Assert.Null(hook.GetFallbackException());
                // Steeltoe - remove deprecated!
                //Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onThreadComplete - onError - ", hook.executionSequence.toString());
                Assert.Equal("onStart - onThreadStart - onExecutionStart - onThreadComplete - onError - ", hook.executionSequence.ToString());


            });
        }
        /**
          * Short-circuit? : NO
          * Thread/semaphore: THREAD
          * Thread Pool full? : NO
          * Thread Pool Queue full?: NO
          * Timeout: YES
          * Execution Result: HystrixRuntimeException (but timeout prior)
          * Fallback: SUCCESS
          */
        [Fact]
        public void TestExecutionHookThreadTimeoutSuccessfulFallbackRunFailure()
        {
            AssertHooksOnSuccess(() =>
            {
                var command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.FAILURE, 500, FallbackResultTest.SUCCESS, 200);
                command.IsFallbackUserDefined = true;
                return command;
            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                Assert.True(hook.CommandEmissionsMatch(1, 0, 1));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(1, 0, 1));
                // Steeltoe - remove deprecated!
                //Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackEmit - !onFallbackSuccess - !onComplete - onEmit - onFallbackSuccess - onSuccess - ", hook.executionSequence.toString());
                Assert.Equal("onStart - onThreadStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackEmit - onEmit - onFallbackSuccess - onSuccess - ", hook.executionSequence.ToString());
            });
        }

        /**
         * Short-circuit? : NO
         * Thread/semaphore: THREAD
         * Thread Pool full? : NO
         * Thread Pool Queue full?: NO
         * Timeout: YES
         * Execution Result: HystrixRuntimeException (but timeout prior)
         * Fallback: synchronous HystrixRuntimeException
         */
        [Fact]
        public void TestExecutionHookThreadTimeoutUnsuccessfulFallbackRunFailure()
        {
            AssertHooksOnFailure(() =>
            {
                var command = GetCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.FAILURE, 500, FallbackResultTest.FAILURE, 200);
                command.IsFallbackUserDefined = true;
                return command;
            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(0, 1, 0));
                Assert.Equal(typeof(TimeoutException), hook.GetCommandException().GetType());
                Assert.Equal(typeof(Exception), hook.GetFallbackException().GetType());
                // Steeltoe - remove deprecated!
                //Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackError - onError - ", hook.executionSequence.toString());
                Assert.Equal("onStart - onThreadStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackError - onError - ", hook.executionSequence.ToString());

            });
        }

        /**
         * Short-circuit? : NO
         * Thread/semaphore: THREAD
         * Thread Pool full? : YES
         * Thread Pool Queue full?: YES
         * Fallback: UnsupportedOperationException
         */
        [Fact]
        public void TestExecutionHookThreadPoolQueueFullNoFallback()
        {
            SingleThreadedPoolWithQueue pool = null;
            AssertHooksOnFailFast(() =>
            {
                TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
                pool = new SingleThreadedPoolWithQueue(1);
                try
                {
                    // fill the pool
                    GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.UNIMPLEMENTED, circuitBreaker, pool, 600).Observe();
                    Time.Wait(10); // Let it start
                    // fill the queue
                    GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.UNIMPLEMENTED, circuitBreaker, pool, 600).Observe();
                }
                catch (Exception )
                {
                    // ignore
                }
                return GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.UNIMPLEMENTED, circuitBreaker, pool, 600);

            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(0, 0, 0));
                Assert.Equal(typeof(RejectedExecutionException), hook.GetCommandException().GetType());
                Assert.Null(hook.GetFallbackException());
                Assert.Equal("onStart - onError - ", hook.executionSequence.ToString());
                pool.Dispose();
                
            });

        }
        /**
         * Short-circuit? : NO
         * Thread/semaphore: THREAD
         * Thread Pool full? : YES
         * Thread Pool Queue full?: YES
         * Fallback: SUCCESS
         */
        [Fact]
        public void TestExecutionHookThreadPoolQueueFullSuccessfulFallback()
        {
            SingleThreadedPoolWithQueue pool = null;
            AssertHooksOnSuccess(() =>
            {
                TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
                pool = new SingleThreadedPoolWithQueue(1);
                try
                {
                    // fill the pool
                    var lat1 = GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.SUCCESS, circuitBreaker, pool, 600);
                    lat1.IsFallbackUserDefined = true;
                    lat1.Observe();
                    Time.Wait(10); // Let it start
                    // fill the queue
                    var lat2 = GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.SUCCESS, circuitBreaker, pool, 600);
                    lat2.IsFallbackUserDefined = true;
                    lat2.Observe();
                }
                catch (Exception )
                {
                    // ignore
                }

                var lat3 = GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.SUCCESS, circuitBreaker, pool, 600);
                lat3.IsFallbackUserDefined = true;
                return lat3;
            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(1, 0, 1));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(1, 0, 1));
                Assert.Equal("onStart - onFallbackStart - onFallbackEmit - onEmit - onFallbackSuccess - onSuccess - ", hook.executionSequence.ToString());
                pool.Dispose();
            });
        }

        /**
         * Short-circuit? : NO
         * Thread/semaphore: THREAD
         * Thread Pool full? : YES
         * Thread Pool Queue full?: YES
         * Fallback: synchronous HystrixRuntimeException
         */
        [Fact]
        public void TestExecutionHookThreadPoolQueueFullUnsuccessfulFallback()
        {
            SingleThreadedPoolWithQueue pool = null;
            AssertHooksOnFailFast(() =>
            {
                TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
                pool = new SingleThreadedPoolWithQueue(1);
                try
                {
                    // fill the pool
                    var lat1 = GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.FAILURE, circuitBreaker, pool, 600);
                    lat1.IsFallbackUserDefined = true;
                    lat1.Observe();

                    Time.Wait(10); // let it start

                    // fill the queue
                    var lat2 = GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.FAILURE, circuitBreaker, pool, 600);
                    lat2.IsFallbackUserDefined = true;
                    lat2.Observe();
                }
                catch (Exception )
                {
                    // ignore
                }

                var lat3 = GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.FAILURE, circuitBreaker, pool, 600);
                lat3.IsFallbackUserDefined = true;
                return lat3;

            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(0, 1, 0));
                Assert.Equal(typeof(RejectedExecutionException), hook.GetCommandException().GetType());
                Assert.Equal(typeof(Exception), hook.GetFallbackException().GetType());
                Assert.Equal("onStart - onFallbackStart - onFallbackError - onError - ", hook.executionSequence.ToString());
                pool.Dispose();
            });
        }
        /**
         * Short-circuit? : NO
         * Thread/semaphore: THREAD
         * Thread Pool full? : YES
         * Thread Pool Queue full?: N/A
         * Fallback: UnsupportedOperationException
         */
        [Fact]
        public void TestExecutionHookThreadPoolFullNoFallback()
        {
            SingleThreadedPoolWithNoQueue pool = null;
            AssertHooksOnFailFast(() =>
            {
                TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
                pool = new SingleThreadedPoolWithNoQueue();
                try
                {
                    // fill the pool
                    GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.UNIMPLEMENTED, circuitBreaker, pool, 600).Observe();
                }
                catch (Exception )
                {
                    // ignore
                }

                return GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.UNIMPLEMENTED, circuitBreaker, pool, 600);

            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(0, 0, 0));
                Assert.Equal(typeof(RejectedExecutionException), hook.GetCommandException().GetType());
                Assert.Null(hook.GetFallbackException());
                Assert.Equal("onStart - onError - ", hook.executionSequence.ToString());
                pool.Dispose();
            });
        }
        /**
         * Short-circuit? : NO
         * Thread/semaphore: THREAD
         * Thread Pool full? : YES
         * Thread Pool Queue full?: N/A
         * Fallback: SUCCESS
         */
        [Fact]
        public void TestExecutionHookThreadPoolFullSuccessfulFallback()
        {
            SingleThreadedPoolWithNoQueue pool = null;
            AssertHooksOnSuccess(() =>
            {
                TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
                pool = new SingleThreadedPoolWithNoQueue();
                try
                {
                    // fill the pool
                    var lat = GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.SUCCESS, circuitBreaker, pool, 600);
                    lat.IsFallbackUserDefined = true;
                    lat.Observe();
                }
                catch (Exception )
                {
                    // ignore
                }

                var lat2 = GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.SUCCESS, circuitBreaker, pool, 600);
                lat2.IsFallbackUserDefined = true;
                return lat2;

            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(1, 0, 1));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.Equal("onStart - onFallbackStart - onFallbackEmit - onEmit - onFallbackSuccess - onSuccess - ", hook.executionSequence.ToString());
                pool.Dispose();
            });
        }

        /**
         * Short-circuit? : NO
         * Thread/semaphore: THREAD
         * Thread Pool full? : YES
         * Thread Pool Queue full?: N/A
         * Fallback: synchronous HystrixRuntimeException
         */
        [Fact]
        public void TestExecutionHookThreadPoolFullUnsuccessfulFallback()
        {
            SingleThreadedPoolWithNoQueue pool = null;
            AssertHooksOnFailFast(() =>
            {
                TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
                pool = new SingleThreadedPoolWithNoQueue();
                try
                {
                    // fill the pool
                    var lat = GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.FAILURE, circuitBreaker, pool, 600);
                    lat.IsFallbackUserDefined = true;
                    lat.Observe();
                }
                catch (Exception )
                {
                    // ignore
                }

                var lat1 = GetLatentCommand(ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 500, FallbackResultTest.FAILURE, circuitBreaker, pool, 600);
                lat1.IsFallbackUserDefined = true;
                return lat1;
            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(0, 1, 0));
                Assert.Equal(typeof(RejectedExecutionException), hook.GetCommandException().GetType());
                Assert.Equal(typeof(Exception), hook.GetFallbackException().GetType());
                Assert.Equal("onStart - onFallbackStart - onFallbackError - onError - ", hook.executionSequence.ToString());
                pool.Dispose();
            });
        }
        /**
         * Short-circuit? : YES
         * Thread/semaphore: THREAD
         * Fallback: UnsupportedOperationException
         */
        [Fact]
        public void TestExecutionHookThreadShortCircuitNoFallback()
        {
            AssertHooksOnFailFast(() =>
            {
                return GetCircuitOpenCommand(ExecutionIsolationStrategy.THREAD, FallbackResultTest.UNIMPLEMENTED);

            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(0, 0, 0));
                Assert.Equal(typeof(Exception), hook.GetCommandException().GetType());
                Assert.Null(hook.GetFallbackException());
                Assert.Equal("onStart - onError - ", hook.executionSequence.ToString());

            });
        }
        /**
         * Short-circuit? : YES
         * Thread/semaphore: THREAD
         * Fallback: SUCCESS
         */
        [Fact]
        public void TestExecutionHookThreadShortCircuitSuccessfulFallback()
        {
            AssertHooksOnSuccess(() =>
            {
                var command = GetCircuitOpenCommand(ExecutionIsolationStrategy.THREAD, FallbackResultTest.SUCCESS);
                command.IsFallbackUserDefined = true;
                return command;

            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(1, 0, 1));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(1, 0, 1));
                Assert.Equal("onStart - onFallbackStart - onFallbackEmit - onEmit - onFallbackSuccess - onSuccess - ", hook.executionSequence.ToString());

            });
        }
        /**
         * Short-circuit? : YES
         * Thread/semaphore: THREAD
         * Fallback: synchronous HystrixRuntimeException
         */
        [Fact]
        public void TestExecutionHookThreadShortCircuitUnsuccessfulFallback()
        {
            AssertHooksOnFailFast(() =>
            {
                TestCircuitBreaker circuitBreaker = new TestCircuitBreaker();
                circuitBreaker.SetForceShortCircuit(true);
                var cmd = GetCircuitOpenCommand(ExecutionIsolationStrategy.THREAD, FallbackResultTest.FAILURE);
                cmd.IsFallbackUserDefined = true;
                return cmd;
            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(0, 1, 0));
                Assert.Equal(typeof(Exception), hook.GetCommandException().GetType());
                Assert.Equal(typeof(Exception), hook.GetFallbackException().GetType());
                Assert.Equal("onStart - onFallbackStart - onFallbackError - onError - ", hook.executionSequence.ToString());

            });
        }
        /**
         * Short-circuit? : NO
         * Request-cache? : YES
         */
        [Fact]
        public void TestExecutionHookResponseFromCache()
        {
            IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Hook-Cache");
            GetCommand(key, ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 0, FallbackResultTest.UNIMPLEMENTED, 0, new TestCircuitBreaker(), null, 100, CacheEnabledTest.YES, 42, 10, 10).Observe();

            AssertHooksOnSuccess(() =>
            {
                return GetCommand(key, ExecutionIsolationStrategy.THREAD, ExecutionResultTest.SUCCESS, 0, FallbackResultTest.UNIMPLEMENTED, 0, new TestCircuitBreaker(), null, 100, CacheEnabledTest.YES, 42, 10, 10);
            },
            (command) =>
            {
                TestableExecutionHook hook = command.Builder.executionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 0, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(0, 0, 0));
                Assert.Equal("onCacheHit - ", hook.executionSequence.ToString());

            });
        }

        int uniqueNameCounter = 0;
        protected override TestHystrixCommand<int> GetCommand(ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult, int fallbackLatency, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int timeout, CacheEnabledTest cacheEnabled, object value, SemaphoreSlim executionSemaphore, SemaphoreSlim fallbackSemaphore, bool circuitBreakerDisabled)
        {
            IHystrixCommandKey commandKey = HystrixCommandKeyDefault.AsKey("Flexible-" + Interlocked.Increment(ref uniqueNameCounter));
            return FlexibleTestHystrixCommand.From(commandKey, isolationStrategy, executionResult, executionLatency, fallbackResult, fallbackLatency, circuitBreaker, threadPool, timeout, cacheEnabled, value, executionSemaphore, fallbackSemaphore, circuitBreakerDisabled);
        }
        protected override TestHystrixCommand<int> GetCommand(IHystrixCommandKey commandKey, ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult, int fallbackLatency, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int timeout, CacheEnabledTest cacheEnabled, object value, SemaphoreSlim executionSemaphore, SemaphoreSlim fallbackSemaphore, bool circuitBreakerDisabled)
        {
            return FlexibleTestHystrixCommand.From(commandKey, isolationStrategy, executionResult, executionLatency, fallbackResult, fallbackLatency, circuitBreaker, threadPool, timeout, cacheEnabled, value, executionSemaphore, fallbackSemaphore, circuitBreakerDisabled);
        }
        protected override void AssertHooksOnSuccess(Func<TestHystrixCommand<int>> ctor, Action<TestHystrixCommand<int>> assertion)
        {
            AssertExecute(ctor(), assertion, true);
            AssertBlockingQueue(ctor(), assertion, true);
            AssertNonBlockingQueue(ctor(), assertion, true, false);
            AssertBlockingObserve(ctor(), assertion, true);
            AssertNonBlockingObserve(ctor(), assertion, true);
        }
        protected override void AssertHooksOnFailure(Func<TestHystrixCommand<int>> ctor, Action<TestHystrixCommand<int>> assertion)
        {
            AssertExecute(ctor(), assertion, false);
            AssertBlockingQueue(ctor(), assertion, false);
            AssertNonBlockingQueue(ctor(), assertion, false, false);
            AssertBlockingObserve(ctor(), assertion, false);
            AssertNonBlockingObserve(ctor(), assertion, false);
        }
        protected override void AssertHooksOnFailure(Func<TestHystrixCommand<int>> ctor, Action<TestHystrixCommand<int>> assertion, bool failFast)
        {
            AssertExecute(ctor(), assertion, false);
            AssertBlockingQueue(ctor(), assertion, false);
            AssertNonBlockingQueue(ctor(), assertion, false, failFast);
            AssertBlockingObserve(ctor(), assertion, false);
            AssertNonBlockingObserve(ctor(), assertion, false);
        }
        private void AssertExecute(TestHystrixCommand<int> command, Action<TestHystrixCommand<int>> assertion, bool isSuccess)
        {
            output.WriteLine(DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " : Running command.execute() and then assertions...");
            if (isSuccess)
            {
                command.Execute();
            }
            else
            {
                try
                {
                    Object o = command.Execute();
                    Assert.True(false, "Expected a command failure!");
                }
                catch (Exception ex)
                {
                    output.WriteLine("Received expected ex : " + ex);
                    // ex.printStackTrace();
                }
            }

            assertion(command);
        }
        private void AssertBlockingQueue(TestHystrixCommand<int> command, Action<TestHystrixCommand<int>> assertion, bool isSuccess)
        {
            output.WriteLine("Running command.queue(), immediately blocking and then running assertions...");
            if (isSuccess)
            {
                try
                {
                    var rest = command.ExecuteAsync().Result;
                }
                catch (Exception )
                {
                    throw; ;
                }
            }
            else
            {
                try
                {
                    var rest = command.ExecuteAsync().Result;
                    Assert.False(true, "Expected a command failure!");
                }
                catch (OperationCanceledException )
                {
                    throw;
                }
                catch (AggregateException ee)
                {
                    output.WriteLine("Received expected ex : " + ee.InnerException);
                    //ee.getCause().printStackTrace();
                }
                catch (Exception e)
                {
                    output.WriteLine("Received expected ex : " + e);
                    //e.printStackTrace();
                }
            }

            assertion(command);
        }
        private void AssertNonBlockingQueue(TestHystrixCommand<int> command, Action<TestHystrixCommand<int>> assertion, bool isSuccess, bool failFast)
        {
            output.WriteLine("Running command.queue(), sleeping the test thread until command is complete, and then running assertions...");
            Task<int> f = null;
            if (failFast)
            {
                try
                {
                    f = command.ExecuteAsync();
                    Assert.False(true, "Expected a failure when queuing the command");
                }
                catch (Exception ex)
                {
                    output.WriteLine("Received expected fail fast ex : " + ex);
                    //ex.printStackTrace();
                }
            }
            else
            {
                try
                {
                    f = command.ExecuteAsync();
                }
                catch (Exception )
                {
                    throw;
                }
            }
            AwaitCommandCompletion(command);

            assertion(command);

            if (isSuccess)
            {
                try
                {
                    var res = f.Result;
                }
                catch (Exception )
                {
                    throw;
                }
            }
            else
            {
                try
                {
                    var res = f.Result;
                    Assert.False(true, "Expected a command failure!");
                }
                catch (OperationCanceledException )
                {
                    throw;
                }
                catch (AggregateException ee)
                {
                    output.WriteLine("Received expected ex : " + ee.InnerException);
                    //ee.getCause().printStackTrace();
                }
                catch (Exception e)
                {
                    output.WriteLine("Received expected ex : " + e);
                    //e.printStackTrace();
                }
            }
        }
        private void AwaitCommandCompletion<T>(TestHystrixCommand<T> command)
        {
            while (!command.IsExecutionComplete)
            {
                try
                {
                    Time.Wait( 10);
                }
                catch (Exception )
                {
                    throw new Exception("interrupted");
                }
            }
        }

    }

    class FlexibleTestHystrixCommand
    {

        public static int EXECUTE_VALUE = 1;
        public static int FALLBACK_VALUE = 11;

        public static AbstractFlexibleTestHystrixCommand From(IHystrixCommandKey commandKey, ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult, int fallbackLatency, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int timeout, CacheEnabledTest cacheEnabled, object value, SemaphoreSlim executionSemaphore, SemaphoreSlim fallbackSemaphore, bool circuitBreakerDisabled)
        {
            if (fallbackResult.Equals(FallbackResultTest.UNIMPLEMENTED))
            {
                return new FlexibleTestHystrixCommandNoFallback(commandKey, isolationStrategy, executionResult, executionLatency, circuitBreaker, threadPool, timeout, cacheEnabled, value, executionSemaphore, fallbackSemaphore, circuitBreakerDisabled);
            }
            else
            {
                var cmd = new FlexibleTestHystrixCommandWithFallback(commandKey, isolationStrategy, executionResult, executionLatency, fallbackResult, fallbackLatency, circuitBreaker, threadPool, timeout, cacheEnabled, value, executionSemaphore, fallbackSemaphore, circuitBreakerDisabled);
                cmd.IsFallbackUserDefined = true;
                return cmd;
            }
        }
    }
    class FlexibleTestHystrixCommandWithFallback : AbstractFlexibleTestHystrixCommand
    {
        protected readonly FallbackResultTest fallbackResult;
        protected readonly int fallbackLatency;

        public FlexibleTestHystrixCommandWithFallback(IHystrixCommandKey commandKey, ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult, int fallbackLatency, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int timeout, CacheEnabledTest cacheEnabled, object value, SemaphoreSlim executionSemaphore, SemaphoreSlim fallbackSemaphore, bool circuitBreakerDisabled) :
            base(commandKey, isolationStrategy, executionResult, executionLatency, circuitBreaker, threadPool, timeout, cacheEnabled, value, executionSemaphore, fallbackSemaphore, circuitBreakerDisabled)
        {

            this.fallbackResult = fallbackResult;
            this.fallbackLatency = fallbackLatency;
        }


        protected override int RunFallback()
        {
            AddLatency(fallbackLatency);
            if (fallbackResult == FallbackResultTest.SUCCESS)
            {
                return FlexibleTestHystrixCommand.FALLBACK_VALUE;
            }
            else if (fallbackResult == FallbackResultTest.FAILURE)
            {
                throw new Exception("Fallback Failure for TestHystrixCommand");
            }
            else if (fallbackResult == FallbackResultTest.UNIMPLEMENTED)
            {
                return base.RunFallback();
            }
            else
            {
                throw new Exception("You passed in a fallbackResult enum that can't be represented in HystrixCommand: " + fallbackResult);
            }
        }
    }
    class FlexibleTestHystrixCommandNoFallback : AbstractFlexibleTestHystrixCommand
    {
        public FlexibleTestHystrixCommandNoFallback(IHystrixCommandKey commandKey, ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int timeout, CacheEnabledTest cacheEnabled, object value, SemaphoreSlim executionSemaphore, SemaphoreSlim fallbackSemaphore, bool circuitBreakerDisabled) :
                base(commandKey, isolationStrategy, executionResult, executionLatency, circuitBreaker, threadPool, timeout, cacheEnabled, value, executionSemaphore, fallbackSemaphore, circuitBreakerDisabled)
        {

        }
    }
    class AbstractFlexibleTestHystrixCommand : TestHystrixCommand<int>
    {

        protected readonly ExecutionResultTest result;
        protected readonly int executionLatency;
        protected readonly CacheEnabledTest cacheEnabled;
        protected readonly object value;


        protected AbstractFlexibleTestHystrixCommand(IHystrixCommandKey commandKey, ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult, int executionLatency, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int timeout, CacheEnabledTest cacheEnabled, object value, SemaphoreSlim executionSemaphore, SemaphoreSlim fallbackSemaphore, bool circuitBreakerDisabled) :
          base(TestPropsBuilder(circuitBreaker)
                .SetCommandKey(commandKey)
                .SetCircuitBreaker(circuitBreaker)
                .SetMetrics(circuitBreaker.metrics)
                .SetThreadPool(threadPool)
                .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), isolationStrategy, timeout, !circuitBreakerDisabled))
                .SetExecutionSemaphore(executionSemaphore)
                .SetFallbackSemaphore(fallbackSemaphore))

        {
            this.result = executionResult;
            this.executionLatency = executionLatency;
            this.cacheEnabled = cacheEnabled;
            this.value = value;
        }

        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, ExecutionIsolationStrategy isolationStrategy, int timeout, bool circuitBreakerDisabled)
        {
            hystrixCommandOptions.ExecutionIsolationStrategy = isolationStrategy;
            hystrixCommandOptions.ExecutionTimeoutInMilliseconds = timeout;
            hystrixCommandOptions.CircuitBreakerEnabled = circuitBreakerDisabled;
            return hystrixCommandOptions;
        }

        protected override int Run()
        {
            //System.out.println(System.currentTimeMillis() + " : " + Thread.currentThread().getName() + " starting the run() method");
            AddLatency(executionLatency);
            if (result == ExecutionResultTest.SUCCESS)
            {
                return FlexibleTestHystrixCommand.EXECUTE_VALUE;
            }
            else if (result == ExecutionResultTest.FAILURE)
            {
                throw new Exception("Execution Failure for TestHystrixCommand");
            }
            else if (result == ExecutionResultTest.HYSTRIX_FAILURE)
            {
                throw new HystrixRuntimeException(FailureType.COMMAND_EXCEPTION, typeof(AbstractFlexibleTestHystrixCommand), "Execution Hystrix Failure for TestHystrixCommand", new Exception("Execution Failure for TestHystrixCommand"), new Exception("Fallback Failure for TestHystrixCommand"));
            }
            else if (result == ExecutionResultTest.RECOVERABLE_ERROR)
            {
                throw new Exception("Execution ERROR for TestHystrixCommand");
            }
            else if (result == ExecutionResultTest.UNRECOVERABLE_ERROR)
            {
                throw new OutOfMemoryException("Unrecoverable Error for TestHystrixCommand");
            }
            else if (result == ExecutionResultTest.BAD_REQUEST)
            {
                throw new HystrixBadRequestException("Execution BadRequestException for TestHystrixCommand");
            }
            else
            {
                throw new Exception("You passed in a executionResult enum that can't be represented in HystrixCommand: " + result);
            }
        }


        protected override string CacheKey
        {
            get
            {
                if (cacheEnabled == CacheEnabledTest.YES)
                    return value.ToString();
                else
                    return null;
            }
        }

        protected void AddLatency(int latency)
        {
            if (latency > 0)
            {
                try
                {
                    //System.out.println(System.currentTimeMillis() + " : " + Thread.currentThread().getName() + " About to sleep for : " + latency);
                    Time.WaitUntil(() => { return _token.IsCancellationRequested; }, latency);
                    _token.ThrowIfCancellationRequested();
                    //System.out.println(System.currentTimeMillis() + " : " + Thread.currentThread().getName() + " Woke up from sleep!");
                }
                catch (Exception)
                {
                    //output.WriteLine(e.ToString());
                    // ignore and sleep some more to simulate a dependency that doesn't obey interrupts
                    try
                    {
                        Time.Wait( latency);
        
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                    //System.out.println("after interruption with extra sleep");
                    throw;
                }
            }
        }

    }
    class KnownFailureTestCommandWithFallback : TestHystrixCommand<bool>
    {

        public KnownFailureTestCommandWithFallback(TestCircuitBreaker circuitBreaker)
            : base(TestPropsBuilder(circuitBreaker).SetMetrics(circuitBreaker.metrics))
        {
        }

        public KnownFailureTestCommandWithFallback(TestCircuitBreaker circuitBreaker, bool fallbackEnabled)
        :
            base(TestPropsBuilder(circuitBreaker).SetMetrics(circuitBreaker.metrics)
                    .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), fallbackEnabled)))
        {
        }
        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, bool fallbackEnabled)
        {
            hystrixCommandOptions.FallbackEnabled = fallbackEnabled;
            return hystrixCommandOptions;
        }

        protected override bool Run()
        {
            //output.WriteLine("*** simulated failed execution ***");
            throw new Exception("we failed with a simulated issue");
        }


        protected override bool RunFallback()
        {
            return false;
        }
    }
    class TestCommandRejection : TestHystrixCommand<bool>
    {

        public const int FALLBACK_NOT_IMPLEMENTED = 1;
        public const int FALLBACK_SUCCESS = 2;
        public const int FALLBACK_FAILURE = 3;

        private readonly int fallbackBehavior;

        private readonly int sleepTime;
        private ITestOutputHelper output;
  
        public TestCommandRejection(ITestOutputHelper output, IHystrixCommandKey key, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int sleepTime, int timeout, int fallbackBehavior) :
            this(key, circuitBreaker, threadPool, sleepTime, timeout, fallbackBehavior)
        {
            this.output = output;
        }

        public TestCommandRejection(IHystrixCommandKey key, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool, int sleepTime, int timeout, int fallbackBehavior)
        : base(TestPropsBuilder()
                    .SetCommandKey(key)
                    .SetThreadPool(threadPool)
                    .SetCircuitBreaker(circuitBreaker)
                    .SetMetrics(circuitBreaker.metrics)
                    .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), timeout)))
        {
            this.fallbackBehavior = fallbackBehavior;
            this.sleepTime = sleepTime;
        }
        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, int timeout)
        {
            hystrixCommandOptions.ExecutionTimeoutInMilliseconds = timeout;
            return hystrixCommandOptions;
        }

        protected override bool Run()
        {
            long start = DateTime.Now.Ticks / 10000;
            output?.WriteLine(">>> TestCommandRejection running " + sleepTime);
            try
            {
                Time.WaitUntil(() => { return _token.IsCancellationRequested; }, sleepTime);
                _token.ThrowIfCancellationRequested();
                output?.WriteLine(">>> TestCommandRejection finished " + ((DateTime.Now.Ticks / 10000) - start));
            }
            catch (Exception e)
            {
                output?.WriteLine(">>> TestCommandRejection finished " + ((DateTime.Now.Ticks / 10000) - start));
                output?.WriteLine(">>> TestCommandRejection exception: " + e.ToString());
            }
            return true;
        }

        protected override bool RunFallback()
        {
            if (fallbackBehavior == FALLBACK_SUCCESS)
            {
                return false;
            }
            else if (fallbackBehavior == FALLBACK_FAILURE)
            {
                throw new Exception("failed on fallback");
            }
            else
            {
                // FALLBACK_NOT_IMPLEMENTED
                return base.RunFallback();
            }
        }
    }
    class SingleThreadedPoolWithNoQueue : IHystrixThreadPool
    {
        private HystrixThreadPoolOptions options;
        private IHystrixTaskScheduler scheduler;

        public SingleThreadedPoolWithNoQueue()
        {
            options = new HystrixThreadPoolOptions()
            {
                MaxQueueSize = 1,
                CoreSize = 1,
                MaximumSize = 1,
                KeepAliveTimeMinutes = 1,
                QueueSizeRejectionThreshold = 100
            };
            scheduler = new HystrixSyncTaskScheduler(options);
        }

        public bool IsQueueSpaceAvailable
        {
            get
            {
                return true;
            }
        }


        public IHystrixTaskScheduler GetScheduler()
        {
            return scheduler;
        }

        public TaskScheduler GetTaskScheduler()
        {
            return scheduler as TaskScheduler;
        }

        public void MarkThreadExecution()
        {
            // not used for this test
        }


        public void MarkThreadCompletion()
        {
            // not used for this test
        }


        public void MarkThreadRejection()
        {
            // not used for this test
        }

        public void Dispose()
        {
            scheduler.Dispose();
        }

        public int CurrentQueueSize
        {
            get { return scheduler.CurrentQueueSize; }
        }

        public bool IsShutdown
        {
            get { return scheduler.IsShutdown; }
        }
    }

    class SingleThreadedPoolWithQueue : IHystrixThreadPool
    {

        private HystrixThreadPoolOptions options;
        private IHystrixTaskScheduler scheduler;
        public SingleThreadedPoolWithQueue(int queueSize)
            : this(queueSize, 100)
        {
        }

        public SingleThreadedPoolWithQueue(int queueSize, int rejectionQueueSizeThreshold)
        {
            options = new HystrixThreadPoolOptions()
            {
                MaxQueueSize = queueSize,
                CoreSize = 1,
                MaximumSize = 1,
                KeepAliveTimeMinutes = 1,
                QueueSizeRejectionThreshold = rejectionQueueSizeThreshold
            };
            scheduler = new HystrixQueuedTaskScheduler(options);
        }

        public IHystrixTaskScheduler GetScheduler()
        {
            return scheduler;
        }

        public TaskScheduler GetTaskScheduler()
        {
            return scheduler as TaskScheduler;
        }

        public void MarkThreadExecution()
        {
            // not used for this test
        }


        public void MarkThreadCompletion()
        {
            // not used for this test
        }


        public void MarkThreadRejection()
        {
            // not used for this test
        }

        public void Dispose()
        {
            scheduler.Dispose();
        }

        public bool IsQueueSpaceAvailable
        {
            get { return scheduler.IsQueueSpaceAvailable; }
        }

        public int CurrentQueueSize
        {
            get { return scheduler.CurrentQueueSize; }
        }

        public bool IsShutdown
        {
            get { return scheduler.IsShutdown; }
        }
    }
    class CommandWithDisabledTimeout : TestHystrixCommand<bool>
    {
        private readonly int latency;

        public CommandWithDisabledTimeout(int timeout, int latency)
        : base(TestPropsBuilder().SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), timeout)))
        {
            this.latency = latency;
        }

        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, int timeout)
        {
            hystrixCommandOptions.ExecutionTimeoutInMilliseconds = timeout;
            hystrixCommandOptions.ExecutionTimeoutEnabled = false;
            return hystrixCommandOptions;
        }
        protected override bool Run()
        {
            try
            {
                Time.Wait(latency);
                return true;
            }
            catch (Exception )
            {
                return false;
            }
        }

        protected override bool RunFallback()
        {
            return false;
        }
    }
    class TestSemaphoreCommandWithSlowFallback : TestHystrixCommand<bool>
    {

        private readonly int fallbackSleep;

        public TestSemaphoreCommandWithSlowFallback(TestCircuitBreaker circuitBreaker, int fallbackSemaphoreExecutionCount, int fallbackSleep)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.metrics)
              .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), fallbackSemaphoreExecutionCount)))
        {
            this.fallbackSleep = fallbackSleep;
        }

        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, int fallbackSemaphoreExecutionCount)
        {
            hystrixCommandOptions.FallbackIsolationSemaphoreMaxConcurrentRequests = fallbackSemaphoreExecutionCount;
            // hystrixCommandOptions.ExecutionIsolationThreadInterruptOnTimeout = false;
            return hystrixCommandOptions;
        }
        protected override bool Run()
        {
            throw new Exception("run fails");
        }


        protected override bool RunFallback()
        {
            try
            {
                Time.Wait(fallbackSleep);
            }
            catch (Exception )
            {
                //e.printStackTrace();
            }
            return true;
        }
    }
    class TestSemaphoreCommand : TestHystrixCommand<bool>
    {

        private readonly int executionSleep;

        public const int RESULT_SUCCESS = 1;
        public const int RESULT_FAILURE = 2;
        public const int RESULT_BAD_REQUEST_EXCEPTION = 3;

        public readonly int resultBehavior;

        public const int FALLBACK_SUCCESS = 10;
        public const int FALLBACK_NOT_IMPLEMENTED = 11;
        public const int FALLBACK_FAILURE = 12;

        public readonly int fallbackBehavior;

        public TestSemaphoreCommand(TestCircuitBreaker circuitBreaker, int executionSemaphoreCount, int executionSleep, int resultBehavior, int fallbackBehavior)
        :
            base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.metrics)
                 .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), executionSemaphoreCount)))
        {
            this.executionSleep = executionSleep;
            this.resultBehavior = resultBehavior;
            this.fallbackBehavior = fallbackBehavior;
        }

        public TestSemaphoreCommand(TestCircuitBreaker circuitBreaker, SemaphoreSlim semaphore, int executionSleep, int resultBehavior, int fallbackBehavior)
        :
            base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.metrics)
                .SetExecutionSemaphore(semaphore)
                 .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions())
                ))
        {
            this.executionSleep = executionSleep;
            this.resultBehavior = resultBehavior;
            this.fallbackBehavior = fallbackBehavior;
        }

        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, int executionSemaphoreCount)
        {
            hystrixCommandOptions.ExecutionIsolationStrategy = ExecutionIsolationStrategy.SEMAPHORE;
            hystrixCommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests = executionSemaphoreCount;
            return hystrixCommandOptions;
        }
        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions)
        {
            hystrixCommandOptions.ExecutionIsolationStrategy = ExecutionIsolationStrategy.SEMAPHORE;
            return hystrixCommandOptions;
        }
        protected override bool Run()
        {
            try
            {
                Time.Wait(executionSleep);
            }
            catch (Exception )
            {
                //e.printStackTrace();
            }
            if (resultBehavior == RESULT_SUCCESS)
            {
                return true;
            }
            else if (resultBehavior == RESULT_FAILURE)
            {
                throw new Exception("TestSemaphoreCommand failure");
            }
            else if (resultBehavior == RESULT_BAD_REQUEST_EXCEPTION)
            {
                throw new HystrixBadRequestException("TestSemaphoreCommand BadRequestException");
            }
            else
            {
                throw new InvalidOperationException("Didn't use a proper enum for result behavior");
            }
        }



        protected override bool RunFallback()
        {
            if (fallbackBehavior == FALLBACK_SUCCESS)
            {
                return false;
            }
            else if (fallbackBehavior == FALLBACK_FAILURE)
            {
                throw new Exception("fallback failure");
            }
            else
            { //FALLBACK_NOT_IMPLEMENTED
                return base.RunFallback();
            }
        }
    }
    class TestSemaphoreCommandWithFallback : TestHystrixCommand<Boolean>
    {

        private readonly int executionSleep;
        private readonly bool fallback;

        public TestSemaphoreCommandWithFallback(TestCircuitBreaker circuitBreaker, int executionSemaphoreCount, int executionSleep, bool fallback)
    :
        base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.metrics)
             .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), executionSemaphoreCount)))
        {
            this.executionSleep = executionSleep;
            this.fallback = fallback;
        }

        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, int executionSemaphoreCount)
        {
            hystrixCommandOptions.ExecutionIsolationStrategy = ExecutionIsolationStrategy.SEMAPHORE;
            hystrixCommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests = executionSemaphoreCount;
            return hystrixCommandOptions;
        }
        protected override bool Run()
        {
            try
            {
                Time.Wait(executionSleep);
            }
            catch (Exception )
            {
                //e.printStackTrace();
            }
            return true;
        }

        protected override bool RunFallback()
        {
            return fallback;
        }

    }
    class LatchedSemaphoreCommand : TestHystrixCommand<bool>
    {

        private readonly CountdownEvent startLatch;
        private readonly CountdownEvent waitLatch;


        public LatchedSemaphoreCommand(TestCircuitBreaker circuitBreaker, SemaphoreSlim semaphore, CountdownEvent startLatch, CountdownEvent waitLatch)
            : this("Latched", circuitBreaker, semaphore, startLatch, waitLatch)
        {
        }

        public LatchedSemaphoreCommand(string commandName, TestCircuitBreaker circuitBreaker, SemaphoreSlim semaphore,
                                        CountdownEvent startLatch, CountdownEvent waitLatch)
        :
            base(TestPropsBuilder()
                    .SetCommandKey(HystrixCommandKeyDefault.AsKey(commandName))
                    .SetCircuitBreaker(circuitBreaker)
                    .SetMetrics(circuitBreaker.metrics)
                    .SetExecutionSemaphore(semaphore)
                  .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions())))
        {
            this.startLatch = startLatch;
            this.waitLatch = waitLatch;
        }
        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions)
        {
            hystrixCommandOptions.ExecutionIsolationStrategy = ExecutionIsolationStrategy.SEMAPHORE;
            hystrixCommandOptions.CircuitBreakerEnabled = false;
            return hystrixCommandOptions;
        }

        protected override bool Run()
        {
            // signals caller that run has started
            this.startLatch.SignalEx();

            try
            {
                // waits for caller to countDown latch
                this.waitLatch.Wait();
            }
            catch (Exception )
            {
                //e.printStackTrace();
                return false;
            }
            return true;
        }
    }
    class DynamicOwnerTestCommand : TestHystrixCommand<bool>
    {

        public DynamicOwnerTestCommand(IHystrixCommandGroupKey owner)
            : base(TestPropsBuilder().SetOwner(owner))
        {
        }

        protected override bool Run()
        {
            //output.WriteLine("successfully executed");
            return true;
        }

    }
    class DynamicOwnerAndKeyTestCommand : TestHystrixCommand<bool>
    {

        public DynamicOwnerAndKeyTestCommand(IHystrixCommandGroupKey owner, IHystrixCommandKey key)
        : base(TestPropsBuilder().SetOwner(owner).SetCommandKey(key).SetCircuitBreaker(null).SetMetrics(null))
        {
            // we specifically are NOT passing in a circuit breaker here so we test that it creates a new one correctly based on the dynamic key
        }


        protected override bool Run()
        {
            //output.WriteLine("successfully executed");
            return true;
        }

    }
    class SuccessfulCacheableCommand<T> : TestHystrixCommand<T>
    {

        private bool cacheEnabled;
        public volatile bool executed = false;
        private T value;

        public SuccessfulCacheableCommand(TestCircuitBreaker circuitBreaker, bool cacheEnabled, T value)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.metrics))
        {
            this.value = value;
            this.cacheEnabled = cacheEnabled;
        }


        protected override T Run()
        {
            executed = true;
            //output.WriteLine("successfully executed");
            return value;
        }

        public bool IsCommandRunningInThread
        {
            get { return base.CommandOptions.ExecutionIsolationStrategy.Equals(ExecutionIsolationStrategy.THREAD); }
        }


        protected override string CacheKey
        {
            get
            {
                if (cacheEnabled)
                    return value.ToString();
                else
                    return null;
            }
        }
    }
    class SlowCacheableCommand : TestHystrixCommand<String>
    {

        private readonly String value;
        private readonly int duration;
        public volatile bool executed = false;

        public SlowCacheableCommand(TestCircuitBreaker circuitBreaker, String value, int duration)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.metrics))
        {

            this.value = value;
            this.duration = duration;
        }


        protected override string Run()
        {
            executed = true;
            try
            {
                Time.Wait(duration);
            }
            catch (Exception )
            {

            }
            //output.WriteLine("successfully executed");
            return value;
        }


        protected override string CacheKey
        {
            get { return value; }
        }
    }
    class SuccessfulCacheableCommandViaSemaphore : TestHystrixCommand<String>
    {

        private readonly bool cacheEnabled;
        public volatile bool executed = false;
        private readonly string value;

        public SuccessfulCacheableCommandViaSemaphore(TestCircuitBreaker circuitBreaker, bool cacheEnabled, string value)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.metrics)
            .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions())))
        {
            this.value = value;
            this.cacheEnabled = cacheEnabled;
        }
        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions)
        {
            hystrixCommandOptions.ExecutionIsolationStrategy = ExecutionIsolationStrategy.SEMAPHORE;
            hystrixCommandOptions.CircuitBreakerEnabled = false;
            return hystrixCommandOptions;
        }

        protected override string Run()
        {
            executed = true;
            //output.WriteLine("successfully executed");
            return value;
        }

        public bool IsCommandRunningInThread
        {
            get { return base.CommandOptions.ExecutionIsolationStrategy.Equals(ExecutionIsolationStrategy.THREAD); }
        }

        protected override string CacheKey
        {
            get
            {
                if (cacheEnabled)
                    return value;
                else
                    return null;
            }
        }
    }
    class NoRequestCacheTimeoutWithoutFallback : TestHystrixCommand<bool>
    {
        public NoRequestCacheTimeoutWithoutFallback(TestCircuitBreaker circuitBreaker)
            : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.metrics)
                      .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions())))
        {

            // we want it to timeout
        }
        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions)
        {
            hystrixCommandOptions.ExecutionTimeoutInMilliseconds = 200;
            hystrixCommandOptions.CircuitBreakerEnabled = false;
            return hystrixCommandOptions;
        }

        protected override bool Run()
        {
            try
            {
                Time.WaitUntil(() => { return _token.IsCancellationRequested; }, 500);
                _token.ThrowIfCancellationRequested();

            }
            catch (Exception)
            {
                //output.WriteLine(">>>> Sleep Interrupted: " + e.Message);
                //                    e.printStackTrace();
                throw;
            }
            return true;
        }

        protected override string CacheKey
        {
            get { return null; }
        }
    }
    class RequestCacheNullPointerExceptionCase : TestHystrixCommand<Boolean>
    {
        public RequestCacheNullPointerExceptionCase(TestCircuitBreaker circuitBreaker)
            : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.metrics)
                  .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions())))
        {
            // we want it to timeout
        }

        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions)
        {
            hystrixCommandOptions.ExecutionTimeoutInMilliseconds = 200;
            return hystrixCommandOptions;
        }
        protected override bool Run()
        {
            try
            {
                Time.WaitUntil(() => { return _token.IsCancellationRequested; }, 500);
                _token.ThrowIfCancellationRequested();

            }
            catch (Exception )
            {
                //e.printStackTrace();
                throw;
            }
            return true;
        }


        protected override bool RunFallback()
        {
            return false;
        }


        protected override string CacheKey
        {
            get { return "A"; }
        }
    }
    class RequestCacheTimeoutWithoutFallback : TestHystrixCommand<Boolean>
    {
        public RequestCacheTimeoutWithoutFallback(TestCircuitBreaker circuitBreaker)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.metrics)
                    .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions())))
        {
            // we want it to timeout
        }
        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions)
        {
            hystrixCommandOptions.ExecutionTimeoutInMilliseconds = 200;
            hystrixCommandOptions.CircuitBreakerEnabled = false;
            return hystrixCommandOptions;
        }

        Stopwatch sw = new Stopwatch();
        protected override bool Run()
        {
            try
            {
   
                sw.Start();
                Time.WaitUntil(() => { return _token.IsCancellationRequested; }, 500);
                _token.ThrowIfCancellationRequested();
                sw.Stop();
            }
            catch (Exception)
            {
                sw.Stop();
                //output.WriteLine(">>>> Sleep Interrupted: " + e.Message);
                //                    e.printStackTrace();
                throw;
            }
            return true;
        }

        protected override string CacheKey
        {
            get { return "A"; }
        }
    }
    class RequestCacheThreadRejectionWithoutFallbackTaskScheduler : HystrixTaskScheduler
    {
        public RequestCacheThreadRejectionWithoutFallbackTaskScheduler(HystrixThreadPoolOptions options) : base(options)
        {
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return null;
        }

        protected override void QueueTask(Task task)
        {
            throw new RejectedExecutionException("Rejected command because task queue queueSize is at rejection threshold.");
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }
    }
    class RequestCacheThreadRejectionWithoutFallbackThreadPool : IHystrixThreadPool
    {
        IHystrixTaskScheduler _scheduler = new RequestCacheThreadRejectionWithoutFallbackTaskScheduler(new HystrixThreadPoolOptions());
        public IHystrixTaskScheduler GetScheduler()
        {
            return _scheduler;
        }

        public TaskScheduler GetTaskScheduler()
        {
            return _scheduler as TaskScheduler;
        }

        public void MarkThreadExecution()
        {
            // not used for this test
        }


        public void MarkThreadCompletion()
        {
            // not used for this test
        }


        public void MarkThreadRejection()
        {
            // not used for this test
        }

        public void Dispose()
        {

        }

        public bool IsQueueSpaceAvailable
        {
            get { return false; }

        }
        public bool IsShutdown
        {
            get { return _scheduler.IsShutdown; }
        }

    }
    class RequestCacheThreadRejectionWithoutFallback : TestHystrixCommand<bool>
    {

        readonly CountdownEvent completionLatch;

        public RequestCacheThreadRejectionWithoutFallback(TestCircuitBreaker circuitBreaker, CountdownEvent completionLatch)
            : base(TestPropsBuilder()
                .SetCircuitBreaker(circuitBreaker)
                .SetMetrics(circuitBreaker.metrics)
                .SetThreadPool(new RequestCacheThreadRejectionWithoutFallbackThreadPool()))
        {

            this.completionLatch = completionLatch;
        }

        protected override bool Run()
        {
            try
            {
                if (completionLatch.Wait(1000))
                {
                    throw new Exception("timed out waiting on completionLatch");
                }
            }
            catch (Exception )
            {
                throw;
            }
            return true;
        }

        protected override string CacheKey
        {
            get { return "A"; }
        }
    }
    class SuccessfulTestCommand : TestHystrixCommand<bool>
    {

        public SuccessfulTestCommand()
        : this(HystrixCommandOptionsTest.GetUnitTestOptions())
        {
        }

        public SuccessfulTestCommand(HystrixCommandOptions properties)
            : base(TestPropsBuilder().SetCommandOptionDefaults(properties))
        {

        }


        protected override bool Run()
        {
            return true;
        }

    }
    class BadRequestCommand : TestHystrixCommand<Boolean>
    {

        public BadRequestCommand(TestCircuitBreaker circuitBreaker, ExecutionIsolationStrategy isolationType)
        : base(TestPropsBuilder()
            .SetCircuitBreaker(circuitBreaker)
            .SetMetrics(circuitBreaker.metrics)
             .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), isolationType)))
        {
        }
        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, ExecutionIsolationStrategy isolationType)
        {
            hystrixCommandOptions.ExecutionIsolationStrategy = isolationType;
            return hystrixCommandOptions;
        }


        protected override bool Run()
        {
            throw new HystrixBadRequestException("Message to developer that they passed in bad data or something like that.");
        }

        protected override bool RunFallback()
        {
            return false;
        }

        protected override string CacheKey
        {
            get { return "one"; }
        }

    }
    class CommandWithCheckedException : TestHystrixCommand<Boolean>
    {

        public CommandWithCheckedException(TestCircuitBreaker circuitBreaker)
        : base(TestPropsBuilder()
                .SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.metrics))
        {
        }


        protected override bool Run()
        {
            throw new IOException("simulated checked exception message");
        }

    }
    class InterruptibleCommand : TestHystrixCommand<bool>
    {

        public InterruptibleCommand(TestCircuitBreaker circuitBreaker, bool shouldInterrupt, bool shouldInterruptOnCancel, int timeoutInMillis)
        : base(TestPropsBuilder()
                .SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.metrics)
                .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), shouldInterrupt, shouldInterruptOnCancel, timeoutInMillis)))
        {
        }
        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, bool shouldInterrupt, bool shouldInterruptOnCancel, int timeoutInMillis)
        {
            hystrixCommandOptions.ExecutionTimeoutInMilliseconds = timeoutInMillis;
            return hystrixCommandOptions;
        }
        public InterruptibleCommand(TestCircuitBreaker circuitBreaker, bool shouldInterrupt)
            : this(circuitBreaker, shouldInterrupt, false, 100)
        {
        }

        public volatile bool hasBeenInterrupted;

        public bool HasBeenInterrupted
        {
            get { return hasBeenInterrupted; }
        }


        protected override bool Run()
        {
            try
            {
                Time.WaitUntil(() => { return _token.IsCancellationRequested; }, 2000);
                _token.ThrowIfCancellationRequested();

            }
            catch (Exception )
            {
                //output.WriteLine("Interrupted!");
                //e.printStackTrace();
                hasBeenInterrupted = true;
                throw;
            }

            return hasBeenInterrupted;
        }
    }
    class EventCommand : HystrixCommand<string>
    {
        public EventCommand() : base(GetTestOptions())
        {
        }
        private static HystrixCommandOptions GetTestOptions()
        {
            HystrixCommandOptions options = new HystrixCommandOptions()
            {
                GroupKey = HystrixCommandGroupKeyDefault.AsKey("eventGroup"),
                FallbackIsolationSemaphoreMaxConcurrentRequests = 3
            };
            return options;
        }
        protected override string Run()
        {
            //output.WriteLine(Thread.CurrentThread.ManagedThreadId + " : In run()");
            throw new Exception("run_exception");
        }


        protected override string RunFallback()
        {
            try
            {
                //output.WriteLine(Thread.CurrentThread.ManagedThreadId + " : In fallback => " + ExecutionEvents);
                Time.WaitUntil(() => { return _token.IsCancellationRequested; }, 30000);
                _token.ThrowIfCancellationRequested();

            }
            catch (Exception )
            {
                //output.WriteLine(Thread.CurrentThread.ManagedThreadId + " : Interruption occurred");
            }
            //output.WriteLine(Thread.CurrentThread.ManagedThreadId + " : CMD Success Result");
            return "fallback";
        }
    }
    class ExceptionToBadRequestByExecutionHookCommandExecutionHook : TestableExecutionHook
    {
        public override Exception OnExecutionError(IHystrixInvokable commandInstance, Exception e)
        {
            base.OnExecutionError(commandInstance, e);
            return new HystrixBadRequestException("autoconverted exception", e);
        }
    }
    class BusinessException : Exception
    {
        public BusinessException(string msg)
            : base(msg)
        {
        }
    }
    class ExceptionToBadRequestByExecutionHookCommand : TestHystrixCommand<bool>
    {
        public ExceptionToBadRequestByExecutionHookCommand(TestCircuitBreaker circuitBreaker, ExecutionIsolationStrategy isolationType)
            : base(TestPropsBuilder()
                .SetCircuitBreaker(circuitBreaker)
                .SetMetrics(circuitBreaker.metrics)
                .SetExecutionHook(new ExceptionToBadRequestByExecutionHookCommandExecutionHook())
                .SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), isolationType)))
        {

        }
        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, ExecutionIsolationStrategy isolationType)
        {
            hystrixCommandOptions.ExecutionIsolationStrategy = isolationType;
            return hystrixCommandOptions;
        }

        protected override bool Run()
        {
            throw new BusinessException("invalid input by the user");
        }


        protected override string CacheKey
        {
            get { return "nein"; }
        }
    }
    class TestChainedCommandSubCommand : TestHystrixCommand<int>
    {

        public TestChainedCommandSubCommand(TestCircuitBreaker circuitBreaker)
        : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.metrics))
        {
        }


        protected override int Run()
        {
            return 2;
        }
    }
    class TestChainedCommandPrimaryCommand : TestHystrixCommand<int>
    {
        public TestChainedCommandPrimaryCommand(TestCircuitBreaker circuitBreaker)
            : base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.metrics))
        {
        }


        protected override int Run()
        {
            throw new Exception("primary failure");
        }


        protected override int RunFallback()
        {
            TestChainedCommandSubCommand subCmd = new TestChainedCommandSubCommand(new TestCircuitBreaker());
            return subCmd.Execute();
        }
    }
    class TestSlowFallbackPrimaryCommand : TestHystrixCommand<int>
    {
        public TestSlowFallbackPrimaryCommand(TestCircuitBreaker circuitBreaker)
    :
        base(TestPropsBuilder().SetCircuitBreaker(circuitBreaker).SetMetrics(circuitBreaker.metrics))
        {
        }


        protected override int Run()
        {
            throw new Exception("primary failure");
        }

        protected override int RunFallback()
        {
            try
            {
                Time.WaitUntil(() => { return _token.IsCancellationRequested; }, 1500);
                _token.ThrowIfCancellationRequested();

                return 1;
            }
            catch (Exception )
            {
                //output.WriteLine("Caught Interrupted Exception");
                //ie.printStackTrace();
            }
            return -1;
        }
    }
    class TestOnRunStartHookThrowsSemaphoreIsolatedFailureInjectionHook : HystrixCommandExecutionHook
    {
        AtomicBoolean onThreadStartInvoked;
        AtomicBoolean onThreadCompleteInvoked;
        public TestOnRunStartHookThrowsSemaphoreIsolatedFailureInjectionHook(AtomicBoolean onThreadStartInvoked, AtomicBoolean onThreadCompleteInvoked)
        {
            this.onThreadStartInvoked = onThreadStartInvoked;
            this.onThreadCompleteInvoked = onThreadCompleteInvoked;
        }
        public override void OnExecutionStart(IHystrixInvokable commandInstance)
        {
            throw new HystrixRuntimeException(FailureType.COMMAND_EXCEPTION, commandInstance.GetType(), "Injected Failure", null, null);
        }

        public override void OnThreadStart(IHystrixInvokable commandInstance)
        {
            onThreadStartInvoked.Value = true;
            base.OnThreadStart(commandInstance);
        }

        public override void OnThreadComplete(IHystrixInvokable commandInstance)
        {
            onThreadCompleteInvoked.Value = true;
            base.OnThreadComplete(commandInstance);
        }
    }
    class TestOnRunStartHookThrowsSemaphoreIsolatedFailureInjectedCommand : TestHystrixCommand<int>
    {
        AtomicBoolean executionAttempted;
        public TestOnRunStartHookThrowsSemaphoreIsolatedFailureInjectedCommand(ExecutionIsolationStrategy isolationStrategy, AtomicBoolean executionAttempted, HystrixCommandExecutionHook failureInjectionHook)
            : base(TestPropsBuilder().SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), isolationStrategy)), failureInjectionHook)
        {
            this.executionAttempted = executionAttempted;
        }

        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, ExecutionIsolationStrategy isolationType)
        {
            hystrixCommandOptions.ExecutionIsolationStrategy = isolationType;
            return hystrixCommandOptions;
        }

        protected override int Run()
        {
            executionAttempted.Value = true;
            return 3;
        }
    }
    class TestOnRunStartHookThrowsThreadIsolatedFailureInjectionHook : HystrixCommandExecutionHook
    {
        AtomicBoolean onThreadStartInvoked;
        AtomicBoolean onThreadCompleteInvoked;
        public TestOnRunStartHookThrowsThreadIsolatedFailureInjectionHook(AtomicBoolean onThreadStartInvoked, AtomicBoolean onThreadCompleteInvoked)
        {
            this.onThreadStartInvoked = onThreadStartInvoked;
            this.onThreadCompleteInvoked = onThreadCompleteInvoked;
        }
        public override void OnExecutionStart(IHystrixInvokable commandInstance)
        {
            throw new HystrixRuntimeException(FailureType.COMMAND_EXCEPTION, commandInstance.GetType(), "Injected Failure", null, null);
        }


        public override void OnThreadStart(IHystrixInvokable commandInstance)
        {
            onThreadStartInvoked.Value = true;
            base.OnThreadStart(commandInstance);
        }


        public override void OnThreadComplete(IHystrixInvokable commandInstance)
        {
            onThreadCompleteInvoked.Value = true;
            base.OnThreadComplete(commandInstance);
        }
    }
    class TestOnRunStartHookThrowsThreadIsolatedFailureInjectedCommand : TestHystrixCommand<int>
    {
        AtomicBoolean executionAttempted;
        public TestOnRunStartHookThrowsThreadIsolatedFailureInjectedCommand(ExecutionIsolationStrategy isolationStrategy, AtomicBoolean executionAttempted, HystrixCommandExecutionHook failureInjectionHook)
        : base(TestPropsBuilder().SetCommandOptionDefaults(GetTestOptions(HystrixCommandOptionsTest.GetUnitTestOptions(), isolationStrategy)), failureInjectionHook)
        {
            this.executionAttempted = executionAttempted;
        }

        private static HystrixCommandOptions GetTestOptions(HystrixCommandOptions hystrixCommandOptions, ExecutionIsolationStrategy isolationType)
        {
            hystrixCommandOptions.ExecutionIsolationStrategy = isolationType;
            return hystrixCommandOptions;
        }

        protected override int Run()
        {
            executionAttempted.Value = true;
            return 3;
        }
    }
    class TestEarlyUnsubscribeDuringExecutionViaToObservableAsyncCommand : HystrixCommand<bool>
    {

        public TestEarlyUnsubscribeDuringExecutionViaToObservableAsyncCommand() :

        base(new HystrixCommandOptions() { GroupKey = HystrixCommandGroupKeyDefault.AsKey("ASYNC") })
        {
        }


        protected override bool Run()
        {
            try
            {
                Time.WaitUntil(() => { return this._token.IsCancellationRequested; }, 500);
                _token.ThrowIfCancellationRequested();
                return true;
            }
            catch (Exception )
            {
                throw;
            }
        }
    }
    class TestEarlyUnsubscribeDuringExecutionViaObserveAsyncCommand : HystrixCommand<bool>
    {

        public TestEarlyUnsubscribeDuringExecutionViaObserveAsyncCommand()

        : base(new HystrixCommandOptions() { GroupKey = HystrixCommandGroupKeyDefault.AsKey("ASYNC") })
        {
        }

        protected override bool Run()
        {
            try
            {
                Time.WaitUntil(() => { return _token.IsCancellationRequested; }, 500);
                _token.ThrowIfCancellationRequested();
                return true;
            }
            catch (Exception )
            {
                throw;
            }
        }
    }
    class TestEarlyUnsubscribeDuringFallbackAsyncCommand : HystrixCommand<Boolean>
    {

        public TestEarlyUnsubscribeDuringFallbackAsyncCommand()
        : base(new HystrixCommandOptions() { GroupKey = HystrixCommandGroupKeyDefault.AsKey("ASYNC") })
        {
        }


        protected override bool Run()
        {
            throw new Exception("run failure");
        }

        protected override bool RunFallback()
        {
            try
            {
                Time.WaitUntil(() => { return _token.IsCancellationRequested; }, 500);
                _token.ThrowIfCancellationRequested();
                return false;
            }
            catch (Exception )
            {
                throw;
            }
        }
    }
    class AsyncCacheableCommand : HystrixCommand<object>
    {
        private string arg;
        private AtomicBoolean cancelled = new AtomicBoolean(false);

        public AsyncCacheableCommand(string arg)
         : base(new HystrixCommandOptions() { GroupKey = HystrixCommandGroupKeyDefault.AsKey("ASYNC") })
        {
            this.arg = arg;
        }


        protected override object Run()
        {
            try
            {
                Time.WaitUntil(() => { return _token.IsCancellationRequested; }, 500);
                _token.ThrowIfCancellationRequested();
                return true;
            }
            catch (Exception )
            {
                cancelled.Value = true;
                throw;
            }
        }

        protected override string CacheKey
        {
            get { return arg; }
        }

        public bool IsCancelled
        {
            get { return cancelled.Value; }
        }
    }
}

