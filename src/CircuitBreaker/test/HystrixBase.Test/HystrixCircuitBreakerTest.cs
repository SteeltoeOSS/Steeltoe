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

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Reactive.Linq;
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
        public async Task TestTripCircuitAsync()
        {
            string key = "cmd-A";

            HystrixCommand<bool> cmd1 = new SuccessCommand(key, 0);
            HystrixCommand<bool> cmd2 = new SuccessCommand(key, 0);
            HystrixCommand<bool> cmd3 = new SuccessCommand(key, 0);
            HystrixCommand<bool> cmd4 = new SuccessCommand(key, 0);

            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            _ = await cmd1.ExecuteAsync();
            _ = await cmd2.ExecuteAsync();
            _ = await cmd3.ExecuteAsync();
            _ = await cmd4.ExecuteAsync();

            // this should still allow requests as everything has been successful
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

            // everything has failed in the test window so we should return false now
            Time.Wait(300);
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");
        }

        [Fact]
        public async Task TestTripCircuitOnFailuresAboveThreshold()
        {
            string key = "cmd-B";

            HystrixCommand<bool> cmd1 = new SuccessCommand(key, 60);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            // this should start as allowing requests
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            // success with high latency
            await cmd1.ExecuteAsync();
            HystrixCommand<bool> cmd2 = new SuccessCommand(key, 1);
            await cmd2.ExecuteAsync();
            HystrixCommand<bool> cmd3 = new FailureCommand(key, 1);
            await cmd3.ExecuteAsync();
            HystrixCommand<bool> cmd4 = new SuccessCommand(key, 1);
            await cmd4.ExecuteAsync();
            HystrixCommand<bool> cmd5 = new FailureCommand(key, 1);
            await cmd5.ExecuteAsync();
            HystrixCommand<bool> cmd6 = new SuccessCommand(key, 1);
            await cmd6.ExecuteAsync();
            HystrixCommand<bool> cmd7 = new FailureCommand(key, 1);
            await cmd7.ExecuteAsync();
            HystrixCommand<bool> cmd8 = new FailureCommand(key, 1);
            await cmd8.ExecuteAsync();

            // this should trip the circuit as the error percentage is above the threshold
            Time.Wait(150);
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");
        }

        [Fact]
        public async Task TestCircuitDoesNotTripOnFailuresBelowThreshold()
        {
            string key = "cmd-C";

            HystrixCommand<bool> cmd1 = new SuccessCommand(key, 60);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            // this should start as allowing requests
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            // success with high latency
            await cmd1.ExecuteAsync();
            HystrixCommand<bool> cmd2 = new SuccessCommand(key, 1);
            await cmd2.ExecuteAsync();
            HystrixCommand<bool> cmd3 = new FailureCommand(key, 1);
            await cmd3.ExecuteAsync();
            HystrixCommand<bool> cmd4 = new SuccessCommand(key, 1);
            await cmd4.ExecuteAsync();
            HystrixCommand<bool> cmd5 = new SuccessCommand(key, 1);
            await cmd5.ExecuteAsync();
            HystrixCommand<bool> cmd6 = new FailureCommand(key, 1);
            await cmd6.ExecuteAsync();
            HystrixCommand<bool> cmd7 = new SuccessCommand(key, 1);
            await cmd7.ExecuteAsync();
            HystrixCommand<bool> cmd8 = new FailureCommand(key, 1);
            await cmd8.ExecuteAsync();

            // this should remain closed as the failure threshold is below the percentage limit
            Time.Wait(150);
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            output.WriteLine("Current CircuitBreaker Status : " + cmd1.Metrics.Healthcounts);
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");
        }

        [Fact]
        public async Task TestTripCircuitOnTimeouts()
        {
            string key = "cmd-D";

            HystrixCommand<bool> cmd1 = new TimeoutCommand(key);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

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

            // everything has been a timeout so we should not allow any requests
            Time.Wait(10);
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");
        }

        [Fact]
        public async Task TestTripCircuitOnTimeoutsAboveThreshold()
        {
            string key = "cmd-E";

            HystrixCommand<bool> cmd1 = new SuccessCommand(key, 60);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            // this should start as allowing requests
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            // success with high latency
            await cmd1.ExecuteAsync();
            HystrixCommand<bool> cmd2 = new SuccessCommand(key, 1);
            await cmd2.ExecuteAsync();
            HystrixCommand<bool> cmd3 = new TimeoutCommand(key);
            await cmd3.ExecuteAsync();
            HystrixCommand<bool> cmd4 = new SuccessCommand(key, 1);
            await cmd4.ExecuteAsync();
            HystrixCommand<bool> cmd5 = new TimeoutCommand(key);
            await cmd5.ExecuteAsync();
            HystrixCommand<bool> cmd6 = new TimeoutCommand(key);
            await cmd6.ExecuteAsync();
            HystrixCommand<bool> cmd7 = new SuccessCommand(key, 1);
            await cmd7.ExecuteAsync();
            HystrixCommand<bool> cmd8 = new TimeoutCommand(key);
            await cmd8.ExecuteAsync();
            HystrixCommand<bool> cmd9 = new TimeoutCommand(key);
            await cmd9.ExecuteAsync();

            // this should trip the circuit as the error percentage is above the threshold
            Time.Wait(150);
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");
        }

        [Fact]
        public async Task TestSingleTestOnOpenCircuitAfterTimeWindow()
        {
            string key = "cmd-F";

            HystrixCommand<bool> cmd1 = new FailureCommand(key, 50);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            // this should start as allowing requests
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected! (1)");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            await cmd1.ExecuteAsync();
            HystrixCommand<bool> cmd2 = new FailureCommand(key, 1);
            await cmd2.ExecuteAsync();
            HystrixCommand<bool> cmd3 = new FailureCommand(key, 1);
            await cmd3.ExecuteAsync();
            HystrixCommand<bool> cmd4 = new FailureCommand(key, 1);
            await cmd4.ExecuteAsync();

            // everything has failed in the test window so we should return false now
            Time.Wait(100);
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected! (1)");
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open! (1)");

            // wait for sleepWindow to pass
            Time.Wait(250);

            // we should now allow 1 request
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected! (2)");

            // but the circuit should still be open
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open! (2)");

            // and further requests are still blocked
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected! (2)");
        }

        [Fact]
        public async Task TestCircuitClosedAfterSuccess()
        {
            string key = "cmd-G";

            int sleepWindow = 100;
            HystrixCommand<bool> cmd1 = new FailureCommand(key, 1, sleepWindow);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            // this should start as allowing requests
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            await cmd1.ExecuteAsync();
            HystrixCommand<bool> cmd2 = new FailureCommand(key, 1, sleepWindow);
            await cmd2.ExecuteAsync();
            HystrixCommand<bool> cmd3 = new FailureCommand(key, 1, sleepWindow);
            await cmd3.ExecuteAsync();
            HystrixCommand<bool> cmd4 = new TimeoutCommand(key, sleepWindow);
            await cmd4.ExecuteAsync();

            // everything has failed in the test window so we should return false now
            Time.Wait(150);
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            output.WriteLine("CircuitBreaker state 1 : " + cmd1.Metrics.Healthcounts);
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");

            // wait for sleepWindow to pass
            Time.Wait(sleepWindow + 50);

            // but the circuit should still be open
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");

            // we should now allow 1 request, and upon success, should cause the circuit to be closed
            HystrixCommand<bool> cmd5 = new SuccessCommand(key, 60, sleepWindow);
            IObservable<bool> asyncResult = cmd5.Observe();

            // and further requests are still blocked while the singleTest command is in flight
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");

            await asyncResult.SingleAsync();

            // all requests should be open again
            Time.Wait(150);
            output.WriteLine("CircuitBreaker state 2 : " + cmd1.Metrics.Healthcounts);
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected (1)!");
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected (2)!");
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected (3)!");

            // and the circuit should be closed again
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");
        }

        [Fact]
        public async Task TestMultipleTimeWindowRetriesBeforeClosingCircuit()
        {
            string key = "cmd-H";

            int sleepWindow = 200;
            HystrixCommand<bool> cmd1 = new FailureCommand(key, 60);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            // this should start as allowing requests
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            await cmd1.ExecuteAsync();
            HystrixCommand<bool> cmd2 = new FailureCommand(key, 1);
            await cmd2.ExecuteAsync();
            HystrixCommand<bool> cmd3 = new FailureCommand(key, 1);
            await cmd3.ExecuteAsync();
            HystrixCommand<bool> cmd4 = new TimeoutCommand(key);
            await cmd4.ExecuteAsync();

            // everything has failed in the test window so we should return false now
            output.WriteLine("!!!! 1 4 failures, circuit will open on recalc");
            Time.Wait(150);

            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");

            // wait for sleepWindow to pass
            output.WriteLine("!!!! 2 Sleep window starting where all commands fail-fast");
            Time.Wait(sleepWindow + 50);
            output.WriteLine("!!!! 3 Sleep window over, should allow singleTest()");

            // but the circuit should still be open
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");

            // we should now allow 1 request, and upon failure, should not affect the circuit breaker, which should remain open
            HystrixCommand<bool> cmd5 = new FailureCommand(key, 60);
            IObservable<bool> asyncResult5 = cmd5.Observe();
            output.WriteLine("!!!! Kicked off the single-test");

            // and further requests are still blocked while the singleTest command is in flight
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");
            output.WriteLine("!!!! Confirmed that no other requests go out during single-test");

            await asyncResult5.SingleAsync();
            output.WriteLine("!!!! SingleTest just completed");

            // all requests should still be blocked, because the singleTest failed
            Assert.False(cb.AllowRequest, "Request allowed (1) when NOT expected!");
            Assert.False(cb.AllowRequest, "Request allowed (2) when NOT expected!");
            Assert.False(cb.AllowRequest, "Request allowed (3) when NOT expected!");

            // wait for sleepWindow to pass
            output.WriteLine("!!!! 2nd sleep window START");
            Time.Wait(sleepWindow + 50);
            output.WriteLine("!!!! 2nd sleep window over");

            // we should now allow 1 request, and upon failure, should not affect the circuit breaker, which should remain open
            HystrixCommand<bool> cmd6 = new FailureCommand(key, 60);
            IObservable<bool> asyncResult6 = cmd6.Observe();
            output.WriteLine("2nd singleTest just kicked off");

            // and further requests are still blocked while the singleTest command is in flight
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");
            output.WriteLine("confirmed that 2nd singletest only happened once");

            await asyncResult6.SingleAsync();
            output.WriteLine("2nd singleTest now over");

            // all requests should still be blocked, because the singleTest failed
            Assert.False(cb.AllowRequest, "Request allowed (1) when NOT expected!");
            Assert.False(cb.AllowRequest, "Request allowed (2) when NOT expected!");
            Assert.False(cb.AllowRequest, "Request allowed (3) when NOT expected!");

            // wait for sleepWindow to pass
            Time.Wait(sleepWindow + 50);

            // but the circuit should still be open
            Assert.True(cb.IsOpen, "Circuit is closed when it should be open!");

            // we should now allow 1 request, and upon success, should cause the circuit to be closed
            HystrixCommand<bool> cmd7 = new SuccessCommand(key, 60);
            IObservable<bool> asyncResult7 = cmd7.Observe();

            // and further requests are still blocked while the singleTest command is in flight
            Assert.False(cb.AllowRequest, "Request allowed when NOT expected!");

            await asyncResult7.SingleAsync();

            // all requests should be open again
            Assert.True(cb.AllowRequest, "Request NOT allowed (1) when expected!");
            Assert.True(cb.AllowRequest, "Request NOT allowed (2) when expected!");
            Assert.True(cb.AllowRequest, "Request NOT allowed (3) when expected!");

            // and the circuit should be closed again
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            // and the circuit should be closed again
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");
        }

        [Fact]
        public async Task TestLowVolumeDoesNotTripCircuit()
        {
            string key = "cmd-I";

            int sleepWindow = 200;
            int lowVolume = 5;

            HystrixCommand<bool> cmd1 = new FailureCommand(key, 60, sleepWindow, lowVolume);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            // this should start as allowing requests
            Assert.True(cb.AllowRequest, "Request NOT allowed when expected!");
            Assert.False(cb.IsOpen, "Circuit breaker is open when it should be closed!");

            await cmd1.ExecuteAsync();
            HystrixCommand<bool> cmd2 = new FailureCommand(key, 1, sleepWindow, lowVolume);
            await cmd2.ExecuteAsync();
            HystrixCommand<bool> cmd3 = new FailureCommand(key, 1, sleepWindow, lowVolume);
            await cmd3.ExecuteAsync();
            HystrixCommand<bool> cmd4 = new FailureCommand(key, 1, sleepWindow, lowVolume);
            await cmd4.ExecuteAsync();

            // even though it has all failed we won't trip the circuit because the volume is low
            Time.Wait(150);
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
            foreach (HystrixCommandMetrics metricsInstance in HystrixCommandMetrics.GetInstances())
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
            : this(commandKey, shouldFail, false, latencyToAdd, 200, 1)
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
                : base(commandKey, false, true, latencyToAdd, 200, 1)
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