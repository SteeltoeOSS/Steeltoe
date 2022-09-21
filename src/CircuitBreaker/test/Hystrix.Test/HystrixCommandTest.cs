// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public class HystrixCommandTest : CommonHystrixCommandTests<TestHystrixCommand<int>>
{
    private readonly ITestOutputHelper _output;

    private int _uniqueNameCounter;

    public HystrixCommandTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestExecutionSuccess()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success);
        Assert.Equal(FlexibleTestHystrixCommand.ExecuteValue, command.Execute());

        Assert.Null(command.FailedExecutionException);
        Assert.Null(command.ExecutionException);
        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsSuccessfulExecution);

        AssertCommandExecutionEvents(command, HystrixEventType.Success);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestExecutionMultipleTimes()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success);
        Assert.False(command.IsExecutionComplete);

        // first should succeed
        Assert.Equal(FlexibleTestHystrixCommand.ExecuteValue, command.Execute());
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
            _output.WriteLine(e.ToString());

            // we want to get here
        }

        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
        AssertCommandExecutionEvents(command, HystrixEventType.Success);
    }

    [Fact]
    public void TestExecutionHystrixFailureWithNoFallback()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.HystrixFailure, FallbackResultTest.Unimplemented);

        try
        {
            command.Execute();
            Assert.True(false, "we shouldn't get here");
        }
        catch (HystrixRuntimeException e)
        {
            _output.WriteLine(e.ToString());
            Assert.NotNull(e.FallbackException);
            Assert.NotNull(e.ImplementingType);
        }

        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsFailedExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Failure, HystrixEventType.FallbackMissing);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestExecutionFailureWithNoFallback()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Failure, FallbackResultTest.Unimplemented);

        try
        {
            command.Execute();
            Assert.True(false, "we shouldn't get here");
        }
        catch (HystrixRuntimeException e)
        {
            _output.WriteLine(e.ToString());
            Assert.NotNull(e.FallbackException);
            Assert.NotNull(e.ImplementingType);
        }

        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsFailedExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Failure, HystrixEventType.FallbackMissing);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestExecutionFailureWithFallback()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Failure, FallbackResultTest.Success);
        Assert.Equal(FlexibleTestHystrixCommand.FallbackValue, command.Execute());
        Assert.Equal("Execution Failure for TestHystrixCommand", command.FailedExecutionException.Message);
        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsFailedExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Failure, HystrixEventType.FallbackSuccess);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestExecutionRejectionWithFallbackException()
    {
        var threads = new List<Thread>();
        int threadRunCount = 0;
        int exceptionCount = 0;

        for (int i = 0; i < 15; i++)
        {
            var thread = new Thread(() =>
            {
                // Run() delays 5 seconds, Fallback() throws exception
                // Command timeout set to 3 * 5000, so doesn't timeout
                var cmd = new BasicDelayCommand(5000, true);

                try
                {
                    Assert.Equal(5000, cmd.Execute());
                }
                catch (Exception e)
                {
                    Assert.IsType<HystrixRuntimeException>(e);
                    Assert.IsType<RejectedExecutionException>(e.InnerException);
                    Interlocked.Increment(ref exceptionCount);
                }

                Interlocked.Increment(ref threadRunCount);
            });

            thread.Start();
            threads.Add(thread);
        }

        // Wait for all threads to finish, all commands completed
        foreach (Thread thread in threads)
        {
            thread.Join();
        }

        Assert.Equal(15, threadRunCount);
        Assert.Equal(5, exceptionCount);

        // Run() delays 1 seconds, Fallback() throws exception
        // Command timeout set to 3 * 1000, so doesn't timeout
        // This command should succeed as all commands have finished
        var c = new BasicDelayCommand(1000, true);
        Assert.Equal(1000, c.Execute());
    }

    [Fact]
    public void TestExecutionFailureWithFallbackFailure()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Failure, FallbackResultTest.Failure);

        try
        {
            command.Execute();
            Assert.True(false, "we shouldn't get here");
        }
        catch (HystrixRuntimeException e)
        {
            _output.WriteLine("------------------------------------------------");
            _output.WriteLine(e.ToString());
            _output.WriteLine("------------------------------------------------");
            Assert.NotNull(e.FallbackException);
        }

        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsFailedExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Failure, HystrixEventType.FallbackFailure);
        Assert.NotNull(command.ExecutionException);

        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestQueueSuccess()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success);
        Task<int> future = command.ExecuteAsync();
        Assert.Equal(FlexibleTestHystrixCommand.ExecuteValue, future.Result);
        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsSuccessfulExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Success);
        Assert.Null(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public async Task TestQueueKnownFailureWithNoFallback()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.HystrixFailure, FallbackResultTest.Unimplemented);

        try
        {
            await command.ExecuteAsync();
            Assert.True(false, "we shouldn't get here");
        }
        catch (HystrixRuntimeException e)
        {
            _output.WriteLine(e.ToString());
            Assert.NotNull(e.FallbackException);
            Assert.NotNull(e.ImplementingType);
        }

        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsFailedExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Failure, HystrixEventType.FallbackMissing);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public async Task TestQueueUnknownFailureWithNoFallback()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Failure, FallbackResultTest.Unimplemented);

        try
        {
            await command.ExecuteAsync();
            Assert.True(false, "we shouldn't get here");
        }
        catch (HystrixRuntimeException e)
        {
            _output.WriteLine(e.ToString());
            Assert.NotNull(e.FallbackException);
            Assert.NotNull(e.ImplementingType);
        }

        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsFailedExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Failure, HystrixEventType.FallbackMissing);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestQueueFailureWithFallback()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Failure, FallbackResultTest.Success);

        try
        {
            Task<int> future = command.ExecuteAsync();
            Assert.Equal(FlexibleTestHystrixCommand.FallbackValue, future.Result);
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());
            Assert.False(true, "We should have received a response from the fallback.");
        }

        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsFailedExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Failure, HystrixEventType.FallbackSuccess);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public async Task TestQueueFailureWithFallbackFailure()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Failure, FallbackResultTest.Failure);

        try
        {
            await command.ExecuteAsync();
            Assert.True(true, "we shouldn't get here");
        }
        catch (HystrixRuntimeException e)
        {
            _output.WriteLine(e.ToString());
            Assert.NotNull(e.FallbackException);
        }

        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsFailedExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Failure, HystrixEventType.FallbackFailure);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public async Task TestObserveSuccess()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success);
        Assert.Equal(FlexibleTestHystrixCommand.ExecuteValue, await command.Observe().SingleAsync());
        Assert.Null(command.FailedExecutionException);
        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsSuccessfulExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Success);
        Assert.Null(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestCallbackThreadForThreadIsolation()
    {
        var commandThread = new AtomicReference<Thread>();
        var subscribeThread = new AtomicReference<Thread>();

        TestHystrixCommand<bool> command =
            new TestCallbackThreadForThreadIsolationTestHystrixCommand(commandThread, TestHystrixCommand<bool>.TestPropsBuilder());

        var latch = new CountdownEvent(1);

        command.ToObservable().Subscribe(_ =>
        {
            subscribeThread.Value = Thread.CurrentThread;
        }, e =>
        {
            latch.SignalEx();
            _output.WriteLine(e.ToString());
        }, () =>
        {
            latch.SignalEx();
        });

        if (!latch.Wait(2000))
        {
            Assert.False(true, "timed out");
        }

        Assert.NotNull(commandThread.Value);
        Assert.NotNull(subscribeThread.Value);

        _output.WriteLine("Command Thread: " + commandThread.Value);
        _output.WriteLine("Subscribe Thread: " + subscribeThread.Value);

        // Threads are thread-pool threads and will not have hystrix- names
        // Assert.True(commandThread.Value.Name.StartsWith("hystrix-"));
        // Assert.True(subscribeThread.Value.Name.StartsWith("hystrix-"));

        // Steeltoe Added this check
        Assert.NotEqual(commandThread.Value.ManagedThreadId, subscribeThread.Value.ManagedThreadId);
    }

    [Fact]
    public void TestCallbackThreadForSemaphoreIsolation()
    {
        var commandThread = new AtomicReference<Thread>();
        var subscribeThread = new AtomicReference<Thread>();

        TestCommandBuilder builder = TestHystrixCommand<bool>.TestPropsBuilder();
        HystrixCommandOptions opts = HystrixCommandOptionsTest.GetUnitTestOptions();
        opts.ExecutionIsolationStrategy = ExecutionIsolationStrategy.Semaphore;
        builder.SetCommandOptionDefaults(opts);
        TestHystrixCommand<bool> command = new TestCallbackThreadForSemaphoreIsolationTestHystrixCommand(commandThread, builder);

        var latch = new CountdownEvent(1);

        command.ToObservable().Subscribe(_ =>
        {
            subscribeThread.Value = Thread.CurrentThread;
        }, e =>
        {
            latch.SignalEx();
            _output.WriteLine(e.ToString());
        }, () =>
        {
            latch.SignalEx();
        });

        if (!latch.Wait(2000))
        {
            Assert.False(true, "timed out");
        }

        Assert.NotNull(commandThread.Value);
        Assert.NotNull(subscribeThread.Value);
        _output.WriteLine("Command Thread: " + commandThread.Value);
        _output.WriteLine("Subscribe Thread: " + subscribeThread.Value);

        int mainThreadId = Thread.CurrentThread.ManagedThreadId;

        // semaphore should be on the calling thread
        Assert.True(commandThread.Value.ManagedThreadId.Equals(mainThreadId));
        Assert.True(subscribeThread.Value.ManagedThreadId.Equals(mainThreadId));
    }

    [Fact]
    public void TestCircuitBreakerReportsOpenIfForcedOpen()
    {
        var opts = new HystrixCommandOptions
        {
            GroupKey = HystrixCommandGroupKeyDefault.AsKey("GROUP"),
            CircuitBreakerForceOpen = true
        };

        var cmd = new HystrixCommand<bool>(opts, () => true, () => false);

        Assert.False(cmd.Execute()); // fallback should fire
        _output.WriteLine("RESULT : " + cmd.ExecutionEvents);
        Assert.True(cmd.IsCircuitBreakerOpen, "CircuitBreaker unexpectedly closed");
    }

    [Fact]
    public void TestCircuitBreakerReportsClosedIfForcedClosed()
    {
        var opts = new HystrixCommandOptions
        {
            GroupKey = HystrixCommandGroupKeyDefault.AsKey("GROUP"),
            CircuitBreakerForceOpen = false,
            CircuitBreakerForceClosed = true
        };

        var cmd = new HystrixCommand<bool>(opts, () => true, () => false);

        Assert.True(cmd.Execute()); // fallback should fire
        _output.WriteLine("RESULT : " + cmd.ExecutionEvents);
        Assert.False(cmd.IsCircuitBreakerOpen, "CircuitBreaker unexpectedly open");
    }

    [Fact]
    public async Task TestCircuitBreakerAcrossMultipleCommandsButSameCircuitBreaker()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("SharedCircuitBreaker");
        var circuitBreaker = new TestCircuitBreaker(key);

        /* fail 3 times and then it should trip the circuit and stop executing */

        // failure 1
        TestHystrixCommand<int> attempt1 = GetSharedCircuitBreakerCommand(key, ExecutionIsolationStrategy.Thread, FallbackResultTest.Success, circuitBreaker);
        _output.WriteLine("COMMAND KEY (from cmd): " + attempt1.CommandKey.Name);
        await attempt1.ExecuteAsync();

        // Time.Wait(100);
        Assert.True(WaitForHealthCountToUpdate(key.Name, 250, _output), "Health count update took to long");

        Assert.True(attempt1.IsFailedExecution, "Unexpected execution success (1)");
        Assert.True(attempt1.IsResponseFromFallback, "Response not from fallback as was expected (1)");
        Assert.False(attempt1.IsCircuitBreakerOpen, "CircuitBreaker unexpectedly open (1)");
        Assert.False(attempt1.IsResponseShortCircuited, "CircuitBreaker unexpectedly short circuited (1)");

        // failure 2 with a different command, same circuit breaker
        TestHystrixCommand<int> attempt2 = GetSharedCircuitBreakerCommand(key, ExecutionIsolationStrategy.Thread, FallbackResultTest.Success, circuitBreaker);
        await attempt2.ExecuteAsync();

        // Time.Wait(100);
        Assert.True(WaitForHealthCountToUpdate(key.Name, 250, _output), "Health count update took to long");

        Assert.True(attempt2.IsFailedExecution, "Unexpected execution success (2)");
        Assert.True(attempt2.IsResponseFromFallback, "Response not from fallback as was expected (2)");
        Assert.False(attempt2.IsCircuitBreakerOpen, "CircuitBreaker unexpectedly open (2)");
        Assert.False(attempt2.IsResponseShortCircuited, "CircuitBreaker unexpectedly short circuited (2)");

        // failure 3 of the Hystrix, 2nd for this particular HystrixCommand
        TestHystrixCommand<int> attempt3 = GetSharedCircuitBreakerCommand(key, ExecutionIsolationStrategy.Thread, FallbackResultTest.Success, circuitBreaker);
        await attempt3.ExecuteAsync();

        // Time.Wait(150);
        Assert.True(WaitForHealthCountToUpdate(key.Name, 250, _output), "Health count update took to long");

        Assert.True(attempt3.IsFailedExecution, "Unexpected execution success (3)");
        Assert.True(attempt3.IsResponseFromFallback, "Response not from fallback as was expected (3)");
        Assert.False(attempt3.IsResponseShortCircuited, "CircuitBreaker unexpectedly short circuited (3)");

        // Time.Wait(150);
        Assert.True(WaitForHealthCountToUpdate(key.Name, 250, _output), "Health count update took to long");

        // it should now be 'open' and prevent further executions
        // after having 3 failures on the Hystrix that these 2 different HystrixCommand objects are for
        Assert.True(attempt3.IsCircuitBreakerOpen, "CircuitBreaker unexpectedly closed (3)");

        // attempt 4
        TestHystrixCommand<int> attempt4 = GetSharedCircuitBreakerCommand(key, ExecutionIsolationStrategy.Thread, FallbackResultTest.Success, circuitBreaker);
        await attempt4.ExecuteAsync();

        // Time.Wait(100);
        Assert.True(WaitForHealthCountToUpdate(key.Name, 250, _output), "Health count update took to long");

        Assert.True(attempt4.IsResponseFromFallback, "Response not from fallback as was expected (4)");

        // this should now be true as the response will be short-circuited
        Assert.True(attempt4.IsResponseShortCircuited, "CircuitBreaker not short circuited as expected (4)");

        // this should remain open
        Assert.True(attempt4.IsCircuitBreakerOpen, "CircuitBreaker unexpectedly closed (4)");

        AssertSaneHystrixRequestLog(4);
        AssertCommandExecutionEvents(attempt1, HystrixEventType.Failure, HystrixEventType.FallbackSuccess);
        AssertCommandExecutionEvents(attempt2, HystrixEventType.Failure, HystrixEventType.FallbackSuccess);
        AssertCommandExecutionEvents(attempt3, HystrixEventType.Failure, HystrixEventType.FallbackSuccess);
        AssertCommandExecutionEvents(attempt4, HystrixEventType.ShortCircuited, HystrixEventType.FallbackSuccess);
    }

    [Fact]
    public void TestExecutionSuccessWithCircuitBreakerDisabled()
    {
        TestHystrixCommand<int> command = GetCircuitBreakerDisabledCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success);
        Assert.Equal(FlexibleTestHystrixCommand.ExecuteValue, command.Execute());

        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);

        // we'll still get metrics ... just not the circuit breaker opening/closing
        AssertCommandExecutionEvents(command, HystrixEventType.Success);
    }

    [Fact]
    public void TestExecutionTimeoutWithNoFallback()
    {
        TestHystrixCommand<int> command =
            GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 200, FallbackResultTest.Unimplemented, 50);

        try
        {
            command.Execute();
            Assert.True(false, "we shouldn't get here");
        }
        catch (Exception e)
        {
            // e.printStackTrace();
            if (e is HystrixRuntimeException de)
            {
                Assert.NotNull(de.FallbackException);
                Assert.True(de.FallbackException is InvalidOperationException);
                Assert.NotNull(de.ImplementingType);
                Assert.NotNull(de.InnerException);
                Assert.True(de.InnerException is TimeoutException);
            }
            else
            {
                Assert.False(true, "the exception should be HystrixRuntimeException");
            }
        }

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

        // the time should be 50+ since we timeout at 50ms
        Assert.True(command.ExecutionTimeInMilliseconds >= 50);

        Assert.True(command.IsResponseTimedOut);
        Assert.False(command.IsResponseFromFallback);
        Assert.False(command.IsResponseRejected);
        AssertCommandExecutionEvents(command, HystrixEventType.Timeout, HystrixEventType.FallbackMissing);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestExecutionTimeoutWithFallback()
    {
        TestHystrixCommand<int> command = GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 200, FallbackResultTest.Success, 50);
        Assert.Equal(FlexibleTestHystrixCommand.FallbackValue, command.Execute());

        // the time should be 50+ since we timeout at 50ms
        Assert.True(command.ExecutionTimeInMilliseconds >= 50);
        Assert.False(command.IsCircuitBreakerOpen, "CircuitBreaker unexpectedly open");
        Assert.False(command.IsResponseShortCircuited);
        Assert.True(command.IsResponseTimedOut);
        Assert.True(command.IsResponseFromFallback);
        AssertCommandExecutionEvents(command, HystrixEventType.Timeout, HystrixEventType.FallbackSuccess);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestExecutionTimeoutFallbackFailure()
    {
        TestHystrixCommand<int> command = GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 200, FallbackResultTest.Failure, 50);

        try
        {
            command.Execute();
            Assert.True(false, "we shouldn't get here");
        }
        catch (Exception e)
        {
            if (e is HystrixRuntimeException de)
            {
                Assert.NotNull(de.FallbackException);
                Assert.False(de.FallbackException is InvalidOperationException);
                Assert.NotNull(de.ImplementingType);
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
        AssertCommandExecutionEvents(command, HystrixEventType.Timeout, HystrixEventType.FallbackFailure);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestCountersOnExecutionTimeout()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 200, FallbackResultTest.Success, 50);
        command.Execute();

        /* wait long enough for the command to have finished */
        Time.Wait(200);

        /* response should still be the same as 'testCircuitBreakerOnExecutionTimeout' */
        Assert.True(command.IsResponseFromFallback);
        Assert.False(command.IsCircuitBreakerOpen, "CircuitBreaker unexpectedly open");
        Assert.False(command.IsResponseShortCircuited);

        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsResponseTimedOut);
        Assert.False(command.IsSuccessfulExecution);
        Assert.NotNull(command.ExecutionException);

        AssertCommandExecutionEvents(command, HystrixEventType.Timeout, HystrixEventType.FallbackSuccess);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public async Task TestQueuedExecutionTimeoutWithNoFallback()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 200, FallbackResultTest.Unimplemented, 50);

        try
        {
            await command.ExecuteAsync();
            Assert.True(false, "we shouldn't get here");
        }
        catch (HystrixRuntimeException e)
        {
            // e.printStackTrace();
            Assert.NotNull(e.FallbackException);
            Assert.True(e.FallbackException is InvalidOperationException);
            Assert.NotNull(e.ImplementingType);
            Assert.NotNull(e.InnerException);
            Assert.True(e.InnerException is TimeoutException);
        }

        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsResponseTimedOut);
        AssertCommandExecutionEvents(command, HystrixEventType.Timeout, HystrixEventType.FallbackMissing);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public async Task TestQueuedExecutionTimeoutWithFallback()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 200, FallbackResultTest.Success, 50);
        Assert.Equal(FlexibleTestHystrixCommand.FallbackValue, await command.ExecuteAsync());
        AssertCommandExecutionEvents(command, HystrixEventType.Timeout, HystrixEventType.FallbackSuccess);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public async Task TestQueuedExecutionTimeoutFallbackFailure()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 200, FallbackResultTest.Failure, 50);

        try
        {
            _ = await command.ExecuteAsync();
            Assert.True(false, "Looks like the 'FailureCommand' didn't fail");
        }
        catch (HystrixRuntimeException e)
        {
            Assert.NotNull(e.FallbackException);
            Assert.False(e.FallbackException is InvalidOperationException, "Fallback exception was unexpected type");
            Assert.NotNull(e.ImplementingType);
            Assert.NotNull(e.InnerException);
            Assert.True(e.InnerException is TimeoutException, "Inner exception was unexpected type");
        }

        AssertCommandExecutionEvents(command, HystrixEventType.Timeout, HystrixEventType.FallbackFailure);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestObservedExecutionTimeoutWithNoFallback()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 200, FallbackResultTest.Unimplemented, 50);

        try
        {
            command.Observe().SingleAsync().Wait();
            Assert.False(true, "we shouldn't get here");
        }
        catch (Exception e)
        {
            if (e is HystrixRuntimeException de)
            {
                Assert.NotNull(de.FallbackException);
                Assert.True(de.FallbackException is InvalidOperationException);
                Assert.NotNull(de.ImplementingType);
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
        AssertCommandExecutionEvents(command, HystrixEventType.Timeout, HystrixEventType.FallbackMissing);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestObservedExecutionTimeoutWithFallback()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 200, FallbackResultTest.Success, 50);
        Assert.Equal(FlexibleTestHystrixCommand.FallbackValue, command.Observe().SingleAsync().Wait());

        AssertCommandExecutionEvents(command, HystrixEventType.Timeout, HystrixEventType.FallbackSuccess);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestObservedExecutionTimeoutFallbackFailure()
    {
        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 200, FallbackResultTest.Failure, 50);

        try
        {
            command.Observe().SingleAsync().Wait();
            Assert.False(true, "we shouldn't get here");
        }
        catch (Exception e)
        {
            if (e is HystrixRuntimeException de)
            {
                Assert.NotNull(de.FallbackException);
                Assert.False(de.FallbackException is InvalidOperationException);
                Assert.NotNull(de.ImplementingType);
                Assert.NotNull(de.InnerException);
                Assert.True(de.InnerException is TimeoutException);
            }
            else
            {
                Assert.True(false, "the exception should be AggregateException with cause as HystrixRuntimeException");
            }
        }

        AssertCommandExecutionEvents(command, HystrixEventType.Timeout, HystrixEventType.FallbackFailure);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestShortCircuitFallbackCounter()
    {
        TestCircuitBreaker circuitBreaker = new TestCircuitBreaker().SetForceShortCircuit(true);
        var command1 = new KnownFailureTestCommandWithFallback(circuitBreaker);
        command1.Execute();

        var command2 = new KnownFailureTestCommandWithFallback(circuitBreaker);
        command2.Execute();

        // will be -1 because it never attempted execution
        Assert.True(command1.ExecutionTimeInMilliseconds == -1);
        Assert.True(command1.IsResponseShortCircuited);
        Assert.False(command1.IsResponseTimedOut);
        Assert.NotNull(command1.ExecutionException);

        AssertCommandExecutionEvents(command1, HystrixEventType.ShortCircuited, HystrixEventType.FallbackSuccess);
        AssertCommandExecutionEvents(command2, HystrixEventType.ShortCircuited, HystrixEventType.FallbackSuccess);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(2);
    }

    [Fact]
    public async Task TestRejectedThreadWithNoFallback()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Rejection-NoFallback");
        var circuitBreaker = new TestCircuitBreaker();
        var pool = new SingleThreadedPoolWithQueue(1);

        Task<bool> f = null;
        Task<bool> f2 = null;
        TestCommandRejection command1 = null;
        TestCommandRejection command2 = null;
        TestCommandRejection command3 = null;

        try
        {
            command1 = new TestCommandRejection(key, circuitBreaker, pool, 500, 700, TestCommandRejection.FallbackNotImplemented);
            command2 = new TestCommandRejection(key, circuitBreaker, pool, 500, 700, TestCommandRejection.FallbackNotImplemented);
            command3 = new TestCommandRejection(key, circuitBreaker, pool, 500, 700, TestCommandRejection.FallbackNotImplemented);
            f = command1.ExecuteAsync(); // Running
            Time.Wait(50); // Let first start
            f2 = command2.ExecuteAsync(); // In Queue
            await command3.ExecuteAsync(); // Start, queue rejected
            Assert.True(false, "we shouldn't get here");
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());
            _output.WriteLine("command.getExecutionTimeInMilliseconds(): " + command3.ExecutionTimeInMilliseconds);

            // will be -1 because it never attempted execution
            Assert.True(command3.IsResponseRejected);
            Assert.False(command3.IsResponseShortCircuited);
            Assert.False(command3.IsResponseTimedOut);
            Assert.NotNull(command3.ExecutionException);

            if (e is HystrixRuntimeException exception && e.InnerException is RejectedExecutionException)
            {
                HystrixRuntimeException de = exception;
                Assert.NotNull(de.FallbackException);
                Assert.True(de.FallbackException is InvalidOperationException);
                Assert.NotNull(de.ImplementingType);
                Assert.NotNull(de.InnerException);
                Assert.True(de.InnerException is RejectedExecutionException);
            }
            else
            {
                Assert.False(true, "the exception should be HystrixRuntimeException with cause as RejectedExecutionException");
            }
        }

        // Make sure finished
        _ = await f;
        _ = await f2;

        AssertCommandExecutionEvents(command1, HystrixEventType.Success);
        AssertCommandExecutionEvents(command2, HystrixEventType.Success);
        AssertCommandExecutionEvents(command3, HystrixEventType.ThreadPoolRejected, HystrixEventType.FallbackMissing);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(3);
        pool.Dispose();
    }

    [Fact]
    public void TestRejectedThreadWithFallback()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Rejection-Fallback");
        var circuitBreaker = new TestCircuitBreaker();
        var pool = new SingleThreadedPoolWithQueue(1);

        // command 1 will execute in threadpool (passing through the queue)
        // command 2 will execute after spending time in the queue (after command1 completes)
        // command 3 will get rejected, since it finds pool and queue both full
        var command1 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FallbackSuccess);
        var command2 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FallbackSuccess);
        var command3 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FallbackSuccess);

        IObservable<bool> result1 = command1.Observe();
        Time.Wait(50); // Let cmd1 get running
        IObservable<bool> result2 = command2.Observe();

        Time.Wait(100);

        // command3 should find queue filled, and get rejected
        bool result = command3.Execute();
        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

        Assert.False(result, "Command3 returned True instead of False");
        Assert.True(command3.IsResponseRejected, "Command3 rejected when not expected");
        Assert.False(command1.IsResponseRejected, "Command1 not rejected when expected");
        Assert.False(command2.IsResponseRejected, "Command2 not rejected when expected");
        Assert.True(command3.IsResponseFromFallback, "Command3 response not from fallback as was expected");
        Assert.NotNull(command3.ExecutionException);

        AssertCommandExecutionEvents(command3, HystrixEventType.ThreadPoolRejected, HystrixEventType.FallbackSuccess);
        result1.Merge(result2).ToList().SingleAsync().Wait(); // await the 2 latent commands

        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);

        AssertSaneHystrixRequestLog(3);
        pool.Dispose();
    }

    [Fact]
    public async Task TestRejectedThreadWithFallbackFailure()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var pool = new SingleThreadedPoolWithQueue(1);
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Rejection-A");

        var command1 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600,
            TestCommandRejection.FallbackFailure); // this should pass through the queue and sit in the pool

        var command2 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FallbackSuccess); // this should sit in the queue

        var command3 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600,
            TestCommandRejection.FallbackFailure); // this should observe full queue and get rejected

        Task<bool> f1 = null;
        Task<bool> f2 = null;

        try
        {
            f1 = command1.ExecuteAsync();
            Time.Wait(10); // Let first one get in and off queue
            f2 = command2.ExecuteAsync();
            Assert.False(command3.Execute()); // should get thread-pool rejected
            Assert.True(false, "we shouldn't get here");
        }
        catch (Exception e)
        {
            // e.printStackTrace()
            if (e is HystrixRuntimeException exception && e.InnerException is RejectedExecutionException)
            {
                HystrixRuntimeException de = exception;
                Assert.NotNull(de.FallbackException);
                Assert.False(de.FallbackException is InvalidOperationException);
                Assert.NotNull(de.ImplementingType);
                Assert.NotNull(de.InnerException);
                Assert.True(de.InnerException is RejectedExecutionException);
            }
            else
            {
                Assert.False(true, "the exception should be HystrixRuntimeException with cause as RejectedExecutionException");
            }
        }

        AssertCommandExecutionEvents(command1); // still in-flight, no events yet
        AssertCommandExecutionEvents(command2); // still in-flight, no events yet
        AssertCommandExecutionEvents(command3, HystrixEventType.ThreadPoolRejected, HystrixEventType.FallbackFailure);
        int numInFlight = circuitBreaker.Metrics.CurrentConcurrentExecutionCount;
        Assert.True(numInFlight <= 1, "Pool-filler NOT still going"); // pool-filler still going

        // This is a case where we knowingly walk away from executing Hystrix threads. They should have an in-flight status ("Executed").  You should avoid this in a production environment
        IHystrixRequestLog requestLog = HystrixRequestLog.CurrentRequestLog;
        Assert.Equal(3, requestLog.AllExecutedCommands.Count);
        Assert.Contains("Executed", requestLog.GetExecutedCommandsAsString());

        // block on the outstanding work, so we don't inadvertently affect any other tests
        long startTime = DateTime.Now.Ticks / 10000;
        _ = await f1;
        _ = await f2;
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        _output.WriteLine("Time blocked : " + (Time.CurrentTimeMillis - startTime));
        pool.Dispose();
    }

    [Fact]
    public async Task TestRejectedThreadUsingQueueSize()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Rejection-B");
        var circuitBreaker = new TestCircuitBreaker();
        var pool = new SingleThreadedPoolWithQueue(10, 1);

        var d1 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FallbackNotImplemented);
        var d2 = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FallbackNotImplemented);

        // Schedule 2 items, one will be taken off and start running, the second will get queued
        // the thread pool won't pick it up because we're bypassing the pool and adding to the queue directly so this will keep the queue full
        var t = new Task(_ => Time.Wait(500), d1);
        t.Start(pool.GetTaskScheduler());

        Time.Wait(10);

        var t2 = new Task(_ => Time.Wait(500), d2);
        t2.Start(pool.GetTaskScheduler());

        var command = new TestCommandRejection(key, circuitBreaker, pool, 500, 600, TestCommandRejection.FallbackNotImplemented);

        try
        {
            // this should fail as we already have 1 in the queue
            await command.ExecuteAsync();
            Assert.False(true, "we shouldn't get here");
        }
        catch (Exception e)
        {
            // e.printStackTrace()
            _output.WriteLine("command.getExecutionTimeInMilliseconds(): " + command.ExecutionTimeInMilliseconds);

            // will be -1 because it never attempted execution
            Assert.True(command.IsResponseRejected, "Command not rejected as was expected");
            Assert.False(command.IsResponseShortCircuited, "Command not short circuited as was expected");
            Assert.False(command.IsResponseTimedOut, "Command unexpectedly timed out");
            Assert.NotNull(command.ExecutionException);

            if (e is HystrixRuntimeException exception && e.InnerException is RejectedExecutionException)
            {
                HystrixRuntimeException de = exception;
                Assert.NotNull(de.FallbackException);
                Assert.True(de.FallbackException is InvalidOperationException);
                Assert.NotNull(de.ImplementingType);
                Assert.NotNull(de.InnerException);
                Assert.True(de.InnerException is RejectedExecutionException);
            }
            else
            {
                Assert.False(true, "the exception should be HystrixRuntimeException with cause as RejectedExecutionException");
            }
        }

        AssertCommandExecutionEvents(command, HystrixEventType.ThreadPoolRejected, HystrixEventType.FallbackMissing);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
        pool.Dispose();
    }

    [Fact]
    public void TestDisabledTimeoutWorks()
    {
        var cmd = new CommandWithDisabledTimeout(100, 900);
        bool result = cmd.Execute();

        Assert.True(result, "Command result was not True");
        Assert.False(cmd.IsResponseTimedOut, "Command response timed out!");
        Assert.Null(cmd.ExecutionException);
        _output.WriteLine("CMD : " + cmd.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.True(cmd.ExecutionResult.ExecutionLatency >= 900, "Execution latency lower than should have been possible");
        AssertCommandExecutionEvents(cmd, HystrixEventType.Success);
    }

    [Fact]
    public async Task TestFallbackSemaphore()
    {
        var circuitBreaker = new TestCircuitBreaker();

        // single thread should work
        var command1 = new TestSemaphoreCommandWithSlowFallback(circuitBreaker, 1, 200);
        bool result = command1.Execute();
        Assert.True(result);

        // 2 threads, the second should be rejected by the fallback semaphore
        bool exceptionReceived = false;
        Task<bool> result2 = null;
        TestSemaphoreCommandWithSlowFallback command2 = null;
        TestSemaphoreCommandWithSlowFallback command3 = null;

        try
        {
            _output.WriteLine("c2 start: " + Time.CurrentTimeMillis);
            command2 = new TestSemaphoreCommandWithSlowFallback(circuitBreaker, 1, 800);
            result2 = command2.ExecuteAsync();
            _output.WriteLine("c2 after queue: " + Time.CurrentTimeMillis);

            // make sure that thread gets a chance to run before queuing the next one
            Time.Wait(50);
            _output.WriteLine("c3 start: " + Time.CurrentTimeMillis);
            command3 = new TestSemaphoreCommandWithSlowFallback(circuitBreaker, 1, 200);
            Task<bool> result3 = command3.ExecuteAsync();
            _output.WriteLine("c3 after queue: " + Time.CurrentTimeMillis);
            _ = await result3;
        }
        catch (Exception)
        {
            exceptionReceived = true;
        }

        Assert.True(result2.Result, "Result 2 was False when True was expected");

        if (!exceptionReceived)
        {
            Assert.False(true, "We expected an exception on the 2nd get");
        }

        AssertCommandExecutionEvents(command1, HystrixEventType.Failure, HystrixEventType.FallbackSuccess);
        AssertCommandExecutionEvents(command2, HystrixEventType.Failure, HystrixEventType.FallbackSuccess);
        AssertCommandExecutionEvents(command3, HystrixEventType.Failure, HystrixEventType.FallbackRejection);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(3);
    }

    [Fact]
    public async Task TestExecutionSemaphoreWithQueue()
    {
        var circuitBreaker = new TestCircuitBreaker();

        // single thread should work
        var command1 = new TestSemaphoreCommand(circuitBreaker, 1, 200, TestSemaphoreCommand.ResultSuccess, TestSemaphoreCommand.FallbackNotImplemented);
        bool result = await command1.ExecuteAsync();
        Assert.True(result);

        var exceptionReceived = new AtomicBoolean();

        var semaphore = new SemaphoreSlim(1);

        var command2 = new TestSemaphoreCommand(circuitBreaker, semaphore, 200, TestSemaphoreCommand.ResultSuccess,
            TestSemaphoreCommand.FallbackNotImplemented);

        var command2Action = new ThreadStart(async () =>
        {
            try
            {
                _ = await command2.ExecuteAsync();
            }
            catch (Exception)
            {
                exceptionReceived.Value = true;
            }
        });

        var command3 = new TestSemaphoreCommand(circuitBreaker, semaphore, 200, TestSemaphoreCommand.ResultSuccess,
            TestSemaphoreCommand.FallbackNotImplemented);

        var command3Action = new ThreadStart(async () =>
        {
            try
            {
                _ = await command3.ExecuteAsync();
            }
            catch (Exception)
            {
                exceptionReceived.Value = true;
            }
        });

        // 2 threads, the second should be rejected by the semaphore
        var t2 = new Thread(command2Action);
        var t3 = new Thread(command3Action);

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

        AssertCommandExecutionEvents(command1, HystrixEventType.Success);
        AssertCommandExecutionEvents(command2, HystrixEventType.Success);
        AssertCommandExecutionEvents(command3, HystrixEventType.SemaphoreRejected, HystrixEventType.FallbackMissing);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(3);
    }

    [Fact]
    public void TestExecutionSemaphoreWithExecution()
    {
        var circuitBreaker = new TestCircuitBreaker();

        // single thread should work
        var command1 = new TestSemaphoreCommand(circuitBreaker, 1, 200, TestSemaphoreCommand.ResultSuccess, TestSemaphoreCommand.FallbackNotImplemented);
        bool result = command1.Execute();
        Assert.False(command1.IsExecutedInThread, "Command1 not executed in thread as was expected");
        Assert.True(result, "Result was false when True was expected");

        var results = new BlockingCollection<bool>(2);

        var exceptionReceived = new AtomicBoolean();

        var semaphore = new SemaphoreSlim(1);

        var command2 = new TestSemaphoreCommand(circuitBreaker, semaphore, 400, TestSemaphoreCommand.ResultSuccess,
            TestSemaphoreCommand.FallbackNotImplemented);

        bool t2Started = false;

        var command2Action = new ThreadStart(() =>
        {
            t2Started = true;

            try
            {
                results.Add(command2.Execute());
            }
            catch (Exception)
            {
                exceptionReceived.Value = true;
            }
        });

        var command3 = new TestSemaphoreCommand(circuitBreaker, semaphore, 400, TestSemaphoreCommand.ResultSuccess,
            TestSemaphoreCommand.FallbackNotImplemented);

        var command3Action = new ThreadStart(() =>
        {
            try
            {
                results.Add(command3.Execute());
            }
            catch (Exception)
            {
                exceptionReceived.Value = true;
            }
        });

        // 2 threads, the second should be rejected by the semaphore
        var t2 = new Thread(command2Action);
        var t3 = new Thread(command3Action);

        t2.Start();
        Assert.True(Time.WaitUntil(() => t2Started, 500), "t2 took to long to start");

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
        AssertCommandExecutionEvents(command1, HystrixEventType.Success);
        AssertCommandExecutionEvents(command2, HystrixEventType.Success);
        AssertCommandExecutionEvents(command3, HystrixEventType.SemaphoreRejected, HystrixEventType.FallbackMissing);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(3);
    }

    [Fact]
    public void TestRejectedExecutionSemaphoreWithFallbackViaExecute()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var results = new BlockingCollection<bool>(2);

        var exceptionReceived = new AtomicBoolean();

        var command1 = new TestSemaphoreCommandWithFallback(circuitBreaker, 1, 200, false);

        var command1Action = new ThreadStart(() =>
        {
            try
            {
                results.Add(command1.Execute());
            }
            catch (Exception)
            {
                exceptionReceived.Value = true;
            }
        });

        var command2 = new TestSemaphoreCommandWithFallback(circuitBreaker, 1, 200, false);

        var command2Action = new ThreadStart(() =>
        {
            try
            {
                results.Add(command2.Execute());
            }
            catch (Exception)
            {
                exceptionReceived.Value = true;
            }
        });

        // 2 threads, the second should be rejected by the semaphore and return fallback
        var t1 = new Thread(command1Action);
        var t2 = new Thread(command2Action);

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
        AssertCommandExecutionEvents(command1, HystrixEventType.Success);
        AssertCommandExecutionEvents(command2, HystrixEventType.SemaphoreRejected, HystrixEventType.FallbackSuccess);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(2);
    }

    [Fact]
    public void TestRejectedExecutionSemaphoreWithFallbackViaObserve()
    {
        var circuitBreaker = new TestCircuitBreaker();

        var results = new BlockingCollection<IObservable<bool>>(2);

        var exceptionReceived = new AtomicBoolean();

        var command1 = new TestSemaphoreCommandWithFallback(circuitBreaker, 1, 200, false);

        var command1Action = new ThreadStart(() =>
        {
            try
            {
                results.Add(command1.Observe());
            }
            catch (Exception)
            {
                exceptionReceived.Value = true;
            }
        });

        var command2 = new TestSemaphoreCommandWithFallback(circuitBreaker, 1, 200, false);

        var command2Action = new ThreadStart(() =>
        {
            try
            {
                results.Add(command2.Observe());
            }
            catch (Exception)
            {
                exceptionReceived.Value = true;
            }
        });

        // 2 threads, the second should be rejected by the semaphore and return fallback
        var t1 = new Thread(command1Action);
        var t2 = new Thread(command2Action);

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

        IList<bool> blockingList = results.Merge().ToList().SingleAsync().Wait();

        // both threads should have returned values
        Assert.Equal(2, blockingList.Count);

        // should contain both a true and false result
        Assert.True(blockingList.Contains(true));
        Assert.True(blockingList.Contains(false));
        AssertCommandExecutionEvents(command1, HystrixEventType.Success);
        AssertCommandExecutionEvents(command2, HystrixEventType.SemaphoreRejected, HystrixEventType.FallbackSuccess);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(2);
    }

    [Fact]
    public void TestSemaphorePermitsInUse()
    {
        var circuitBreaker = new TestCircuitBreaker();

        // this semaphore will be shared across multiple command instances
        var sharedSemaphore = new SemaphoreSlim(3);

        // used to wait until all commands have started
        var startLatch = new CountdownEvent(sharedSemaphore.CurrentCount * 2 + 1);

        // used to signal that all command can finish
        var sharedLatch = new CountdownEvent(1);

        // tracks failures to obtain semaphores
        var failureCount = new AtomicInteger();

        var sharedSemaphoreRunnable = new ThreadStart(() =>
        {
            try
            {
                new LatchedSemaphoreCommand("Command-Shared", circuitBreaker, sharedSemaphore, startLatch, sharedLatch).Execute();
            }
            catch (Exception)
            {
                startLatch.SignalEx();

                // e.printStackTrace();
                failureCount.IncrementAndGet();
            }
        });

        // creates group of threads each using command sharing a single semaphore
        // I create extra threads and commands so that I can verify that some of them fail to obtain a semaphore
        int sharedThreadCount = sharedSemaphore.CurrentCount * 2;
        var sharedSemaphoreThreads = new Thread[sharedThreadCount];

        for (int i = 0; i < sharedThreadCount; i++)
        {
            sharedSemaphoreThreads[i] = new Thread(sharedSemaphoreRunnable);
        }

        // creates thread using isolated semaphore
        var isolatedSemaphore = new SemaphoreSlim(1);

        var isolatedLatch = new CountdownEvent(1);

        var isolatedThread = new Thread(() =>
        {
            try
            {
                new LatchedSemaphoreCommand("Command-Isolated", circuitBreaker, isolatedSemaphore, startLatch, isolatedLatch).Execute();
            }
            catch (Exception)
            {
                startLatch.SignalEx();

                // e.printStackTrace();
                failureCount.IncrementAndGet();
            }
        });

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
        _output.WriteLine("REQLOG : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

        Assert.Equal(3, sharedSemaphore.CurrentCount);
        Assert.Equal(1, isolatedSemaphore.CurrentCount);

        // verifies that some executions failed
        // Assert.Equal(sharedSemaphore.numberOfPermits.get().longValue(), failureCount.get());
        IHystrixRequestLog requestLog = HystrixRequestLog.CurrentRequestLog;
        Assert.Contains("SEMAPHORE_REJECTED", requestLog.GetExecutedCommandsAsString());
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
    }

    [Fact]
    public void TestDynamicOwner()
    {
        TestHystrixCommand<bool> command = new DynamicOwnerTestCommand(CommandGroupForUnitTest.OwnerOne);
        Assert.True(command.Execute());
        AssertCommandExecutionEvents(command, HystrixEventType.Success);
    }

    [Fact]
    public void TestDynamicOwnerFails()
    {
        Assert.Throws<ArgumentNullException>(() => new DynamicOwnerTestCommand(null));
    }

    [Fact]
    public void TestDynamicKey()
    {
        var command1 = new DynamicOwnerAndKeyTestCommand(CommandGroupForUnitTest.OwnerOne, CommandKeyForUnitTest.KeyOne);
        Assert.True(command1.Execute());
        var command2 = new DynamicOwnerAndKeyTestCommand(CommandGroupForUnitTest.OwnerOne, CommandKeyForUnitTest.KeyTwo);
        Assert.True(command2.Execute());

        // 2 different circuit breakers should be created
        Assert.True(command1.CircuitBreaker != command2.CircuitBreaker);
    }

    [Fact]
    public void TestRequestCache1()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var command1 = new SuccessfulCacheableCommand<string>(circuitBreaker, true, "A");
        var command2 = new SuccessfulCacheableCommand<string>(circuitBreaker, true, "A");

        Assert.True(command1.IsCommandRunningInThread);

        Task<string> f1 = command1.ExecuteAsync();
        Task<string> f2 = command2.ExecuteAsync();

        Assert.Equal("A", f1.Result);
        Assert.Equal("A", f2.Result);

        Assert.True(command1.Executed);

        // the second one should not have executed as it should have received the cached value instead
        Assert.False(command2.Executed);
        Assert.True(command1.ExecutionTimeInMilliseconds > -1);
        Assert.False(command1.IsResponseFromCache);
        Assert.True(command2.IsResponseFromCache);
        AssertCommandExecutionEvents(command1, HystrixEventType.Success);
        AssertCommandExecutionEvents(command2, HystrixEventType.Success, HystrixEventType.ResponseFromCache);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(2);
    }

    [Fact]
    public void TestRequestCache2()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var command1 = new SuccessfulCacheableCommand<string>(circuitBreaker, true, "A");
        var command2 = new SuccessfulCacheableCommand<string>(circuitBreaker, true, "B");

        Assert.True(command1.IsCommandRunningInThread);

        Task<string> f1 = command1.ExecuteAsync();
        Task<string> f2 = command2.ExecuteAsync();

        Assert.Equal("A", f1.Result);
        Assert.Equal("B", f2.Result);

        Assert.True(command1.Executed);

        // both should execute as they are different
        Assert.True(command2.Executed);
        Assert.True(command2.ExecutionTimeInMilliseconds > -1);
        Assert.False(command2.IsResponseFromCache);
        AssertCommandExecutionEvents(command1, HystrixEventType.Success);
        AssertCommandExecutionEvents(command2, HystrixEventType.Success);
        Assert.Null(command1.ExecutionException);
        Assert.False(command2.IsResponseFromCache);
        Assert.Null(command2.ExecutionException);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(2);
    }

    [Fact]
    public void TestRequestCache3()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var command1 = new SuccessfulCacheableCommand<string>(circuitBreaker, true, "A");
        var command2 = new SuccessfulCacheableCommand<string>(circuitBreaker, true, "B");
        var command3 = new SuccessfulCacheableCommand<string>(circuitBreaker, true, "A");

        Assert.True(command1.IsCommandRunningInThread);

        Task<string> f1 = command1.ExecuteAsync();
        Task<string> f2 = command2.ExecuteAsync();
        Task<string> f3 = command3.ExecuteAsync();
        Assert.Equal("A", f1.Result);
        Assert.Equal("B", f2.Result);
        Assert.Equal("A", f3.Result);

        Assert.True(command1.Executed);

        // both should execute as they are different
        Assert.True(command2.Executed);

        // but the 3rd should come from cache
        Assert.False(command3.Executed);
        Assert.True(command3.IsResponseFromCache);
        AssertCommandExecutionEvents(command1, HystrixEventType.Success);
        AssertCommandExecutionEvents(command2, HystrixEventType.Success);
        AssertCommandExecutionEvents(command3, HystrixEventType.Success, HystrixEventType.ResponseFromCache);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        AssertSaneHystrixRequestLog(3);
    }

    [Fact]
    public void TestRequestCacheWithSlowExecution()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var command1 = new SlowCacheableCommand(circuitBreaker, "A", 200);
        var command2 = new SlowCacheableCommand(circuitBreaker, "A", 100);
        var command3 = new SlowCacheableCommand(circuitBreaker, "A", 100);
        var command4 = new SlowCacheableCommand(circuitBreaker, "A", 100);

        Task<string> f1 = command1.ExecuteAsync();
        Task<string> f2 = command2.ExecuteAsync();
        Task<string> f3 = command3.ExecuteAsync();
        Task<string> f4 = command4.ExecuteAsync();

        Assert.Equal("A", f2.Result);
        Assert.Equal("A", f3.Result);
        Assert.Equal("A", f4.Result);
        Assert.Equal("A", f1.Result);

        Assert.True(command1.Executed);

        // the second one should not have executed as it should have received the cached value instead
        Assert.False(command2.Executed);
        Assert.False(command3.Executed);
        Assert.False(command4.Executed);

        Assert.True(command1.ExecutionTimeInMilliseconds > -1);
        Assert.False(command1.IsResponseFromCache);
        Assert.True(command2.ExecutionTimeInMilliseconds == -1);
        Assert.True(command2.IsResponseFromCache);
        Assert.True(command3.IsResponseFromCache);
        Assert.True(command3.ExecutionTimeInMilliseconds == -1);
        Assert.True(command4.IsResponseFromCache);
        Assert.True(command4.ExecutionTimeInMilliseconds == -1);
        AssertCommandExecutionEvents(command1, HystrixEventType.Success);
        AssertCommandExecutionEvents(command2, HystrixEventType.Success, HystrixEventType.ResponseFromCache);
        AssertCommandExecutionEvents(command3, HystrixEventType.Success, HystrixEventType.ResponseFromCache);
        AssertCommandExecutionEvents(command4, HystrixEventType.Success, HystrixEventType.ResponseFromCache);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(4);
        _output.WriteLine("HystrixRequestLog: " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
    }

    [Fact]
    public void TestNoRequestCache3()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var command1 = new SuccessfulCacheableCommand<string>(circuitBreaker, false, "A");
        var command2 = new SuccessfulCacheableCommand<string>(circuitBreaker, false, "B");
        var command3 = new SuccessfulCacheableCommand<string>(circuitBreaker, false, "A");

        Assert.True(command1.IsCommandRunningInThread);

        Task<string> f1 = command1.ExecuteAsync();
        Task<string> f2 = command2.ExecuteAsync();
        Task<string> f3 = command3.ExecuteAsync();

        Assert.Equal("A", f1.Result);
        Assert.Equal("B", f2.Result);
        Assert.Equal("A", f3.Result);

        Assert.True(command1.Executed);

        // both should execute as they are different
        Assert.True(command2.Executed);

        // this should also execute since we disabled the cache
        Assert.True(command3.Executed);

        AssertCommandExecutionEvents(command1, HystrixEventType.Success);
        AssertCommandExecutionEvents(command2, HystrixEventType.Success);
        AssertCommandExecutionEvents(command3, HystrixEventType.Success);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(3);
    }

    [Fact]
    public void TestRequestCacheViaQueueSemaphore1()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var command1 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "A");
        var command2 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "B");
        var command3 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "A");

        Assert.False(command1.IsCommandRunningInThread);

        Task<string> f1 = command1.ExecuteAsync();
        Task<string> f2 = command2.ExecuteAsync();
        Task<string> f3 = command3.ExecuteAsync();

        Assert.Equal("A", f1.Result);
        Assert.Equal("B", f2.Result);
        Assert.Equal("A", f3.Result);

        Assert.True(command1.Executed);

        // both should execute as they are different
        Assert.True(command2.Executed);

        // but the 3rd should come from cache
        Assert.False(command3.Executed);
        Assert.True(command3.IsResponseFromCache);
        Assert.True(command3.ExecutionTimeInMilliseconds == -1);
        AssertCommandExecutionEvents(command1, HystrixEventType.Success);
        AssertCommandExecutionEvents(command2, HystrixEventType.Success);
        AssertCommandExecutionEvents(command3, HystrixEventType.Success, HystrixEventType.ResponseFromCache);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(3);
    }

    [Fact]
    public void TestNoRequestCacheViaQueueSemaphore1()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var command1 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "A");
        var command2 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "B");
        var command3 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "A");

        Assert.False(command1.IsCommandRunningInThread);

        Task<string> f1 = command1.ExecuteAsync();
        Task<string> f2 = command2.ExecuteAsync();
        Task<string> f3 = command3.ExecuteAsync();

        Assert.Equal("A", f1.Result);
        Assert.Equal("B", f2.Result);
        Assert.Equal("A", f3.Result);

        Assert.True(command1.Executed);

        // both should execute as they are different
        Assert.True(command2.Executed);

        // this should also execute because caching is disabled
        Assert.True(command3.Executed);
        AssertCommandExecutionEvents(command1, HystrixEventType.Success);
        AssertCommandExecutionEvents(command2, HystrixEventType.Success);
        AssertCommandExecutionEvents(command3, HystrixEventType.Success);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(3);
    }

    [Fact]
    public void TestRequestCacheViaExecuteSemaphore1()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var command1 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "A");
        var command2 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "B");
        var command3 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, true, "A");

        Assert.False(command1.IsCommandRunningInThread);

        string f1 = command1.Execute();
        string f2 = command2.Execute();
        string f3 = command3.Execute();

        Assert.Equal("A", f1);
        Assert.Equal("B", f2);
        Assert.Equal("A", f3);

        Assert.True(command1.Executed);

        // both should execute as they are different
        Assert.True(command2.Executed);

        // but the 3rd should come from cache
        Assert.False(command3.Executed);
        AssertCommandExecutionEvents(command1, HystrixEventType.Success);
        AssertCommandExecutionEvents(command2, HystrixEventType.Success);
        AssertCommandExecutionEvents(command3, HystrixEventType.Success, HystrixEventType.ResponseFromCache);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(3);
    }

    [Fact]
    public void TestNoRequestCacheViaExecuteSemaphore1()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var command1 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "A");
        var command2 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "B");
        var command3 = new SuccessfulCacheableCommandViaSemaphore(circuitBreaker, false, "A");

        Assert.False(command1.IsCommandRunningInThread);

        string f1 = command1.Execute();
        string f2 = command2.Execute();
        string f3 = command3.Execute();

        Assert.Equal("A", f1);
        Assert.Equal("B", f2);
        Assert.Equal("A", f3);

        Assert.True(command1.Executed);

        // both should execute as they are different
        Assert.True(command2.Executed);

        // this should also execute because caching is disabled
        Assert.True(command3.Executed);
        AssertCommandExecutionEvents(command1, HystrixEventType.Success);
        AssertCommandExecutionEvents(command2, HystrixEventType.Success);
        AssertCommandExecutionEvents(command3, HystrixEventType.Success);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(3);
    }

    [Fact]
    public void TestNoRequestCacheOnTimeoutThrowsException()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var r1 = new NoRequestCacheTimeoutWithoutFallback(circuitBreaker);

        try
        {
            _output.WriteLine("r1 value: " + r1.Execute());

            // we should have thrown an exception
            Assert.True(false, "expected a timeout");
        }
        catch (HystrixRuntimeException)
        {
            Assert.True(r1.IsResponseTimedOut);

            // what we want
        }

        var r2 = new NoRequestCacheTimeoutWithoutFallback(circuitBreaker);

        try
        {
            r2.Execute();

            // we should have thrown an exception
            Assert.True(false, "expected a timeout");
        }
        catch (HystrixRuntimeException)
        {
            Assert.True(r2.IsResponseTimedOut);

            // what we want
        }

        var r3 = new NoRequestCacheTimeoutWithoutFallback(circuitBreaker);
        Task<bool> f3 = r3.ExecuteAsync();

        try
        {
            _ = f3.Result;

            // we should have thrown an exception
            Assert.True(false, "expected a timeout");
        }
        catch (Exception)
        {
            // e.printStackTrace();
            Assert.True(r3.IsResponseTimedOut);

            // what we want
        }

        Time.Wait(500); // timeout on command is set to 200ms

        var r4 = new NoRequestCacheTimeoutWithoutFallback(circuitBreaker);

        try
        {
            r4.Execute();

            // we should have thrown an exception
            Assert.True(false, "expected a timeout");
        }
        catch (HystrixRuntimeException)
        {
            Assert.True(r4.IsResponseTimedOut);
            Assert.False(r4.IsResponseFromFallback);

            // what we want
        }

        AssertCommandExecutionEvents(r1, HystrixEventType.Timeout, HystrixEventType.FallbackMissing);
        AssertCommandExecutionEvents(r2, HystrixEventType.Timeout, HystrixEventType.FallbackMissing);
        AssertCommandExecutionEvents(r3, HystrixEventType.Timeout, HystrixEventType.FallbackMissing);
        AssertCommandExecutionEvents(r4, HystrixEventType.Timeout, HystrixEventType.FallbackMissing);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(4);
    }

    [Fact]
    public void TestRequestCacheOnTimeoutCausesNullPointerException()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var command1 = new RequestCacheNullPointerExceptionCase(circuitBreaker);
        var command2 = new RequestCacheNullPointerExceptionCase(circuitBreaker);
        var command3 = new RequestCacheNullPointerExceptionCase(circuitBreaker);

        // Expect it to time out - all results should be false
        Assert.False(command1.Execute());
        Assert.False(command2.Execute()); // return from cache #1
        Assert.False(command3.Execute()); // return from cache #2
        Time.Wait(500); // timeout on command is set to 200ms

        var command4 = new RequestCacheNullPointerExceptionCase(circuitBreaker);
        bool value = command4.Execute(); // return from cache #3
        Assert.False(value);
        var command5 = new RequestCacheNullPointerExceptionCase(circuitBreaker);
        Task<bool> f = command5.ExecuteAsync(); // return from cache #4

        // the bug is that we're getting a null Future back, rather than a Future that returns false
        Assert.NotNull(f);
        Assert.False(f.Result);

        Assert.True(command5.IsResponseFromFallback);
        Assert.True(command5.IsResponseTimedOut);
        Assert.False(command5.IsFailedExecution);
        Assert.False(command5.IsResponseShortCircuited);
        Assert.NotNull(command5.ExecutionException);

        AssertCommandExecutionEvents(command1, HystrixEventType.Timeout, HystrixEventType.FallbackSuccess);
        AssertCommandExecutionEvents(command2, HystrixEventType.Timeout, HystrixEventType.FallbackSuccess, HystrixEventType.ResponseFromCache);
        AssertCommandExecutionEvents(command3, HystrixEventType.Timeout, HystrixEventType.FallbackSuccess, HystrixEventType.ResponseFromCache);
        AssertCommandExecutionEvents(command4, HystrixEventType.Timeout, HystrixEventType.FallbackSuccess, HystrixEventType.ResponseFromCache);
        AssertCommandExecutionEvents(command5, HystrixEventType.Timeout, HystrixEventType.FallbackSuccess, HystrixEventType.ResponseFromCache);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(5);
    }

    [Fact]
    public void TestRequestCacheOnTimeoutThrowsException()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var r1 = new RequestCacheTimeoutWithoutFallback(circuitBreaker);

        try
        {
            _output.WriteLine("r1 value: " + r1.Execute());

            // we should have thrown an exception
            Assert.True(false, "expected a timeout");
        }
        catch (HystrixRuntimeException)
        {
            Assert.True(r1.IsResponseTimedOut);

            // what we want
        }

        var r2 = new RequestCacheTimeoutWithoutFallback(circuitBreaker);

        try
        {
            r2.Execute();

            // we should have thrown an exception
            Assert.True(false, "expected a timeout");
        }
        catch (HystrixRuntimeException)
        {
            Assert.True(r2.IsResponseTimedOut);

            // what we want
        }

        var r3 = new RequestCacheTimeoutWithoutFallback(circuitBreaker);
        Task<bool> f3 = r3.ExecuteAsync();

        try
        {
            _ = f3.Result;

            // we should have thrown an exception
            Assert.True(false, "expected a timeout");
        }
        catch (Exception)
        {
            // e.printStackTrace();
            Assert.True(r3.IsResponseTimedOut);

            // what we want
        }

        Time.Wait(500); // timeout on command is set to 200ms

        var r4 = new RequestCacheTimeoutWithoutFallback(circuitBreaker);

        try
        {
            r4.Execute();

            // we should have thrown an exception
            Assert.False(true, "expected a timeout");
        }
        catch (HystrixRuntimeException)
        {
            Assert.True(r4.IsResponseTimedOut);
            Assert.False(r4.IsResponseFromFallback);

            // what we want
        }

        AssertCommandExecutionEvents(r1, HystrixEventType.Timeout, HystrixEventType.FallbackMissing);
        AssertCommandExecutionEvents(r2, HystrixEventType.Timeout, HystrixEventType.FallbackMissing, HystrixEventType.ResponseFromCache);
        AssertCommandExecutionEvents(r3, HystrixEventType.Timeout, HystrixEventType.FallbackMissing, HystrixEventType.ResponseFromCache);
        AssertCommandExecutionEvents(r4, HystrixEventType.Timeout, HystrixEventType.FallbackMissing, HystrixEventType.ResponseFromCache);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(4);
    }

    [Fact]
    public async Task TestRequestCacheOnThreadRejectionThrowsException()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var completionLatch = new CountdownEvent(1);
        var r1 = new RequestCacheThreadRejectionWithoutFallback(circuitBreaker, completionLatch);

        try
        {
            _output.WriteLine("r1: " + r1.Execute());

            // we should have thrown an exception
            Assert.True(false, "expected a rejection");
        }
        catch (HystrixRuntimeException)
        {
            Assert.True(r1.IsResponseRejected);

            // what we want
        }

        var r2 = new RequestCacheThreadRejectionWithoutFallback(circuitBreaker, completionLatch);

        try
        {
            _output.WriteLine("r2: " + r2.Execute());

            // we should have thrown an exception
            Assert.True(false, "expected a rejection");
        }
        catch (HystrixRuntimeException)
        {
            // e.printStackTrace();
            Assert.True(r2.IsResponseRejected);

            // what we want
        }

        var r3 = new RequestCacheThreadRejectionWithoutFallback(circuitBreaker, completionLatch);

        try
        {
            _output.WriteLine("f3: " + await r3.ExecuteAsync());

            // we should have thrown an exception
            Assert.True(false, "expected a rejection");
        }
        catch (HystrixRuntimeException)
        {
            // } catch (HystrixRuntimeException e) {
            //                e.printStackTrace();
            Assert.True(r3.IsResponseRejected);

            // what we want
        }

        // let the command finish (only 1 should actually be blocked on this due to the response cache)
        completionLatch.SignalEx();

        // then another after the command has completed
        var r4 = new RequestCacheThreadRejectionWithoutFallback(circuitBreaker, completionLatch);

        try
        {
            _output.WriteLine("r4: " + r4.Execute());

            // we should have thrown an exception
            Assert.True(false, "expected a rejection");
        }
        catch (HystrixRuntimeException)
        {
            // e.printStackTrace();
            Assert.True(r4.IsResponseRejected);
            Assert.False(r4.IsResponseFromFallback);

            // what we want
        }

        AssertCommandExecutionEvents(r1, HystrixEventType.ThreadPoolRejected, HystrixEventType.FallbackMissing);
        AssertCommandExecutionEvents(r2, HystrixEventType.ThreadPoolRejected, HystrixEventType.FallbackMissing, HystrixEventType.ResponseFromCache);
        AssertCommandExecutionEvents(r3, HystrixEventType.ThreadPoolRejected, HystrixEventType.FallbackMissing, HystrixEventType.ResponseFromCache);
        AssertCommandExecutionEvents(r4, HystrixEventType.ThreadPoolRejected, HystrixEventType.FallbackMissing, HystrixEventType.ResponseFromCache);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(4);
    }

    [Fact]
    public async Task TestBasicExecutionWorksWithoutRequestVariable()
    {
        /* force the RequestVariable to not be initialized */
        HystrixRequestContext.SetContextOnCurrentThread(null);

        TestHystrixCommand<bool> command = new SuccessfulTestCommand();
        Assert.True(command.Execute());

        TestHystrixCommand<bool> command2 = new SuccessfulTestCommand();
        Assert.True(await command2.ExecuteAsync());
    }

    [Fact]
    public async Task TestCacheKeyExecutionRequiresRequestVariable()
    {
        /* force the RequestVariable to not be initialized */
        HystrixRequestContext.SetContextOnCurrentThread(null);

        var circuitBreaker = new TestCircuitBreaker();

        var command = new SuccessfulCacheableCommand<string>(circuitBreaker, true, "one");
        Assert.Throws<HystrixRuntimeException>(() => command.Execute());

        await Assert.ThrowsAsync<HystrixRuntimeException>(() => command.ExecuteAsync());
    }

    [Fact]
    public void TestBadRequestExceptionViaExecuteInThread()
    {
        var circuitBreaker = new TestCircuitBreaker();
        BadRequestCommand command1 = null;

        try
        {
            command1 = new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.Thread);
            command1.Execute();
            Assert.True(false, $"we expect to receive a {nameof(HystrixBadRequestException)}");
        }
        catch (HystrixBadRequestException)
        {
            // success
            // e.printStackTrace();
        }

        AssertCommandExecutionEvents(command1, HystrixEventType.BadRequest);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public async Task TestBadRequestExceptionViaQueueInThread()
    {
        var circuitBreaker = new TestCircuitBreaker();
        BadRequestCommand command1 = null;

        try
        {
            command1 = new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.Thread);
            await command1.ExecuteAsync();
            Assert.True(false, $"we expect to receive a {nameof(HystrixBadRequestException)}");
        }
        catch (Exception e)
        {
            // e.printStackTrace();
            if (e is HystrixBadRequestException)
            {
                // success
            }
            else
            {
                Assert.True(false, $"We expect a {nameof(HystrixBadRequestException)} but got a {e.GetType().Name}");
            }
        }

        AssertCommandExecutionEvents(command1, HystrixEventType.BadRequest);
        Assert.NotNull(command1.ExecutionException);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public async Task TestBadRequestExceptionViaQueueInThreadOnResponseFromCache()
    {
        var circuitBreaker = new TestCircuitBreaker();

        // execute once to cache the value
        BadRequestCommand command1 = null;

        try
        {
            command1 = new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.Thread);
            command1.Execute();
        }
        catch (Exception)
        {
            // ignore
        }

        BadRequestCommand command2 = null;

        try
        {
            command2 = new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.Thread);
            await command2.ExecuteAsync();
            Assert.True(false, $"we expect to receive a {nameof(HystrixBadRequestException)}");
        }
        catch (Exception e)
        {
            // e.printStackTrace();
            if (e is HystrixBadRequestException)
            {
                // success
            }
            else
            {
                Assert.False(true, $"We expect a {nameof(HystrixBadRequestException)} but got a {e.GetType().Name}");
            }
        }

        AssertCommandExecutionEvents(command1, HystrixEventType.BadRequest);
        AssertCommandExecutionEvents(command2, HystrixEventType.BadRequest, HystrixEventType.ResponseFromCache);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(2);
    }

    [Fact]
    public void TestBadRequestExceptionViaExecuteInSemaphore()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var command1 = new BadRequestCommand(circuitBreaker, ExecutionIsolationStrategy.Semaphore);

        try
        {
            command1.Execute();
            Assert.True(false, $"we expect to receive a {nameof(HystrixBadRequestException)}");
        }
        catch (HystrixBadRequestException)
        {
            // success
            // e.printStackTrace();
        }

        AssertCommandExecutionEvents(command1, HystrixEventType.BadRequest);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestCheckedExceptionViaExecute()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var command = new CommandWithCheckedException(circuitBreaker);

        try
        {
            command.Execute();
            Assert.True(false, $"we expect to receive a {nameof(Exception)}");
        }
        catch (Exception e)
        {
            Assert.Equal("simulated checked exception message", e.InnerException.Message);
        }

        Assert.Equal("simulated checked exception message", command.FailedExecutionException.Message);

        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsFailedExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Failure, HystrixEventType.FallbackMissing);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestCheckedExceptionViaObserve()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var command = new CommandWithCheckedException(circuitBreaker);
        var t = new AtomicReference<Exception>();
        var latch = new CountdownEvent(1);

        try
        {
            command.Observe().Subscribe(_ =>
            {
            }, e =>
            {
                t.Value = e;
                latch.SignalEx();
            }, () =>
            {
                latch.SignalEx();
            });
        }
        catch (Exception)
        {
            // e.printStackTrace();
            Assert.True(false, "we should not get anything thrown, it should be emitted via the Observer#onError method");
        }

        latch.Wait(1000);
        Assert.NotNull(t.Value);

        // t.get().printStackTrace();
        Assert.True(t.Value is HystrixRuntimeException);
        Assert.Equal("simulated checked exception message", t.Value.InnerException.Message);
        Assert.Equal("simulated checked exception message", command.FailedExecutionException.Message);
        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsFailedExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Failure, HystrixEventType.FallbackMissing);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestSemaphoreExecutionWithTimeout()
    {
        TestHystrixCommand<bool> cmd = new InterruptibleCommand(new TestCircuitBreaker());

        _output.WriteLine("Starting command");
        long timeMillis = DateTime.Now.Ticks / 10000;

        try
        {
            cmd.Execute();
            Assert.True(false, "Should throw");
        }
        catch (Exception)
        {
            Assert.NotNull(cmd.ExecutionException);

            _output.WriteLine("Unsuccessful Execution took : " + (Time.CurrentTimeMillis - timeMillis));
            AssertCommandExecutionEvents(cmd, HystrixEventType.Timeout, HystrixEventType.FallbackMissing);
            Assert.Equal(0, cmd.InnerMetrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
    }

    [Fact]
    public void TestRecoverableErrorWithNoFallbackThrowsError()
    {
        TestHystrixCommand<int> command = GetRecoverableErrorCommand(ExecutionIsolationStrategy.Thread, FallbackResultTest.Unimplemented);

        try
        {
            command.Execute();
            Assert.False(true, $"we expect to receive a {nameof(Exception)}");
        }
        catch (Exception e)
        {
            Assert.Equal("Execution ERROR for TestHystrixCommand", e.InnerException.Message);
        }

        Assert.Equal("Execution ERROR for TestHystrixCommand", command.FailedExecutionException.Message);

        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsFailedExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Failure, HystrixEventType.FallbackMissing);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.InnerMetrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestRecoverableErrorMaskedByFallbackButLogged()
    {
        TestHystrixCommand<int> command = GetRecoverableErrorCommand(ExecutionIsolationStrategy.Thread, FallbackResultTest.Success);
        Assert.Equal(FlexibleTestHystrixCommand.FallbackValue, command.Execute());

        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsFailedExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Failure, HystrixEventType.FallbackSuccess);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.InnerMetrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestUnrecoverableErrorThrownWithNoFallback()
    {
        TestHystrixCommand<int> command = GetUnrecoverableErrorCommand(ExecutionIsolationStrategy.Thread, FallbackResultTest.Unimplemented);

        try
        {
            command.Execute();
            Assert.True(false, $"we expect to receive a {nameof(Exception)}");
        }
        catch (Exception e)
        {
            Assert.Equal("Unrecoverable Error for TestHystrixCommand", e.InnerException.Message);
        }

        Assert.Equal("Unrecoverable Error for TestHystrixCommand", command.FailedExecutionException.Message);

        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsFailedExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Failure);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.InnerMetrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact] // even though fallback is implemented, that logic never fires, as this is an unrecoverable error and should be directly propagated to the caller
    public void TestUnrecoverableErrorThrownWithFallback()
    {
        TestHystrixCommand<int> command = GetUnrecoverableErrorCommand(ExecutionIsolationStrategy.Thread, FallbackResultTest.Success);

        try
        {
            command.Execute();
            Assert.False(true, $"we expect to receive a {nameof(Exception)}");
        }
        catch (Exception e)
        {
            Assert.Equal("Unrecoverable Error for TestHystrixCommand", e.InnerException.Message);
        }

        Assert.Equal("Unrecoverable Error for TestHystrixCommand", command.FailedExecutionException.Message);

        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsFailedExecution);
        AssertCommandExecutionEvents(command, HystrixEventType.Failure);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.InnerMetrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestNonBlockingCommandQueueFiresTimeout()
    {
        // see https://github.com/Netflix/Hystrix/issues/514
        TestHystrixCommand<int> cmd = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 200, FallbackResultTest.Success, 50);
        cmd.IsFallbackUserDefined = true;

        // await cmd.ExecuteAsync();
        cmd.ExecuteAsync();

        // t.Start();
        Time.Wait(200);

        // timeout should occur in 50ms, and underlying thread should run for 500ms
        // therefore, after 200ms, the command should have finished with a fallback on timeout
        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

        Assert.True(cmd.IsExecutionComplete);
        Assert.True(cmd.IsResponseTimedOut);

        Assert.Equal(0, cmd.InnerMetrics.CurrentConcurrentExecutionCount);
    }

    [Fact]
    public void TestExecutionFailureWithFallbackImplementedButDisabled()
    {
        TestHystrixCommand<bool> commandEnabled = new KnownFailureTestCommandWithFallback(new TestCircuitBreaker(), true);

        try
        {
            Assert.False(commandEnabled.Execute());
        }
        catch (Exception)
        {
            // e.printStackTrace();
            Assert.True(false, "We should have received a response from the fallback.");
        }

        TestHystrixCommand<bool> commandDisabled = new KnownFailureTestCommandWithFallback(new TestCircuitBreaker(), false);

        try
        {
            Assert.False(commandDisabled.Execute());
            Assert.False(true, "expect exception thrown");
        }
        catch (Exception)
        {
            // expected
        }

        Assert.Equal("we failed with a simulated issue", commandDisabled.FailedExecutionException.Message);

        Assert.True(commandDisabled.IsFailedExecution);
        AssertCommandExecutionEvents(commandEnabled, HystrixEventType.Failure, HystrixEventType.FallbackSuccess);
        AssertCommandExecutionEvents(commandDisabled, HystrixEventType.Failure);
        Assert.NotNull(commandDisabled.ExecutionException);
        Assert.Equal(0, commandDisabled.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(2);
    }

    [Fact]
    public void TestExecutionTimeoutValue()
    {
        var properties = new HystrixCommandOptions
        {
            GroupKey = HystrixCommandGroupKeyDefault.AsKey("TestKey"),
            ExecutionTimeoutInMilliseconds = 50
        };

        HystrixCommand<string> command = new TestExecutionTimeoutValueHystrixCommand(properties)
        {
            IsFallbackUserDefined = true
        };

        string value = command.Execute();
        Assert.True(command.IsResponseTimedOut);
        Assert.Equal("timed-out", value);
    }

    [Fact]
    public void TestObservableTimeoutNoFallbackThreadContext()
    {
        var latch = new CountdownEvent(1);
        var onErrorThread = new AtomicReference<Thread>();
        var isRequestContextInitialized = new AtomicBoolean();

        TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 200, FallbackResultTest.Unimplemented, 50);
        Exception onErrorEvent = null;

        command.ToObservable().Subscribe(_ =>
        {
        }, ex =>
        {
            onErrorEvent = ex;
            _output.WriteLine("onError: " + ex);
            _output.WriteLine("onError Thread: " + Thread.CurrentThread);
            _output.WriteLine("ThreadContext in onError: " + HystrixRequestContext.IsCurrentThreadInitialized);
            onErrorThread.Value = Thread.CurrentThread;
            isRequestContextInitialized.Value = HystrixRequestContext.IsCurrentThreadInitialized;
            latch.SignalEx();
        }, () =>
        {
            latch.SignalEx();
        });

        latch.Wait(5000);

        Assert.True(isRequestContextInitialized.Value);
        Assert.True(onErrorThread.Value != null);

        if (onErrorEvent is HystrixRuntimeException de)
        {
            Assert.NotNull(de.FallbackException);
            Assert.True(de.FallbackException is InvalidOperationException);
            Assert.NotNull(de.ImplementingType);
            Assert.NotNull(de.InnerException);
            Assert.True(de.InnerException is TimeoutException);
        }
        else
        {
            Assert.False(true, "the exception should be ExecutionException with cause as HystrixRuntimeException");
        }

        Assert.True(command.ExecutionTimeInMilliseconds > -1);
        Assert.True(command.IsResponseTimedOut);
        AssertCommandExecutionEvents(command, HystrixEventType.Timeout, HystrixEventType.FallbackMissing);
        Assert.NotNull(command.ExecutionException);
        Assert.Equal(0, command.Builder.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestExceptionConvertedToBadRequestExceptionInExecutionHookBypassesCircuitBreaker()
    {
        var circuitBreaker = new TestCircuitBreaker();
        var command = new ExceptionToBadRequestByExecutionHookCommand(circuitBreaker, ExecutionIsolationStrategy.Thread);

        try
        {
            command.Execute();
            Assert.False(true, $"we expect to receive a {nameof(HystrixBadRequestException)}");
        }
        catch (HystrixBadRequestException)
        {
            // success
            // e.printStackTrace()
        }
        catch (Exception e)
        {
            // e.printStackTrace()
            Assert.False(true, $"We expect a {nameof(HystrixBadRequestException)} but got a {e.GetType().Name}");
        }

        AssertCommandExecutionEvents(command, HystrixEventType.BadRequest);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        AssertSaneHystrixRequestLog(1);
    }

    [Fact]
    public void TestInterruptFutureOnTimeout()
    {
        // given
        var cmd = new InterruptibleCommand(new TestCircuitBreaker());

        // when
        _ = cmd.ExecuteAsync();

        // then
        Time.Wait(1000);
        Assert.True(cmd.HasBeenInterrupted);
    }

    [Fact]
    public void TestInterruptObserveOnTimeout()
    {
        // given
        var cmd = new InterruptibleCommand(new TestCircuitBreaker());

        // when
        cmd.Observe().Subscribe();

        // then
        Time.Wait(500);
        Assert.True(cmd.HasBeenInterrupted);
    }

    [Fact]
    public void TestInterruptToObservableOnTimeout()
    {
        // given
        var cmd = new InterruptibleCommand(new TestCircuitBreaker());

        // when
        cmd.ToObservable().Subscribe();

        // then
        Time.Wait(500);
        Assert.True(cmd.HasBeenInterrupted);
    }

    [Fact]
    public void TestCancelFutureWithInterruption()
    {
        // given
        var cmd = new InterruptibleCommand(new TestCircuitBreaker(), 1000);

        // when
        var cts = new CancellationTokenSource();
        Task<bool> f = cmd.ExecuteAsync(cts.Token);
        Time.Wait(500);
        cts.Cancel(true);
        Time.Wait(500);

        // then
        try
        {
            _ = f.Result;

            Assert.True(false, "Should have thrown a CancellationException");
        }
        catch (Exception)
        {
            Assert.True(cmd.HasBeenInterrupted);
        }
    }

    [Fact]
    public void TestChainedCommand()
    {
        Assert.True(new TestChainedCommandPrimaryCommand(new TestCircuitBreaker()).Execute() == 2);
    }

    [Fact]
    public void TestSlowFallback()
    {
        Assert.True(new TestSlowFallbackPrimaryCommand(new TestCircuitBreaker()).Execute() == 1);
    }

    [Fact]
    public void TestSemaphoreThreadSafety()
    {
        const int numPermits = 1;
        var s = new SemaphoreSlim(numPermits);

        const int numThreads = 10;

        const int numTrials = 50;

        for (int t = 0; t < numTrials; t++)
        {
            _output.WriteLine("TRIAL : " + t);

            var numAcquired = new AtomicInteger(0);
            var latch = new CountdownEvent(numThreads);

            for (int i = 0; i < numThreads; i++)
            {
                var task = new Task(() =>
                {
                    bool acquired = s.TryAcquire();

                    if (acquired)
                    {
                        try
                        {
                            numAcquired.IncrementAndGet();
                            Time.Wait(500);
                        }
                        catch (Exception ex)
                        {
                            _output.WriteLine(ex.ToString());
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
                Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
            }
            catch (Exception ex)
            {
                Assert.True(false, ex.Message);
            }

            _output.WriteLine("Number acquired: " + numAcquired.Value);
            _output.WriteLine("Current Count: " + s.CurrentCount);

            Assert.Equal(numPermits, numAcquired.Value);
            Assert.Equal(numPermits, s.CurrentCount);
        }
    }

    [Fact]
    public void TestCancelledTasksInQueueGetRemoved()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Cancellation-A");
        var circuitBreaker = new TestCircuitBreaker();
        var pool = new SingleThreadedPoolWithQueue(10, 1);
        var command1 = new TestCommandRejection(_output, key, circuitBreaker, pool, 500, 600, TestCommandRejection.FallbackNotImplemented);
        var command2 = new TestCommandRejection(_output, key, circuitBreaker, pool, 500, 600, TestCommandRejection.FallbackNotImplemented);

        // this should go through the queue and into the thread pool
        Task<bool> poolFiller = command1.ExecuteAsync();
        Time.Wait(30); // Let it start

        // this command will stay in the queue until the thread pool is empty
        IObservable<bool> cmdInQueue = command2.Observe();
        IDisposable s = cmdInQueue.Subscribe();
        Time.Wait(30); // Let it get in queue
        Assert.Equal(1, pool.CurrentQueueSize);
        s.Dispose();
        Assert.True(command2.Token.IsCancellationRequested);

        // Assert.Equal(0, pool.CurrentQueueSize);
        // make sure we wait for the command to finish so the state is clean for next test
        _ = poolFiller.Result;
        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

        Time.Wait(100);

        AssertCommandExecutionEvents(command1, HystrixEventType.Success);
        AssertCommandExecutionEvents(command2, HystrixEventType.Cancelled);
        Assert.Equal(0, circuitBreaker.Metrics.CurrentConcurrentExecutionCount);
        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        AssertSaneHystrixRequestLog(2);
        pool.Dispose();
    }

    [Fact]
    public void TestOnRunStartHookThrowsSemaphoreIsolated()
    {
        var exceptionEncountered = new AtomicBoolean(false);
        var onThreadStartInvoked = new AtomicBoolean(false);
        var onThreadCompleteInvoked = new AtomicBoolean(false);
        var executionAttempted = new AtomicBoolean(false);

        var failureInjectionHook = new TestOnRunStartHookThrowsSemaphoreIsolatedFailureInjectionHook(onThreadStartInvoked, onThreadCompleteInvoked);

        TestHystrixCommand<int> semaphoreCmd =
            new TestOnRunStartHookThrowsSemaphoreIsolatedFailureInjectedCommand(ExecutionIsolationStrategy.Semaphore, executionAttempted, failureInjectionHook);

        try
        {
            int result = semaphoreCmd.Execute();
            _output.WriteLine("RESULT : " + result);
        }
        catch (Exception)
        {
            // ex.printStackTrace();
            exceptionEncountered.Value = true;
        }

        Assert.True(exceptionEncountered.Value);
        Assert.False(onThreadStartInvoked.Value);
        Assert.False(onThreadCompleteInvoked.Value);
        Assert.False(executionAttempted.Value);
        Assert.Equal(0, semaphoreCmd.InnerMetrics.CurrentConcurrentExecutionCount);
    }

    [Fact]
    public void TestOnRunStartHookThrowsThreadIsolated()
    {
        var exceptionEncountered = new AtomicBoolean(false);
        var onThreadStartInvoked = new AtomicBoolean(false);
        var onThreadCompleteInvoked = new AtomicBoolean(false);
        var executionAttempted = new AtomicBoolean(false);

        var failureInjectionHook = new TestOnRunStartHookThrowsThreadIsolatedFailureInjectionHook(onThreadStartInvoked, onThreadCompleteInvoked);

        TestHystrixCommand<int> threadCmd =
            new TestOnRunStartHookThrowsThreadIsolatedFailureInjectedCommand(ExecutionIsolationStrategy.Thread, executionAttempted, failureInjectionHook);

        try
        {
            int result = threadCmd.Execute();
            _output.WriteLine("RESULT : " + result);
        }
        catch (Exception)
        {
            // ex.printStackTrace();
            exceptionEncountered.Value = true;
        }

        Assert.True(exceptionEncountered.Value);
        Assert.True(onThreadStartInvoked.Value);
        Assert.True(onThreadCompleteInvoked.Value);
        Assert.False(executionAttempted.Value);
        Assert.Equal(0, threadCmd.InnerMetrics.CurrentConcurrentExecutionCount);
    }

    [Fact]
    public void TestEarlyUnsubscribeDuringExecutionViaToObservable()
    {
        HystrixCommand<bool> cmd = new TestEarlyUnsubscribeDuringExecutionViaToObservableAsyncCommand();

        var latch = new CountdownEvent(1);

        IObservable<bool> o = cmd.ToObservable();

        IDisposable s = o.Finally(() =>
        {
            _output.WriteLine("OnUnsubscribe");
            latch.SignalEx();
        }).Subscribe(b =>
        {
            _output.WriteLine("OnNext : " + b);
        }, e =>
        {
            _output.WriteLine("OnError : " + e);
        }, () =>
        {
            _output.WriteLine("OnCompleted");
        });

        try
        {
            Time.Wait(10);
            s.Dispose();
            Assert.True(latch.Wait(200));
            _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(cmd.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, cmd.GetExecutionSemaphore().CurrentCount);
            Assert.Equal(cmd.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, cmd.GetFallbackSemaphore().CurrentCount);
            Assert.False(cmd.IsExecutionComplete);
            Assert.Null(cmd.FailedExecutionException);
            Assert.Null(cmd.ExecutionException);
            _output.WriteLine("Execution time : " + cmd.ExecutionTimeInMilliseconds);
            Assert.True(cmd.ExecutionTimeInMilliseconds > -1);
            Assert.False(cmd.IsSuccessfulExecution);
            AssertCommandExecutionEvents(cmd, HystrixEventType.Cancelled);
            Assert.Equal(0, cmd.InnerMetrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        catch (Exception ex)
        {
            // ex.printStackTrace();
            _output.WriteLine(ex.ToString());
        }
    }

    [Fact]
    public void TestEarlyUnsubscribeDuringExecutionViaObserve()
    {
        HystrixCommand<bool> cmd = new TestEarlyUnsubscribeDuringExecutionViaObserveAsyncCommand();
        var latch = new CountdownEvent(1);

        IObservable<bool> o = cmd.Observe();

        IDisposable s = o.Finally(() =>
        {
            _output.WriteLine("OnUnsubscribe");
            latch.SignalEx();
        }).Subscribe(b =>
        {
            _output.WriteLine("OnNext : " + b);
        }, e =>
        {
            _output.WriteLine("OnError : " + e);
        }, () =>
        {
            _output.WriteLine("OnCompleted");
        });

        try
        {
            Time.Wait(10);
            s.Dispose();
            Assert.True(latch.Wait(200));
            _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(cmd.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, cmd.GetExecutionSemaphore().CurrentCount);
            Assert.Equal(cmd.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, cmd.GetFallbackSemaphore().CurrentCount);
            Assert.False(cmd.IsExecutionComplete);
            Assert.Null(cmd.FailedExecutionException);
            Assert.Null(cmd.ExecutionException);
            _output.WriteLine("Execution time : " + cmd.ExecutionTimeInMilliseconds);
            Assert.True(cmd.ExecutionTimeInMilliseconds > -1);
            Assert.False(cmd.IsSuccessfulExecution);
            AssertCommandExecutionEvents(cmd, HystrixEventType.Cancelled);
            Assert.Equal(0, cmd.InnerMetrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        catch (Exception ex)
        {
            // ex.printStackTrace();
            _output.WriteLine(ex.ToString());
        }
    }

    [Fact]
    public void TestEarlyUnsubscribeDuringFallback()
    {
        HystrixCommand<bool> cmd = new TestEarlyUnsubscribeDuringFallbackAsyncCommand();
        var latch = new CountdownEvent(1);

        IObservable<bool> o = cmd.ToObservable();

        IDisposable s = o.Finally(() =>
        {
            _output.WriteLine("OnUnsubscribe");
            latch.SignalEx();
        }).Subscribe(b =>
        {
            _output.WriteLine("OnNext : " + b);
        }, e =>
        {
            _output.WriteLine("OnError : " + e);
        }, () =>
        {
            _output.WriteLine("OnCompleted");
            latch.SignalEx();
        });

        try
        {
            Time.Wait(10);
            s.Dispose();
            Assert.True(latch.Wait(200));
            _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(cmd.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, cmd.GetExecutionSemaphore().CurrentCount);
            Assert.Equal(cmd.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, cmd.GetFallbackSemaphore().CurrentCount);
            Assert.False(cmd.IsExecutionComplete);

            Assert.Equal(0, cmd.InnerMetrics.CurrentConcurrentExecutionCount);
            AssertSaneHystrixRequestLog(1);
        }
        catch (Exception ex)
        {
            // ex.printStackTrace();
            _output.WriteLine(ex.ToString());
        }
    }

    [Fact]
    public void TestRequestThenCacheHitAndCacheHitUnsubscribed()
    {
        var original = new AsyncCacheableCommand("foo");
        var fromCache = new AsyncCacheableCommand("foo");

        var originalValue = new AtomicReference<object>(null);
        var fromCacheValue = new AtomicReference<object>(null);

        var originalLatch = new CountdownEvent(1);
        var fromCacheLatch = new CountdownEvent(1);

        IObservable<object> originalObservable = original.ToObservable();
        IObservable<object> fromCacheObservable = fromCache.ToObservable();

        originalObservable.Finally(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original Unsubscribe");
            originalLatch.SignalEx();
        }).Subscribe(b =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnNext : " + b);
            originalValue.Value = b;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnError : " + e);
            originalLatch.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnCompleted");
            originalLatch.SignalEx();
        });

        IDisposable fromCacheSubscription = fromCacheObservable.Finally(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache Unsubscribe");
            fromCacheLatch.SignalEx();
        }).Subscribe(b =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache OnNext : " + b);
            fromCacheValue.Value = b;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache OnError : " + e);
            fromCacheLatch.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache OnCompleted");
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
            Assert.Null(original.FailedExecutionException);
            Assert.Null(original.ExecutionException);
            Assert.True(original.ExecutionTimeInMilliseconds > -1);
            Assert.True(original.IsSuccessfulExecution);
            AssertCommandExecutionEvents(original, HystrixEventType.Success);
            Assert.NotNull(originalValue.Value);
            Assert.True((bool)originalValue.Value);
            Assert.Equal(0, original.InnerMetrics.CurrentConcurrentExecutionCount);

            Assert.Equal(fromCache.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache.GetExecutionSemaphore().CurrentCount);
            Assert.Equal(fromCache.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache.GetFallbackSemaphore().CurrentCount);
            Assert.False(fromCache.IsExecutionComplete);
            Assert.False(fromCache.IsExecutedInThread);
            Assert.Null(fromCache.FailedExecutionException);
            Assert.Null(fromCache.ExecutionException);
            AssertCommandExecutionEvents(fromCache, HystrixEventType.ResponseFromCache, HystrixEventType.Cancelled);
            Assert.True(fromCache.ExecutionTimeInMilliseconds == -1);
            Assert.False(fromCache.IsSuccessfulExecution);
            Assert.Equal(0, fromCache.InnerMetrics.CurrentConcurrentExecutionCount);

            Assert.False(original.IsCancelled); // underlying work
            _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            AssertSaneHystrixRequestLog(2);
        }
        catch (Exception ex)
        {
            _output.WriteLine(ex.ToString());
        }
    }

    [Fact]
    public void TestRequestThenCacheHitAndOriginalUnsubscribed()
    {
        var original = new AsyncCacheableCommand("foo");
        var fromCache = new AsyncCacheableCommand("foo");

        var originalValue = new AtomicReference<object>(null);
        var fromCacheValue = new AtomicReference<object>(null);

        var originalLatch = new CountdownEvent(1);
        var fromCacheLatch = new CountdownEvent(1);

        IObservable<object> originalObservable = original.ToObservable();
        IObservable<object> fromCacheObservable = fromCache.ToObservable();

        IDisposable originalSubscription = originalObservable.Finally(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original Unsubscribe");
            originalLatch.SignalEx();
        }).Subscribe(b =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnNext : " + b);
            originalValue.Value = b;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnError : " + e);
            originalLatch.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnCompleted");
            originalLatch.SignalEx();
        });

        fromCacheObservable.Finally(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache Unsubscribe");
            fromCacheLatch.SignalEx();
        }).Subscribe(b =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache OnNext : " + b);
            fromCacheValue.Value = b;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache OnError : " + e);
            fromCacheLatch.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " FromCache OnCompleted");
            fromCacheLatch.SignalEx();
        });

        try
        {
            Time.Wait(10);
            originalSubscription.Dispose();
            Assert.True(originalLatch.Wait(600));
            Assert.True(fromCacheLatch.Wait(600));
            Assert.Equal(original.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, original.GetExecutionSemaphore().CurrentCount);
            Assert.Equal(original.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, original.GetFallbackSemaphore().CurrentCount);
            Assert.False(original.IsExecutionComplete);
            Assert.True(original.IsExecutedInThread);
            Assert.Null(original.FailedExecutionException);
            Assert.Null(original.ExecutionException);
            Assert.True(original.ExecutionTimeInMilliseconds > -1);
            Assert.False(original.IsSuccessfulExecution);
            AssertCommandExecutionEvents(original, HystrixEventType.Cancelled);
            Assert.Null(originalValue.Value);
            Assert.Equal(0, original.InnerMetrics.CurrentConcurrentExecutionCount);

            Assert.Equal(fromCache.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache.GetExecutionSemaphore().CurrentCount);
            Assert.Equal(fromCache.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache.GetFallbackSemaphore().CurrentCount);
            Assert.True(fromCache.IsExecutionComplete);
            Assert.False(fromCache.IsExecutedInThread);
            Assert.Null(fromCache.FailedExecutionException);
            Assert.Null(fromCache.ExecutionException);
            AssertCommandExecutionEvents(fromCache, HystrixEventType.Success, HystrixEventType.ResponseFromCache);
            Assert.True(fromCache.ExecutionTimeInMilliseconds == -1);
            Assert.True(fromCache.IsSuccessfulExecution);
            Assert.Equal(0, fromCache.InnerMetrics.CurrentConcurrentExecutionCount);

            Assert.False(original.IsCancelled); // underlying work
            _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            AssertSaneHystrixRequestLog(2);
        }
        catch (Exception ex)
        {
            _output.WriteLine(ex.ToString());
        }
    }

    [Fact]
    public void TestRequestThenTwoCacheHitsOriginalAndOneCacheHitUnsubscribed()
    {
        var original = new AsyncCacheableCommand("foo");
        var fromCache1 = new AsyncCacheableCommand("foo");
        var fromCache2 = new AsyncCacheableCommand("foo");

        var originalValue = new AtomicReference<object>(null);
        var fromCache1Value = new AtomicReference<object>(null);
        var fromCache2Value = new AtomicReference<object>(null);

        var originalLatch = new CountdownEvent(1);
        var fromCache1Latch = new CountdownEvent(1);
        var fromCache2Latch = new CountdownEvent(1);

        IObservable<object> originalObservable = original.ToObservable();
        IObservable<object> fromCache1Observable = fromCache1.ToObservable();
        IObservable<object> fromCache2Observable = fromCache2.ToObservable();

        IDisposable originalSubscription = originalObservable.Finally(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original Unsubscribe");
            originalLatch.SignalEx();
        }).Subscribe(b =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnNext : " + b);
            originalValue.Value = b;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnError : " + e);
            originalLatch.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnCompleted");
            originalLatch.SignalEx();
        });

        fromCache1Observable.Finally(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 Unsubscribe");
            fromCache1Latch.SignalEx();
        }).Subscribe(b =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 OnNext : " + b);
            fromCache1Value.Value = b;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 OnError : " + e);
            fromCache1Latch.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 OnCompleted");
            fromCache1Latch.SignalEx();
        });

        IDisposable fromCache2Subscription = fromCache2Observable.Finally(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 Unsubscribe");
            fromCache2Latch.SignalEx();
        }).Subscribe(b =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 OnNext : " + b);
            fromCache2Value.Value = b;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 OnError : " + e);
            fromCache2Latch.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 OnCompleted");
            fromCache2Latch.SignalEx();
        });

        try
        {
            Time.Wait(10);
            originalSubscription.Dispose();

            // fromCache1Subscription.Dispose();
            fromCache2Subscription.Dispose();
            Assert.True(originalLatch.Wait(600));
            Assert.True(fromCache1Latch.Wait(600));
            Assert.True(fromCache2Latch.Wait(600));
            _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            Assert.Equal(original.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, original.GetExecutionSemaphore().CurrentCount);
            Assert.Equal(original.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, original.GetFallbackSemaphore().CurrentCount);
            Assert.False(original.IsExecutionComplete);
            Assert.True(original.IsExecutedInThread);
            Assert.Null(original.FailedExecutionException);
            Assert.Null(original.ExecutionException);
            Assert.True(original.ExecutionTimeInMilliseconds > -1);
            Assert.False(original.IsSuccessfulExecution);
            AssertCommandExecutionEvents(original, HystrixEventType.Cancelled);
            Assert.Null(originalValue.Value);
            Assert.False(original.IsCancelled); // underlying work
            Assert.Equal(0, original.InnerMetrics.CurrentConcurrentExecutionCount);

            Assert.Equal(fromCache1.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache1.GetExecutionSemaphore().CurrentCount);
            Assert.Equal(fromCache1.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache1.GetFallbackSemaphore().CurrentCount);

            Assert.True(fromCache1.IsExecutionComplete);
            Assert.False(fromCache1.IsExecutedInThread);
            Assert.Null(fromCache1.FailedExecutionException);
            Assert.Null(fromCache1.ExecutionException);
            AssertCommandExecutionEvents(fromCache1, HystrixEventType.Success, HystrixEventType.ResponseFromCache);
            Assert.True(fromCache1.ExecutionTimeInMilliseconds == -1);
            Assert.True(fromCache1.IsSuccessfulExecution);
            Assert.True((bool)fromCache1Value.Value);
            Assert.Equal(0, fromCache1.InnerMetrics.CurrentConcurrentExecutionCount);

            Assert.Equal(fromCache2.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache2.GetExecutionSemaphore().CurrentCount);
            Assert.Equal(fromCache2.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache2.GetFallbackSemaphore().CurrentCount);

            Assert.False(fromCache2.IsExecutionComplete);
            Assert.False(fromCache2.IsExecutedInThread);
            Assert.Null(fromCache2.FailedExecutionException);
            Assert.Null(fromCache2.ExecutionException);
            AssertCommandExecutionEvents(fromCache2, HystrixEventType.ResponseFromCache, HystrixEventType.Cancelled);
            Assert.True(fromCache2.ExecutionTimeInMilliseconds == -1);
            Assert.False(fromCache2.IsSuccessfulExecution);
            Assert.Null(fromCache2Value.Value);
            Assert.Equal(0, fromCache2.InnerMetrics.CurrentConcurrentExecutionCount);

            AssertSaneHystrixRequestLog(3);
        }
        catch (Exception ex)
        {
            _output.WriteLine(ex.ToString());
        }
    }

    [Fact]
    public void TestRequestThenTwoCacheHitsAllUnsubscribed()
    {
        var original = new AsyncCacheableCommand("foo");
        var fromCache1 = new AsyncCacheableCommand("foo");
        var fromCache2 = new AsyncCacheableCommand("foo");

        var originalLatch = new CountdownEvent(1);
        var fromCache1Latch = new CountdownEvent(1);
        var fromCache2Latch = new CountdownEvent(1);

        IObservable<object> originalObservable = original.ToObservable();
        IObservable<object> fromCache1Observable = fromCache1.ToObservable();
        IObservable<object> fromCache2Observable = fromCache2.ToObservable();

        IDisposable originalSubscription = originalObservable.Finally(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original Unsubscribe");
            originalLatch.SignalEx();
        }).Subscribe(b =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnNext : " + b);
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnError : " + e);
            originalLatch.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.Original OnCompleted");
            originalLatch.SignalEx();
        });

        IDisposable fromCache1Subscription = fromCache1Observable.Finally(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 Unsubscribe");
            fromCache1Latch.SignalEx();
        }).Subscribe(b =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 OnNext : " + b);
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 OnError : " + e);
            fromCache1Latch.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache1 OnCompleted");
            fromCache1Latch.SignalEx();
        });

        IDisposable fromCache2Subscription = fromCache2Observable.Finally(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 Unsubscribe");
            fromCache2Latch.SignalEx();
        }).Subscribe(b =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 OnNext : " + b);
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 OnError : " + e);
            fromCache2Latch.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Test.FromCache2 OnCompleted");
            fromCache2Latch.SignalEx();
        });

        try
        {
            Time.Wait(10);
            originalSubscription.Dispose();
            fromCache1Subscription.Dispose();
            fromCache2Subscription.Dispose();
            Assert.True(originalLatch.Wait(200));
            Assert.True(fromCache1Latch.Wait(200));
            Assert.True(fromCache2Latch.Wait(200));
            _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(original.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, original.GetExecutionSemaphore().CurrentCount);
            Assert.Equal(original.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, original.GetFallbackSemaphore().CurrentCount);

            Assert.False(original.IsExecutionComplete);
            Assert.True(original.IsExecutedInThread);
            _output.WriteLine("FEE : " + original.FailedExecutionException);

            if (original.FailedExecutionException != null)
            {
                _output.WriteLine(original.FailedExecutionException.ToString());
            }

            Assert.Null(original.FailedExecutionException);
            Assert.Null(original.ExecutionException);
            Assert.True(original.ExecutionTimeInMilliseconds > -1);
            Assert.False(original.IsSuccessfulExecution);
            AssertCommandExecutionEvents(original, HystrixEventType.Cancelled);
            Assert.True(original.IsCancelled);
            Assert.Equal(0, original.InnerMetrics.CurrentConcurrentExecutionCount);

            Assert.Equal(fromCache1.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache1.GetExecutionSemaphore().CurrentCount);
            Assert.Equal(fromCache1.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache1.GetFallbackSemaphore().CurrentCount);

            Assert.False(fromCache1.IsExecutionComplete);
            Assert.False(fromCache1.IsExecutedInThread);
            Assert.Null(fromCache1.FailedExecutionException);
            Assert.Null(fromCache1.ExecutionException);
            AssertCommandExecutionEvents(fromCache1, HystrixEventType.ResponseFromCache, HystrixEventType.Cancelled);
            Assert.True(fromCache1.ExecutionTimeInMilliseconds == -1);
            Assert.False(fromCache1.IsSuccessfulExecution);
            Assert.Equal(0, fromCache1.InnerMetrics.CurrentConcurrentExecutionCount);

            Assert.Equal(fromCache2.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache2.GetExecutionSemaphore().CurrentCount);
            Assert.Equal(fromCache2.CommandOptions.ExecutionIsolationSemaphoreMaxConcurrentRequests, fromCache2.GetFallbackSemaphore().CurrentCount);

            Assert.False(fromCache2.IsExecutionComplete);
            Assert.False(fromCache2.IsExecutedInThread);
            Assert.Null(fromCache2.FailedExecutionException);
            Assert.Null(fromCache2.ExecutionException);
            AssertCommandExecutionEvents(fromCache2, HystrixEventType.ResponseFromCache, HystrixEventType.Cancelled);
            Assert.True(fromCache2.ExecutionTimeInMilliseconds == -1);
            Assert.False(fromCache2.IsSuccessfulExecution);
            Assert.Equal(0, fromCache2.InnerMetrics.CurrentConcurrentExecutionCount);

            AssertSaneHystrixRequestLog(3);
        }
        catch (Exception ex)
        {
            _output.WriteLine(ex.ToString());
        }
    }

    [Fact]
    public void TestUnsubscribingDownstreamOperatorStillResultsInSuccessEventType()
    {
        HystrixCommand<int> cmd = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 100, FallbackResultTest.Unimplemented);

        IObservable<int> o = cmd.ToObservable().Do(i =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " CMD OnNext : " + i);
        }, throwable =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " CMD OnError : " + throwable);
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " CMD OnCompleted");
        }).OnSubscribe(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " CMD OnSubscribe");
        }).OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " CMD OnUnsubscribe");
        }).Take(1).ObserveOn(DefaultScheduler.Instance).Map(i =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Doing some more computation in the onNext!!");

            try
            {
                Time.Wait(100);
            }
            catch (Exception ex)
            {
                _output.WriteLine(ex.ToString());
            }

            return i;
        });

        var latch = new CountdownEvent(1);

        o.OnSubscribe(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnSubscribe");
        }).OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnUnsubscribe");
        }).Subscribe(i =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnNext : " + i);
        }, e =>
        {
            latch.SignalEx();
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnError : " + e);
        }, () =>
        {
            latch.SignalEx();
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnCompleted");
        });

        latch.Wait(1000);

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.True(cmd.IsExecutedInThread);
        AssertCommandExecutionEvents(cmd, HystrixEventType.Success);
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void TestUnsubscribeBeforeSubscribe()
#pragma warning restore S2699 // Tests should include assertions
    {
        // this may happen in Observable chain, so Hystrix should make sure that command never executes/allocates in this situation
        IObservable<string> error = Observable.Throw<string>(new Exception("foo"));
        HystrixCommand<int> cmd = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 100);

        IObservable<int> cmdResult = cmd.ToObservable().Do(integer =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnNext : " + integer);
        }, ex =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnError : " + ex);
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnCompleted");
        }).OnSubscribe(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnSubscribe");
        }).OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnUnsubscribe");
        });

        // the zip operator will subscribe to each observable.  there is a race between the error of the first
        // zipped observable terminating the zip and the subscription to the command's observable
        IObservable<string> zipped = error.Zip(cmdResult, (s, integer) => s + integer);

        var latch = new CountdownEvent(1);

        zipped.Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnNext : " + s);
        }, e =>
        {
            latch.SignalEx();
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnError : " + e);
        }, () =>
        {
            latch.SignalEx();
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnCompleted");
        });

        latch.Wait(1000);
        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void TestRxRetry()
