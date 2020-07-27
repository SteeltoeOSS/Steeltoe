// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.CircuitBreaker;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Metric;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixCircuitBreakerTest : HystrixTestBase
    {
        private readonly ITestOutputHelper output;

        public HystrixCircuitBreakerTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
            Init();
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestTripCircuitAsync()
        {
            var key = "cmd-A";

            HystrixCommand<bool> cmd1 = new SuccessCommand(key, 0);
            HystrixCommand<bool> cmd2 = new SuccessCommand(key, 0);
            HystrixCommand<bool> cmd3 = new SuccessCommand(key, 0);
            HystrixCommand<bool> cmd4 = new SuccessCommand(key, 0);

            var cb = cmd1._circuitBreaker;
            Assert.True(WaitForHealthCountToUpdate(key, 1000, output), "Health count stream failed to start");

            _ = await cmd1.ExecuteAsync();
            _ = await cmd2.ExecuteAsync();
            _ = await cmd3.ExecuteAsync();
            _ = await cmd4.ExecuteAsync();

            // this should still allow requests as everything has been successful

            // Time.Wait(125);
            Assert.True(WaitForHealthCountToUpdate(key, 250, output), "Health count stream failed to update");

            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            // fail
            HystrixCommand<bool> cmd5 = new FailureCommand(key, 0);
            HystrixCommand<bool> cmd6 = new FailureCommand(key, 0);
            HystrixCommand<bool> cmd7 = new FailureCommand(key, 0);
            HystrixCommand<bool> cmd8 = new FailureCommand(key, 0);
            Assert.False(await cmd5.ExecuteAsync());
            Assert.False(await cmd6.ExecuteAsync());
            Assert.False(await cmd7.ExecuteAsync());
            Assert.False(await cmd8.ExecuteAsync());

            // make sure window has passed
            // Time.Wait(125);
            Assert.True(WaitForHealthCountToUpdate(key, 250, output), "Health count stream failed to update");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            output.WriteLine("Current CircuitBreaker Status : " + cmd1.Metrics.Healthcounts);
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestTripCircuitOnFailuresAboveThreshold()
        {
            var key = "cmd-B";

            HystrixCommand<bool> cmd1 = new SuccessCommand(key, 0);
            var cb = cmd1._circuitBreaker;
            var stream = HealthCountsStream.GetInstance(HystrixCommandKeyDefault.AsKey(key), cmd1.CommandOptions);
            Assert.True(WaitForHealthCountToUpdate(key, 1000, output), "Health count stream failed to start");

            // this should start as allowing requests
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            // success with high latency
            _ = await cmd1.ExecuteAsync();
            HystrixCommand<bool> cmd2 = new SuccessCommand(key, 0);
            _ = await cmd2.ExecuteAsync();
            HystrixCommand<bool> cmd3 = new FailureCommand(key, 0);
            _ = await cmd3.ExecuteAsync();
            HystrixCommand<bool> cmd4 = new SuccessCommand(key, 0);
            _ = await cmd4.ExecuteAsync();
            HystrixCommand<bool> cmd5 = new FailureCommand(key, 0);
            _ = await cmd5.ExecuteAsync();
            HystrixCommand<bool> cmd6 = new SuccessCommand(key, 0);
            _ = await cmd6.ExecuteAsync();
            HystrixCommand<bool> cmd7 = new FailureCommand(key, 0);
            _ = await cmd7.ExecuteAsync();
            HystrixCommand<bool> cmd8 = new FailureCommand(key, 0);
            _ = await cmd8.ExecuteAsync();

            // Let window pass, this should trip the circuit as the error percentage is above the threshold
            // Time.Wait(125);
            Assert.True(WaitForHealthCountToUpdate(key, 250, output), "Health count stream failed to update");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            output.WriteLine("Current CircuitBreaker Status : " + cmd1.Metrics.Healthcounts);
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestCircuitDoesNotTripOnFailuresBelowThreshold()
        {
            var key = "cmd-C";

            HystrixCommand<bool> cmd1 = new SuccessCommand(key, 0);
            var cb = cmd1._circuitBreaker;
            var stream = HealthCountsStream.GetInstance(HystrixCommandKeyDefault.AsKey(key), cmd1.CommandOptions);
            Assert.True(WaitForHealthCountToUpdate(key, 1000, output), "Health count stream failed to start");

            // this should start as allowing requests
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            // success with high latency
            await cmd1.ExecuteAsync();
            HystrixCommand<bool> cmd2 = new SuccessCommand(key, 0);
            await cmd2.ExecuteAsync();
            HystrixCommand<bool> cmd3 = new FailureCommand(key, 0);
            await cmd3.ExecuteAsync();
            HystrixCommand<bool> cmd4 = new SuccessCommand(key, 0);
            await cmd4.ExecuteAsync();
            HystrixCommand<bool> cmd5 = new SuccessCommand(key, 0);
            await cmd5.ExecuteAsync();
            HystrixCommand<bool> cmd6 = new FailureCommand(key, 0);
            await cmd6.ExecuteAsync();
            HystrixCommand<bool> cmd7 = new SuccessCommand(key, 0);
            await cmd7.ExecuteAsync();
            HystrixCommand<bool> cmd8 = new FailureCommand(key, 0);
            await cmd8.ExecuteAsync();

            // Allow window to pass, this should remain closed as the failure threshold is below the percentage limit
            // Time.Wait(125);
            Assert.True(WaitForHealthCountToUpdate(key, 250, output), "Health count stream failed to update");

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            output.WriteLine("Current CircuitBreaker Status : " + cmd1.Metrics.Healthcounts);
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestTripCircuitOnTimeouts()
        {
            var key = "cmd-D";

            HystrixCommand<bool> cmd1 = new TimeoutCommand(key);
            var cb = cmd1._circuitBreaker;
            var stream = HealthCountsStream.GetInstance(HystrixCommandKeyDefault.AsKey(key), cmd1.CommandOptions);
            Assert.True(WaitForHealthCountToUpdate(key, 1000, output), "Health count stream failed to start");

            // this should start as allowing requests
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            // success with high latency
            await cmd1.ExecuteAsync();
            HystrixCommand<bool> cmd2 = new TimeoutCommand(key);
            await cmd2.ExecuteAsync();
            HystrixCommand<bool> cmd3 = new TimeoutCommand(key);
            await cmd3.ExecuteAsync();
            HystrixCommand<bool> cmd4 = new TimeoutCommand(key);
            await cmd4.ExecuteAsync();

            // Allow window to pass, everything has been a timeout so we should not allow any requests
            // Time.Wait(125);
            Assert.True(WaitForHealthCountToUpdate(key, 250, output), "Health count stream failed to update");

            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestTripCircuitOnTimeoutsAboveThreshold()
        {
            var key = "cmd-E";

            HystrixCommand<bool> cmd1 = new SuccessCommand(key, 0);
            var cb = cmd1._circuitBreaker;
            var stream = HealthCountsStream.GetInstance(HystrixCommandKeyDefault.AsKey(key), cmd1.CommandOptions);
            Assert.True(WaitForHealthCountToUpdate(key, 1000, output), "Health count stream failed to start");

            // this should start as allowing requests
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            // success with high latency
            HystrixCommand<bool> cmd2 = new SuccessCommand(key, 0);
            HystrixCommand<bool> cmd3 = new TimeoutCommand(key);
            HystrixCommand<bool> cmd4 = new SuccessCommand(key, 0);
            HystrixCommand<bool> cmd5 = new TimeoutCommand(key);
            HystrixCommand<bool> cmd6 = new TimeoutCommand(key);
            HystrixCommand<bool> cmd7 = new SuccessCommand(key, 0);
            HystrixCommand<bool> cmd8 = new TimeoutCommand(key);
            HystrixCommand<bool> cmd9 = new TimeoutCommand(key);
            var taskList = new List<Task>
            {
                cmd1.ExecuteAsync(),
                cmd2.ExecuteAsync(),
                cmd3.ExecuteAsync(),
                cmd4.ExecuteAsync(),
                cmd5.ExecuteAsync(),
                cmd6.ExecuteAsync(),
                cmd7.ExecuteAsync(),
                cmd8.ExecuteAsync(),
                cmd9.ExecuteAsync(),
            };
            await Task.WhenAll(taskList);

            // Allow window to pass, this should trip the circuit as the error percentage is above the threshold
            // Time.Wait(200);
            Assert.True(WaitForHealthCountToUpdate(key, 250, output), "Health count stream failed to update");

            output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestSingleTestOnOpenCircuitAfterTimeWindow()
        {
            var key = "cmd-F";

            HystrixCommand<bool> cmd1 = new FailureCommand(key, 0);
            var cb = cmd1._circuitBreaker;
            var stream = HealthCountsStream.GetInstance(HystrixCommandKeyDefault.AsKey(key), cmd1.CommandOptions);
            Assert.True(WaitForHealthCountToUpdate(key, 1000, output), "Health count stream failed to start");

            // this should start as allowing requests
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected! (1)");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            await cmd1.ExecuteAsync();
            HystrixCommand<bool> cmd2 = new FailureCommand(key, 0);
            await cmd2.ExecuteAsync();
            HystrixCommand<bool> cmd3 = new FailureCommand(key, 0);
            await cmd3.ExecuteAsync();
            HystrixCommand<bool> cmd4 = new FailureCommand(key, 0);
            await cmd4.ExecuteAsync();

            // Allow window to pass, everything has failed in the test window so we should return false now
            // Time.Wait(200);
            Assert.True(WaitForHealthCountToUpdate(key, 250, output), "Health count stream failed to update");

            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");

            // wait for sleepWindow to pass
            Time.Wait(500);

            // we should now allow 1 request
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected! (2)");

            // but the circuit should still be open
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open! (2)");

            // and further requests are still blocked
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected! (2)");
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestCircuitClosedAfterSuccess()
        {
            var key = "cmd-G";

            var sleepWindow = 400;
            HystrixCommand<bool> cmd1 = new FailureCommand(key, 0, sleepWindow);
            var cb = (HystrixCircuitBreakerImpl)cmd1._circuitBreaker;
            var stream = HealthCountsStream.GetInstance(HystrixCommandKeyDefault.AsKey(key), cmd1.CommandOptions);
            Assert.True(WaitForHealthCountToUpdate(key, 1000, output), "Health count stream failed to start");

            // this should start as allowing requests
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            _ = await cmd1.ExecuteAsync();
            HystrixCommand<bool> cmd2 = new FailureCommand(key, 0, sleepWindow);
            _ = await cmd2.ExecuteAsync();
            HystrixCommand<bool> cmd3 = new FailureCommand(key, 0, sleepWindow);
            _ = await cmd3.ExecuteAsync();
            HystrixCommand<bool> cmd4 = new TimeoutCommand(key, sleepWindow);
            _ = await cmd4.ExecuteAsync();

            // Allow window to pass, everything has failed in the test window so we should return false now
            // Time.Wait(200);
            Assert.True(WaitForHealthCountToUpdate(key, 250, output), "Health count stream failed to update");

            output.WriteLine("ReqLog : " + Time.CurrentTimeMillis + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            output.WriteLine("CircuitBreaker state 1 : " + Time.CurrentTimeMillis + cmd1.Metrics.Healthcounts);
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");

            // wait for sleepWindow to pass
            Time.Wait(sleepWindow + 100);

            // but the circuit should still be open
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");

            // we should now allow 1 request, and upon success, should cause the circuit to be closed
            HystrixCommand<bool> cmd5 = new SuccessCommand(key, 10, sleepWindow);
            output.WriteLine("Starting test cmd : " + Time.CurrentTimeMillis + cmd1.Metrics.Healthcounts);
            _ = await cmd5.Observe();

            // Allow window to pass, all requests should be open again
            // Time.Wait(200);
            Assert.True(WaitForHealthCountToUpdate(key, 250, output), "Health count stream failed to update");

            output.WriteLine("CircuitBreaker state 2 : " + Time.CurrentTimeMillis + cmd1.Metrics.Healthcounts);
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected (1)!");
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected (2)!");
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected (3)!");

            // and the circuit should be closed again
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestMultipleTimeWindowRetriesBeforeClosingCircuit()
        {
            var key = "cmd-H";

            var sleepWindow = 400;
            HystrixCommand<bool> cmd1 = new FailureCommand(key, 0);
            var cb = cmd1._circuitBreaker;
            var stream = HealthCountsStream.GetInstance(HystrixCommandKeyDefault.AsKey(key), cmd1.CommandOptions);
            Assert.True(WaitForHealthCountToUpdate(key, 1000, output), "Health count stream failed to start");

            // this should start as allowing requests
            Assert.True(cb.AllowRequest, Time.CurrentTimeMillis + " Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, Time.CurrentTimeMillis + " Circuit breaker is open when it should be closed!");

            _ = await cmd1.ExecuteAsync();
            HystrixCommand<bool> cmd2 = new FailureCommand(key, 0);
            _ = await cmd2.ExecuteAsync();
            HystrixCommand<bool> cmd3 = new FailureCommand(key, 0);
            _ = await cmd3.ExecuteAsync();
            HystrixCommand<bool> cmd4 = new TimeoutCommand(key);
            _ = await cmd4.ExecuteAsync();

            // everything has failed in the test window so we should return false now
            // Allow window to pass,
            // Time.Wait(200);
            Assert.True(WaitForHealthCountToUpdate(key, 250, output), "Health count stream failed to update");

            output.WriteLine(Time.CurrentTimeMillis + " !!!! 1 4 failures, circuit will open on recalc");

            // Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");
            Assert.True(cb.IsOpen, Time.CurrentTimeMillis + " Circuit is closed when it should be open!");

            // wait for sleepWindow to pass
            output.WriteLine(Time.CurrentTimeMillis + " !!!! 2 Sleep window starting where all commands fail-fast");
            Time.Wait(sleepWindow + 50);
            output.WriteLine(Time.CurrentTimeMillis + " !!!! 3 Sleep window over, should allow singleTest()");

            // but the circuit should still be open
            Assert.True(cb.IsOpen, Time.CurrentTimeMillis + " Circuit is closed when it should be open!");

            // we should now allow 1 request, and upon failure, should not affect the circuit breaker, which should remain open
            HystrixCommand<bool> cmd5 = new FailureCommand(key, 50);
            var asyncResult5 = cmd5.Observe();
            output.WriteLine(Time.CurrentTimeMillis + " !!!! Kicked off the single-test");

            // and further requests are still blocked while the singleTest command is in flight
            Assert.False(cb.AllowRequest, Time.CurrentTimeMillis + " Request allowed when NOT expected!");
            output.WriteLine(Time.CurrentTimeMillis + " !!!! Confirmed that no other requests go out during single-test");

            await asyncResult5.SingleAsync();
            output.WriteLine(Time.CurrentTimeMillis + " !!!! SingleTest just completed");

            // all requests should still be blocked, because the singleTest failed
            Assert.False(cb.AllowRequest, Time.CurrentTimeMillis + " Request allowed (1) when NOT expected!");
            Assert.False(cb.AllowRequest, Time.CurrentTimeMillis + " Request allowed (2) when NOT expected!");
            Assert.False(cb.AllowRequest, Time.CurrentTimeMillis + " Request allowed (3) when NOT expected!");

            // wait for sleepWindow to pass
            output.WriteLine(Time.CurrentTimeMillis + " !!!! 2nd sleep window START");
            Time.Wait(sleepWindow + 50);
            output.WriteLine(Time.CurrentTimeMillis + " !!!! 2nd sleep window over");

            // we should now allow 1 request, and upon failure, should not affect the circuit breaker, which should remain open
            HystrixCommand<bool> cmd6 = new FailureCommand(key, 50);
            var asyncResult6 = cmd6.Observe();
            output.WriteLine(Time.CurrentTimeMillis + " 2nd singleTest just kicked off");

            // and further requests are still blocked while the singleTest command is in flight
            Assert.False(cb.AllowRequest, Time.CurrentTimeMillis + " Request allowed when NOT expected!");
            Assert.False(await asyncResult6.SingleAsync());
            output.WriteLine(Time.CurrentTimeMillis + " 2nd singleTest now over");

            // all requests should still be blocked, because the singleTest failed
            Assert.False(cb.AllowRequest, Time.CurrentTimeMillis + " Request allowed (1) when NOT expected!");
            Assert.False(cb.AllowRequest, Time.CurrentTimeMillis + " Request allowed (2) when NOT expected!");
            Assert.False(cb.AllowRequest, Time.CurrentTimeMillis + " Request allowed (3) when NOT expected!");

            // wait for sleepWindow to pass
            Time.Wait(sleepWindow);

            // but the circuit should still be open
            Assert.True(cb.IsOpen, Time.CurrentTimeMillis + " Circuit is closed when it should be open!");

            // we should now allow 1 request, and upon success, should cause the circuit to be closed
            HystrixCommand<bool> cmd7 = new SuccessCommand(key, 50);
            var asyncResult7 = cmd7.Observe();

            // and further requests are still blocked while the singleTest command is in flight
            Assert.False(cb.AllowRequest, Time.CurrentTimeMillis + " Request allowed when NOT expected!");

            await asyncResult7.SingleAsync();

            // all requests should be open again
            Assert.True(cb.AllowRequest, Time.CurrentTimeMillis + " Request NOT allowed (1) when expected!");
            Assert.True(cb.AllowRequest, Time.CurrentTimeMillis + " Request NOT allowed (2) when expected!");
            Assert.True(cb.AllowRequest, Time.CurrentTimeMillis + " Request NOT allowed (3) when expected!");

            // and the circuit should be closed again
            Assert.False(cb.IsOpen, Time.CurrentTimeMillis + " Circuit breaker is open when it should be closed!");

            // and the circuit should be closed again
            Assert.False(cb.IsOpen, Time.CurrentTimeMillis + " Circuit breaker is open when it should be closed!");
        }

        [Fact]
        public async Task TestLowVolumeDoesNotTripCircuit()
        {
            var key = "cmd-I";

            var sleepWindow = 400;
            var lowVolume = 5;

            HystrixCommand<bool> cmd1 = new FailureCommand(key, 0, sleepWindow, lowVolume);
            var cb = cmd1._circuitBreaker;
            var stream = HealthCountsStream.GetInstance(HystrixCommandKeyDefault.AsKey(key), cmd1.CommandOptions);
            Assert.True(WaitForHealthCountToUpdate(key, 1000, output), "Health count stream failed to start");

            // this should start as allowing requests
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            await cmd1.ExecuteAsync();
            HystrixCommand<bool> cmd2 = new FailureCommand(key, 0, sleepWindow, lowVolume);
            await cmd2.ExecuteAsync();
            HystrixCommand<bool> cmd3 = new FailureCommand(key, 0, sleepWindow, lowVolume);
            await cmd3.ExecuteAsync();
            HystrixCommand<bool> cmd4 = new FailureCommand(key, 0, sleepWindow, lowVolume);
            await cmd4.ExecuteAsync();

            // Allow window to pass, even though it has all failed we won't trip the circuit because the volume is low
            // Time.Wait(200);
            Assert.True(WaitForHealthCountToUpdate(key, 250, output), "Health count stream failed to update");

            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");
        }

        internal static HystrixCommandMetrics GetMetrics(HystrixCommandOptions properties)
        {
            return HystrixCommandMetrics.GetInstance(CommandKeyForUnitTest.KEY_ONE, CommandOwnerForUnitTest.OWNER_ONE, ThreadPoolKeyForUnitTest.THREAD_POOL_ONE, properties);
        }

        internal static HystrixCommandMetrics GetMetrics(IHystrixCommandKey commandKey, HystrixCommandOptions properties)
        {
            return HystrixCommandMetrics.GetInstance(commandKey, CommandOwnerForUnitTest.OWNER_ONE, ThreadPoolKeyForUnitTest.THREAD_POOL_ONE, properties);
        }

        private void Init()
        {
            foreach (var metricsInstance in HystrixCommandMetrics.GetInstances())
            {
                metricsInstance.ResetStream();
            }
        }

        public class TestCircuitBreaker : IHystrixCircuitBreaker
        {
            private readonly HystrixCommandMetrics metrics;
            private bool forceShortCircuit = false;

            public TestCircuitBreaker()
            {
                metrics = GetMetrics(HystrixCommandOptionsTest.GetUnitTestOptions());
                forceShortCircuit = false;
            }

            public TestCircuitBreaker(IHystrixCommandKey commandKey)
            {
                metrics = GetMetrics(commandKey, HystrixCommandOptionsTest.GetUnitTestOptions());
                forceShortCircuit = false;
            }

            public TestCircuitBreaker SetForceShortCircuit(bool value)
            {
                forceShortCircuit = value;
                return this;
            }

            public bool IsOpen
            {
                get
                {
                    // output.WriteLine("metrics : " + metrics.CommandKey.Name + " : " + metrics.Healthcounts);
                    if (forceShortCircuit)
                    {
                        return true;
                    }
                    else
                    {
                        return metrics.Healthcounts.ErrorCount >= 3;
                    }
                }
            }

            public void MarkSuccess()
            {
                // we don't need to do anything since we're going to permanently trip the circuit
            }

            public bool AllowRequest => !IsOpen;
        }

        private class Command : HystrixCommand<bool>
        {
            protected readonly bool shouldFail;
            protected readonly bool shouldFailWithBadRequest;
            protected readonly int latencyToAdd;

            public Command(string commandKey, bool shouldFail, bool shouldFailWithBadRequest, int latencyToAdd, int sleepWindow, int requestVolumeThreshold)
            : base(Options("Command", commandKey, requestVolumeThreshold, sleepWindow))
            {
                this.shouldFail = shouldFail;
                this.shouldFailWithBadRequest = shouldFailWithBadRequest;
                this.latencyToAdd = latencyToAdd;
            }

            protected static HystrixCommandOptions Options(string groupKey, string commandKey, int requestVolumeThreshold, int sleepWindow)
            {
                var opts = HystrixCommandOptionsTest.GetUnitTestOptions();
                opts.GroupKey = HystrixCommandGroupKeyDefault.AsKey(groupKey);
                opts.CommandKey = HystrixCommandKeyDefault.AsKey(commandKey);
                opts.ExecutionTimeoutInMilliseconds = 500;
                opts.CircuitBreakerRequestVolumeThreshold = requestVolumeThreshold;
                opts.CircuitBreakerSleepWindowInMilliseconds = sleepWindow;
                return opts;
            }

            public Command(string commandKey, bool shouldFail, int latencyToAdd)
            : this(commandKey, shouldFail, false, latencyToAdd, 400, 1)
            {
            }

            protected override bool Run()
            {
                Time.WaitUntil(() => { return _token.IsCancellationRequested; }, latencyToAdd);
                _token.ThrowIfCancellationRequested();

                if (shouldFail)
                {
                    throw new Exception("induced failure");
                }

                if (shouldFailWithBadRequest)
                {
                    throw new HystrixBadRequestException("bad request");
                }

                return true;
            }

            protected override bool RunFallback()
            {
                return false;
            }
        }

        private class SuccessCommand : Command
        {
            public SuccessCommand(string commandKey, int latencyToAdd)
                : base(commandKey, false, latencyToAdd)
            {
            }

            public SuccessCommand(string commandKey, int latencyToAdd, int sleepWindow)
                : base(commandKey, false, false, latencyToAdd, sleepWindow, 1)
            {
            }
        }

        private class FailureCommand : Command
        {
            public FailureCommand(string commandKey, int latencyToAdd)
                : base(commandKey, true, latencyToAdd)
            {
            }

            public FailureCommand(string commandKey, int latencyToAdd, int sleepWindow)
                : base(commandKey, true, false, latencyToAdd, sleepWindow, 1)
            {
            }

            public FailureCommand(string commandKey, int latencyToAdd, int sleepWindow, int requestVolumeThreshold)
                : base(commandKey, true, false, latencyToAdd, sleepWindow, requestVolumeThreshold)
            {
            }
        }

        private class TimeoutCommand : Command
        {
            public TimeoutCommand(string commandKey)
                : base(commandKey, false, 2000)
            {
            }

            public TimeoutCommand(string commandKey, int sleepWindow)
                : base(commandKey, false, false, 2000, sleepWindow, 1)
            {
            }
        }

        private class BadRequestCommand : Command
        {
            public BadRequestCommand(string commandKey, int latencyToAdd)
                : base(commandKey, false, true, latencyToAdd, 400, 1)
            {
            }

            public BadRequestCommand(string commandKey, int latencyToAdd, int sleepWindow)
                : base(commandKey, false, true, latencyToAdd, sleepWindow, 1)
            {
            }
        }

        private class MyHystrixCommandExecutionHook : HystrixCommandExecutionHook
        {
            public override T OnEmit<T>(IHystrixInvokable command, T response)
            {
                LogHC(command, response);
                return base.OnEmit(command, response);
            }

            private void LogHC<T>(IHystrixInvokable command, T response)
            {
                if (command is IHystrixInvokableInfo commandInfo)
                {
                    var metrics = commandInfo.Metrics;

                    Console.WriteLine("cb/error-count/%/total: "
                            + commandInfo.IsCircuitBreakerOpen + " "
                            + metrics.Healthcounts.ErrorCount + " "
                            + metrics.Healthcounts.ErrorPercentage + " "
                            + metrics.Healthcounts.TotalRequests + "  => " + response + "  " + commandInfo.ExecutionEvents);
                }
            }
        }
    }
}