#pragma warning restore S2699 // Tests should include assertions
    {
        // see https://github.com/Netflix/Hystrix/issues/1100
        // Since each command instance is single-use, the expectation is that applying the .retry() operator
        // results in only a single execution and propagation out of that error
        HystrixCommand<int> cmd = GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Failure, 300, FallbackResultTest.Unimplemented, 100);

        var latch = new CountdownEvent(1);

        _output.WriteLine(Time.CurrentTimeMillis + " : Starting");
        IObservable<int> o = cmd.ToObservable().Retry(2);
        _output.WriteLine(Time.CurrentTimeMillis + " Created retried command : " + o);

        o.Subscribe(integer =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnNext : " + integer);
        }, e =>
        {
            latch.SignalEx();
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnError : " + e);
        }, () =>
        {
            latch.SignalEx();
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnCompleted");
        });

        latch.Wait(1000);
        _output.WriteLine(Time.CurrentTimeMillis + " ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
    }

    [Fact]
    public void TestExecutionHookThreadSuccess()
    {
        AssertHooksOnSuccess(() => GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success), command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(1, 0, 1));
            Assert.True(hook.ExecutionEventsMatch(1, 0, 1));
            Assert.True(hook.FallbackEventsMatch(0, 0, 0));
            string result = hook.ExecutionSequence.ToString();

            // Steeltoe - remove deprecated!
            // Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onExecutionEmit - !onRunSuccess - !onComplete - onEmit - onExecutionSuccess - onThreadComplete - onSuccess - ", result);
            Assert.Equal("onStart - onThreadStart - onExecutionStart - onExecutionEmit - onEmit - onExecutionSuccess - onThreadComplete - onSuccess - ",
                result);
        });
    }

    [Fact]
    public void TestExecutionHookThreadBadRequestException()
    {
        AssertHooksOnFailure(() => GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.BadRequest), command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
            Assert.True(hook.ExecutionEventsMatch(0, 1, 0));
            Assert.True(hook.FallbackEventsMatch(0, 0, 0));
            Assert.Equal(typeof(HystrixBadRequestException), hook.GetCommandException().GetType());
            Assert.Equal(typeof(HystrixBadRequestException), hook.GetExecutionException().GetType());

            // Steeltoe - remove deprecated!
            // Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onExecutionError - !onRunError - onThreadComplete - onError - ", hook.executionSequence.toString());
            Assert.Equal("onStart - onThreadStart - onExecutionStart - onExecutionError - onThreadComplete - onError - ", hook.ExecutionSequence.ToString());
        });
    }

    [Fact]
    public void TestExecutionHookThreadExceptionNoFallback()
    {
        AssertHooksOnFailure(() => GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Failure, 0, FallbackResultTest.Unimplemented), command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
            Assert.True(hook.ExecutionEventsMatch(0, 1, 0));
            Assert.True(hook.FallbackEventsMatch(0, 0, 0));
            Assert.Equal(typeof(Exception), hook.GetCommandException().GetType());
            Assert.Equal(typeof(Exception), hook.GetExecutionException().GetType());
            Assert.Null(hook.GetFallbackException());

            // Steeltoe - remove deprecated!
            // Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onExecutionError - !onRunError - onThreadComplete - onError - ", hook.executionSequence.ToString());
            Assert.Equal("onStart - onThreadStart - onExecutionStart - onExecutionError - onThreadComplete - onError - ", hook.ExecutionSequence.ToString());
        });
    }

    [Fact]
    public void TestExecutionHookThreadExceptionSuccessfulFallback()
    {
        AssertHooksOnSuccess(() =>
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Failure, 0, FallbackResultTest.Success);
            command.IsFallbackUserDefined = true;
            return command;
        }, command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(1, 0, 1));
            Assert.True(hook.ExecutionEventsMatch(0, 1, 0));
            Assert.True(hook.FallbackEventsMatch(1, 0, 1));
            Assert.Equal(typeof(Exception), hook.GetExecutionException().GetType());

            // Steeltoe - remove deprecated!
            // Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onExecutionError - !onRunError - onThreadComplete - onFallbackStart - onFallbackEmit - !onFallbackSuccess - !onComplete - onEmit - onFallbackSuccess - onSuccess - ", hook.executionSequence.toString());
            Assert.Equal(
                "onStart - onThreadStart - onExecutionStart - onExecutionError - onThreadComplete - onFallbackStart - onFallbackEmit - onEmit - onFallbackSuccess - onSuccess - ",
                hook.ExecutionSequence.ToString());
        });
    }

    [Fact]
    public void TestExecutionHookThreadExceptionUnsuccessfulFallback()
    {
        AssertHooksOnFailure(() =>
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Failure, 0, FallbackResultTest.Failure);
            command.IsFallbackUserDefined = true;
            return command;
        }, command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
            Assert.True(hook.ExecutionEventsMatch(0, 1, 0));
            Assert.True(hook.FallbackEventsMatch(0, 1, 0));
            Assert.Equal(typeof(Exception), hook.GetCommandException().GetType());
            Assert.Equal(typeof(Exception), hook.GetExecutionException().GetType());
            Assert.Equal(typeof(Exception), hook.GetFallbackException().GetType());

            // Steeltoe - remove deprecated!
            // Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onExecutionError - !onRunError - onThreadComplete - onFallbackStart - onFallbackError - onError - ", hook.executionSequence.toString());
            Assert.Equal("onStart - onThreadStart - onExecutionStart - onExecutionError - onThreadComplete - onFallbackStart - onFallbackError - onError - ",
                hook.ExecutionSequence.ToString());
        });
    }

    [Fact]
    public void TestExecutionHookThreadTimeoutNoFallbackRunSuccess()
    {
        AssertHooksOnFailure(() => GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Unimplemented, 200),
            command =>
            {
                TestableExecutionHook hook = command.Builder.ExecutionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(0, 0, 0));
                Assert.Equal(typeof(TimeoutException), hook.GetCommandException().GetType());
                Assert.Null(hook.GetFallbackException());
                _output.WriteLine("RequestLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

                // Steeltoe - remove deprecated!
                // Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onThreadComplete - onError - ", hook.executionSequence.toString());
                Assert.Equal("onStart - onThreadStart - onExecutionStart - onThreadComplete - onError - ", hook.ExecutionSequence.ToString());
            });
    }

    [Fact]
    public void TestExecutionHookThreadTimeoutSuccessfulFallbackRunSuccess()
    {
        AssertHooksOnSuccess(() =>
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Success, 200);
            command.IsFallbackUserDefined = true;
            return command;
        }, command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(1, 0, 1));
            Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
            Assert.True(hook.FallbackEventsMatch(1, 0, 1));
            _output.WriteLine("RequestLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

            // Steeltoe - remove deprecated!
            // Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackEmit - !onFallbackSuccess - !onComplete - onEmit - onFallbackSuccess - onSuccess - ", hook.executionSequence.toString());
            Assert.Equal(
                "onStart - onThreadStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackEmit - onEmit - onFallbackSuccess - onSuccess - ",
                hook.ExecutionSequence.ToString());
        });
    }

    [Fact]
    public void TestExecutionHookThreadTimeoutUnsuccessfulFallbackRunSuccess()
    {
        AssertHooksOnFailure(() =>
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Failure, 200);
            command.IsFallbackUserDefined = true;
            return command;
        }, command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
            Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
            Assert.True(hook.FallbackEventsMatch(0, 1, 0));
            Assert.Equal(typeof(TimeoutException), hook.GetCommandException().GetType());
            Assert.Equal(typeof(Exception), hook.GetFallbackException().GetType());

            // Steeltoe - remove deprecated!
            // Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackError - onError - ", hook.executionSequence.toString());
            Assert.Equal("onStart - onThreadStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackError - onError - ",
                hook.ExecutionSequence.ToString());
        });
    }

    [Fact]
    public void TestExecutionHookThreadTimeoutNoFallbackRunFailure()
    {
        AssertHooksOnFailure(() => GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Failure, 500, FallbackResultTest.Unimplemented, 200),
            command =>
            {
                TestableExecutionHook hook = command.Builder.ExecutionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(0, 0, 0));
                Assert.Equal(typeof(TimeoutException), hook.GetCommandException().GetType());
                Assert.Null(hook.GetFallbackException());

                // Steeltoe - remove deprecated!
                // Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onThreadComplete - onError - ", hook.executionSequence.toString());
                Assert.Equal("onStart - onThreadStart - onExecutionStart - onThreadComplete - onError - ", hook.ExecutionSequence.ToString());
            });
    }

    [Fact]
    public void TestExecutionHookThreadTimeoutSuccessfulFallbackRunFailure()
    {
        AssertHooksOnSuccess(() =>
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Failure, 500, FallbackResultTest.Success, 200);
            command.IsFallbackUserDefined = true;
            return command;
        }, command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.True(hook.CommandEmissionsMatch(1, 0, 1));
            Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
            Assert.True(hook.FallbackEventsMatch(1, 0, 1));

            // Steeltoe - remove deprecated!
            // Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackEmit - !onFallbackSuccess - !onComplete - onEmit - onFallbackSuccess - onSuccess - ", hook.executionSequence.toString());
            Assert.Equal(
                "onStart - onThreadStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackEmit - onEmit - onFallbackSuccess - onSuccess - ",
                hook.ExecutionSequence.ToString());
        });
    }

    [Fact]
    public void TestExecutionHookThreadTimeoutUnsuccessfulFallbackRunFailure()
    {
        AssertHooksOnFailure(() =>
        {
            TestHystrixCommand<int> command = GetCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Failure, 500, FallbackResultTest.Failure, 200);
            command.IsFallbackUserDefined = true;
            return command;
        }, command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
            Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
            Assert.True(hook.FallbackEventsMatch(0, 1, 0));
            Assert.Equal(typeof(TimeoutException), hook.GetCommandException().GetType());
            Assert.Equal(typeof(Exception), hook.GetFallbackException().GetType());

            // Steeltoe - remove deprecated!
            // Assert.Equal("onStart - onThreadStart - !onRunStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackError - onError - ", hook.executionSequence.toString());
            Assert.Equal("onStart - onThreadStart - onExecutionStart - onThreadComplete - onFallbackStart - onFallbackError - onError - ",
                hook.ExecutionSequence.ToString());
        });
    }

    [Fact]
    public void TestExecutionHookThreadPoolQueueFullNoFallback()
    {
        SingleThreadedPoolWithQueue pool = null;

        AssertHooksOnFailFast(() =>
        {
            var circuitBreaker = new TestCircuitBreaker();
            pool = new SingleThreadedPoolWithQueue(1);

            try
            {
                // fill the pool
                GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Unimplemented, circuitBreaker, pool,
                    600).Observe();

                Time.Wait(10); // Let it start

                // fill the queue
                GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Unimplemented, circuitBreaker, pool,
                    600).Observe();
            }
            catch (Exception)
            {
                // ignore
            }

            return GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Unimplemented, circuitBreaker, pool,
                600);
        }, command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
            Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
            Assert.True(hook.FallbackEventsMatch(0, 0, 0));
            Assert.Equal(typeof(RejectedExecutionException), hook.GetCommandException().GetType());
            Assert.Null(hook.GetFallbackException());
            Assert.Equal("onStart - onError - ", hook.ExecutionSequence.ToString());
            pool.Dispose();
        });
    }

    [Fact]
    public void TestExecutionHookThreadPoolQueueFullSuccessfulFallback()
    {
        SingleThreadedPoolWithQueue pool = null;

        AssertHooksOnSuccess(() =>
        {
            var circuitBreaker = new TestCircuitBreaker();
            pool = new SingleThreadedPoolWithQueue(1);

            try
            {
                // fill the pool
                TestHystrixCommand<int> lat1 = GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Success,
                    circuitBreaker, pool, 600);

                lat1.IsFallbackUserDefined = true;
                lat1.Observe();
                Time.Wait(30); // Let it start

                // fill the queue
                TestHystrixCommand<int> lat2 = GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Success,
                    circuitBreaker, pool, 600);

                lat2.IsFallbackUserDefined = true;
                lat2.Observe();
            }
            catch (Exception)
            {
                // ignore
            }

            TestHystrixCommand<int> lat3 = GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Success,
                circuitBreaker, pool, 600);

            lat3.IsFallbackUserDefined = true;
            return lat3;
        }, command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(1, 0, 1));
            Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
            Assert.True(hook.FallbackEventsMatch(1, 0, 1));
            Assert.Equal("onStart - onFallbackStart - onFallbackEmit - onEmit - onFallbackSuccess - onSuccess - ", hook.ExecutionSequence.ToString());
            pool.Dispose();
        });
    }

    [Fact]
    public void TestExecutionHookThreadPoolQueueFullUnsuccessfulFallback()
    {
        SingleThreadedPoolWithQueue pool = null;

        AssertHooksOnFailFast(() =>
        {
            var circuitBreaker = new TestCircuitBreaker();
            pool = new SingleThreadedPoolWithQueue(1);

            try
            {
                // fill the pool
                TestHystrixCommand<int> lat1 = GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Failure,
                    circuitBreaker, pool, 600);

                lat1.IsFallbackUserDefined = true;
                lat1.Observe();

                Time.Wait(10); // let it start

                // fill the queue
                TestHystrixCommand<int> lat2 = GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Failure,
                    circuitBreaker, pool, 600);

                lat2.IsFallbackUserDefined = true;
                lat2.Observe();
            }
            catch (Exception)
            {
                // ignore
            }

            TestHystrixCommand<int> lat3 = GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Failure,
                circuitBreaker, pool, 600);

            lat3.IsFallbackUserDefined = true;
            return lat3;
        }, command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
            Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
            Assert.True(hook.FallbackEventsMatch(0, 1, 0));
            Assert.Equal(typeof(RejectedExecutionException), hook.GetCommandException().GetType());
            Assert.Equal(typeof(Exception), hook.GetFallbackException().GetType());
            Assert.Equal("onStart - onFallbackStart - onFallbackError - onError - ", hook.ExecutionSequence.ToString());
            pool.Dispose();
        });
    }

    [Fact]
    public void TestExecutionHookThreadPoolFullNoFallback()
    {
        SingleThreadedPoolWithNoQueue pool = null;

        AssertHooksOnFailFast(() =>
        {
            var circuitBreaker = new TestCircuitBreaker();
            pool = new SingleThreadedPoolWithNoQueue();

            try
            {
                // fill the pool
                GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Unimplemented, circuitBreaker, pool,
                    600).Observe();
            }
            catch (Exception)
            {
                // ignore
            }

            return GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Unimplemented, circuitBreaker, pool,
                600);
        }, command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
            Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
            Assert.True(hook.FallbackEventsMatch(0, 0, 0));
            Assert.Equal(typeof(RejectedExecutionException), hook.GetCommandException().GetType());
            Assert.Null(hook.GetFallbackException());
            Assert.Equal("onStart - onError - ", hook.ExecutionSequence.ToString());
            pool.Dispose();
        });
    }

    [Fact]
    public void TestExecutionHookThreadPoolFullSuccessfulFallback()
    {
        SingleThreadedPoolWithNoQueue pool = null;

        AssertHooksOnSuccess(() =>
        {
            var circuitBreaker = new TestCircuitBreaker();
            pool = new SingleThreadedPoolWithNoQueue();

            try
            {
                // fill the pool
                TestHystrixCommand<int> lat = GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Success,
                    circuitBreaker, pool, 600);

                lat.IsFallbackUserDefined = true;
                lat.Observe();
            }
            catch (Exception)
            {
                // ignore
            }

            TestHystrixCommand<int> lat2 = GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Success,
                circuitBreaker, pool, 600);

            lat2.IsFallbackUserDefined = true;
            return lat2;
        }, command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(1, 0, 1));
            Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
            Assert.Equal("onStart - onFallbackStart - onFallbackEmit - onEmit - onFallbackSuccess - onSuccess - ", hook.ExecutionSequence.ToString());
            pool.Dispose();
        });
    }

    [Fact]
    public void TestExecutionHookThreadPoolFullUnsuccessfulFallback()
    {
        SingleThreadedPoolWithNoQueue pool = null;

        AssertHooksOnFailFast(() =>
        {
            var circuitBreaker = new TestCircuitBreaker();
            pool = new SingleThreadedPoolWithNoQueue();

            try
            {
                // fill the pool
                TestHystrixCommand<int> lat = GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Failure,
                    circuitBreaker, pool, 600);

                lat.IsFallbackUserDefined = true;
                lat.Observe();
            }
            catch (Exception)
            {
                // ignore
            }

            TestHystrixCommand<int> lat1 = GetLatentCommand(ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 500, FallbackResultTest.Failure,
                circuitBreaker, pool, 600);

            lat1.IsFallbackUserDefined = true;
            return lat1;
        }, command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
            Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
            Assert.True(hook.FallbackEventsMatch(0, 1, 0));
            Assert.Equal(typeof(RejectedExecutionException), hook.GetCommandException().GetType());
            Assert.Equal(typeof(Exception), hook.GetFallbackException().GetType());
            Assert.Equal("onStart - onFallbackStart - onFallbackError - onError - ", hook.ExecutionSequence.ToString());
            pool.Dispose();
        });
    }

    [Fact]
    public void TestExecutionHookThreadShortCircuitNoFallback()
    {
        AssertHooksOnFailFast(() => GetCircuitOpenCommand(ExecutionIsolationStrategy.Thread, FallbackResultTest.Unimplemented), command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
            Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
            Assert.True(hook.FallbackEventsMatch(0, 0, 0));
            Assert.Equal(typeof(Exception), hook.GetCommandException().GetType());
            Assert.Null(hook.GetFallbackException());
            Assert.Equal("onStart - onError - ", hook.ExecutionSequence.ToString());
        });
    }

    [Fact]
    public void TestExecutionHookThreadShortCircuitSuccessfulFallback()
    {
        AssertHooksOnSuccess(() =>
        {
            TestHystrixCommand<int> command = GetCircuitOpenCommand(ExecutionIsolationStrategy.Thread, FallbackResultTest.Success);
            command.IsFallbackUserDefined = true;
            return command;
        }, command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(1, 0, 1));
            Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
            Assert.True(hook.FallbackEventsMatch(1, 0, 1));
            Assert.Equal("onStart - onFallbackStart - onFallbackEmit - onEmit - onFallbackSuccess - onSuccess - ", hook.ExecutionSequence.ToString());
        });
    }

    [Fact]
    public void TestExecutionHookThreadShortCircuitUnsuccessfulFallback()
    {
        AssertHooksOnFailFast(() =>
        {
            var circuitBreaker = new TestCircuitBreaker();
            circuitBreaker.SetForceShortCircuit(true);
            TestHystrixCommand<int> cmd = GetCircuitOpenCommand(ExecutionIsolationStrategy.Thread, FallbackResultTest.Failure);
            cmd.IsFallbackUserDefined = true;
            return cmd;
        }, command =>
        {
            TestableExecutionHook hook = command.Builder.ExecutionHook;
            Assert.True(hook.CommandEmissionsMatch(0, 1, 0));
            Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
            Assert.True(hook.FallbackEventsMatch(0, 1, 0));
            Assert.Equal(typeof(Exception), hook.GetCommandException().GetType());
            Assert.Equal(typeof(Exception), hook.GetFallbackException().GetType());
            Assert.Equal("onStart - onFallbackStart - onFallbackError - onError - ", hook.ExecutionSequence.ToString());
        });
    }

    [Fact]
    public void TestExecutionHookResponseFromCache()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("Hook-Cache");

        GetCommand(key, ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 0, FallbackResultTest.Unimplemented, 0, new TestCircuitBreaker(), null,
            100, CacheEnabledTest.Yes, 42, 10, 10).Observe();

        AssertHooksOnSuccess(
            () => GetCommand(key, ExecutionIsolationStrategy.Thread, ExecutionResultTest.Success, 0, FallbackResultTest.Unimplemented, 0,
                new TestCircuitBreaker(), null, 100, CacheEnabledTest.Yes, 42, 10, 10), command =>
            {
                TestableExecutionHook hook = command.Builder.ExecutionHook;
                Assert.True(hook.CommandEmissionsMatch(0, 0, 0));
                Assert.True(hook.ExecutionEventsMatch(0, 0, 0));
                Assert.True(hook.FallbackEventsMatch(0, 0, 0));
                Assert.Equal("onCacheHit - ", hook.ExecutionSequence.ToString());
            });
    }

    protected override TestHystrixCommand<int> GetCommand(ExecutionIsolationStrategy isolationStrategy, ExecutionResultTest executionResult,
        int executionLatency, FallbackResultTest fallbackResult, int fallbackLatency, TestCircuitBreaker circuitBreaker, IHystrixThreadPool threadPool,
        int timeout, CacheEnabledTest cacheEnabled, object value, SemaphoreSlim executionSemaphore, SemaphoreSlim fallbackSemaphore,
        bool circuitBreakerDisabled)
    {
        IHystrixCommandKey commandKey = HystrixCommandKeyDefault.AsKey($"Flexible-{Interlocked.Increment(ref _uniqueNameCounter)}");

        AbstractFlexibleTestHystrixCommand result = FlexibleTestHystrixCommand.From(commandKey, isolationStrategy, executionResult, executionLatency,
            fallbackResult, fallbackLatency, circuitBreaker, threadPool, timeout, cacheEnabled, value, executionSemaphore, fallbackSemaphore,
            circuitBreakerDisabled);

        result.Output = _output;

        if (result.ExecutionHook is TestableExecutionHook testExecHook)
        {
            testExecHook.Output = _output;
        }

        return result;
    }

    protected override TestHystrixCommand<int> GetCommand(IHystrixCommandKey commandKey, ExecutionIsolationStrategy isolationStrategy,
        ExecutionResultTest executionResult, int executionLatency, FallbackResultTest fallbackResult, int fallbackLatency, TestCircuitBreaker circuitBreaker,
        IHystrixThreadPool threadPool, int timeout, CacheEnabledTest cacheEnabled, object value, SemaphoreSlim executionSemaphore,
        SemaphoreSlim fallbackSemaphore, bool circuitBreakerDisabled)
    {
        AbstractFlexibleTestHystrixCommand result = FlexibleTestHystrixCommand.From(commandKey, isolationStrategy, executionResult, executionLatency,
            fallbackResult, fallbackLatency, circuitBreaker, threadPool, timeout, cacheEnabled, value, executionSemaphore, fallbackSemaphore,
            circuitBreakerDisabled);

        result.Output = _output;

        if (result.ExecutionHook is TestableExecutionHook testExecHook)
        {
            testExecHook.Output = _output;
        }

        return result;
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
        _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Running command.execute() and then assertions...");

        if (isSuccess)
        {
            command.Execute();
        }
        else
        {
            try
            {
                command.Execute();
                Assert.True(false, "Expected a command failure!");
            }
            catch (Exception ex)
            {
                _output.WriteLine("Received expected ex : " + ex);
            }
        }

        assertion(command);
    }

    private void AssertBlockingQueue(TestHystrixCommand<int> command, Action<TestHystrixCommand<int>> assertion, bool isSuccess)
    {
        _output.WriteLine("Running command.queue(), immediately blocking and then running assertions...");

        if (isSuccess)
        {
            command.ExecuteAsync().GetAwaiter().GetResult();
        }
        else
        {
            try
            {
                command.ExecuteAsync().GetAwaiter().GetResult();
                Assert.False(true, "Expected a command failure!");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (AggregateException ee)
            {
                _output.WriteLine("Received expected ex : " + ee.InnerException);
            }
            catch (Exception e)
            {
                _output.WriteLine("Received expected ex : " + e);
            }
        }

        assertion(command);
    }

    private void AssertNonBlockingQueue(TestHystrixCommand<int> command, Action<TestHystrixCommand<int>> assertion, bool isSuccess, bool failFast)
    {
        _output.WriteLine("Running command.queue(), sleeping the test thread until command is complete, and then running assertions...");
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
                _output.WriteLine("Received expected fail fast ex : " + ex);
            }
        }
        else
        {
            f = command.ExecuteAsync();
        }

        AwaitCommandCompletion(command);

        assertion(command);

        if (isSuccess)
        {
            _ = f.Result;
        }
        else
        {
            try
            {
                _ = f.Result;
                Assert.False(true, "Expected a command failure!");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (AggregateException ee)
            {
                _output.WriteLine("Received expected ex : " + ee.InnerException);
            }
            catch (Exception e)
            {
                _output.WriteLine("Received expected ex : " + e);
            }
        }
    }

    private void AwaitCommandCompletion<T>(TestHystrixCommand<T> command)
    {
        while (!command.IsExecutionComplete)
        {
            try
            {
                Time.Wait(10);
            }
            catch (Exception)
            {
                throw new Exception("interrupted");
            }
        }
    }

    private sealed class TestCallbackThreadForThreadIsolationTestHystrixCommand : TestHystrixCommand<bool>
    {
        private readonly AtomicReference<Thread> _commandThread;

        public TestCallbackThreadForThreadIsolationTestHystrixCommand(AtomicReference<Thread> commandThread, TestCommandBuilder builder)
            : base(builder)
        {
            _commandThread = commandThread;
        }

        protected override bool Run()
        {
            _commandThread.Value = Thread.CurrentThread;
            return true;
        }
    }

    private sealed class TestCallbackThreadForSemaphoreIsolationTestHystrixCommand : TestHystrixCommand<bool>
    {
        private readonly AtomicReference<Thread> _commandThread;

        public TestCallbackThreadForSemaphoreIsolationTestHystrixCommand(AtomicReference<Thread> commandThread, TestCommandBuilder builder)
            : base(builder)
        {
            _commandThread = commandThread;
        }

        protected override bool Run()
        {
            _commandThread.Value = Thread.CurrentThread;
            return true;
        }
    }

    private sealed class TestExecutionTimeoutValueHystrixCommand : HystrixCommand<string>
    {
        public TestExecutionTimeoutValueHystrixCommand(HystrixCommandOptions commandOptions)
            : base(commandOptions)
        {
        }

        protected override string Run()
        {
            Time.WaitUntil(() => Token.IsCancellationRequested, 3000);
            Token.ThrowIfCancellationRequested();
            return "hello";
        }

        protected override string RunFallback()
        {
            if (IsResponseTimedOut)
            {
                return "timed-out";
            }

            return "abc";
        }
    }
}
