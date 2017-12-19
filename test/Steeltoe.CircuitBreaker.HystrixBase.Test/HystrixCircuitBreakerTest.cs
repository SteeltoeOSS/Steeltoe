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
using Steeltoe.CircuitBreaker.Hystrix.Strategy.ExecutionHook;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Reactive.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixCircuitBreakerTest : HystrixTestBase, IDisposable
    {
        ITestOutputHelper output;
        public HystrixCircuitBreakerTest(ITestOutputHelper output) : base()
        {
            this.output = output;
            Init();
        }

        private void Init()
        {
            foreach (HystrixCommandMetrics metricsInstance in HystrixCommandMetrics.GetInstances())
            {
                metricsInstance.ResetStream();
            }
        }
        /**
          * Test that if all 'marks' are successes during the test window that it does NOT trip the circuit.
          * Test that if all 'marks' are failures during the test window that it trips the circuit.
          */
        [Fact]
        public void TestTripCircuit()
        {
            String key = "cmd-A";
     
            HystrixCommand<bool> cmd1 = new SuccessCommand(key, 1);
            HystrixCommand<bool> cmd2 = new SuccessCommand(key, 1);
            HystrixCommand<bool> cmd3 = new SuccessCommand(key, 1);
            HystrixCommand<bool> cmd4 = new SuccessCommand(key, 1);

            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            cmd1.Execute();
            cmd2.Execute();
            cmd3.Execute();
            cmd4.Execute();

            // this should still allow requests as everything has been successful
            Time.Wait( 150);
            Assert.True(cb.AllowRequest);
            Assert.False(cb.IsOpen);

            // fail
            HystrixCommand<bool> cmd5 = new FailureCommand(key, 1);
            HystrixCommand<bool> cmd6 = new FailureCommand(key, 1);
            HystrixCommand<bool> cmd7 = new FailureCommand(key, 1);
            HystrixCommand<bool> cmd8 = new FailureCommand(key, 1);
            Assert.False(cmd5.Execute());
            Assert.False(cmd6.Execute());
            Assert.False(cmd7.Execute());
            Assert.False(cmd8.Execute());

            // everything has failed in the test window so we should return false now
            Time.Wait( 150);
            Assert.False(cb.AllowRequest);
            Assert.True(cb.IsOpen);
   
        }
        /**
         * Test that if the % of failures is higher than the threshold that the circuit trips.
         */
        [Fact]
        public void TestTripCircuitOnFailuresAboveThreshold()
        {
            String key = "cmd-B";
 
            HystrixCommand<Boolean> cmd1 = new SuccessCommand(key, 60);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            // this should start as allowing requests
            Assert.True(cb.AllowRequest);
            Assert.False(cb.IsOpen);

            // success with high latency
            cmd1.Execute();
            HystrixCommand<Boolean> cmd2 = new SuccessCommand(key, 1);
            cmd2.Execute();
            HystrixCommand<Boolean> cmd3 = new FailureCommand(key, 1);
            cmd3.Execute();
            HystrixCommand<Boolean> cmd4 = new SuccessCommand(key, 1);
            cmd4.Execute();
            HystrixCommand<Boolean> cmd5 = new FailureCommand(key, 1);
            cmd5.Execute();
            HystrixCommand<Boolean> cmd6 = new SuccessCommand(key, 1);
            cmd6.Execute();
            HystrixCommand<Boolean> cmd7 = new FailureCommand(key, 1);
            cmd7.Execute();
            HystrixCommand<Boolean> cmd8 = new FailureCommand(key, 1);
            cmd8.Execute();

            // this should trip the circuit as the error percentage is above the threshold
            Time.Wait( 150);
            Assert.False(cb.AllowRequest);
            Assert.True(cb.IsOpen);
    
        }
        /**
    * Test that if the % of failures is higher than the threshold that the circuit trips.
*/
        [Fact]
        public void TestCircuitDoesNotTripOnFailuresBelowThreshold()
        {
            String key = "cmd-C";
      
            HystrixCommand<Boolean> cmd1 = new SuccessCommand(key, 60);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            // this should start as allowing requests
            Assert.True(cb.AllowRequest);
            Assert.False(cb.IsOpen);

            // success with high latency
            cmd1.Execute();
            HystrixCommand<Boolean> cmd2 = new SuccessCommand(key, 1);
            cmd2.Execute();
            HystrixCommand<Boolean> cmd3 = new FailureCommand(key, 1);
            cmd3.Execute();
            HystrixCommand<Boolean> cmd4 = new SuccessCommand(key, 1);
            cmd4.Execute();
            HystrixCommand<Boolean> cmd5 = new SuccessCommand(key, 1);
            cmd5.Execute();
            HystrixCommand<Boolean> cmd6 = new FailureCommand(key, 1);
            cmd6.Execute();
            HystrixCommand<Boolean> cmd7 = new SuccessCommand(key, 1);
            cmd7.Execute();
            HystrixCommand<Boolean> cmd8 = new FailureCommand(key, 1);
            cmd8.Execute();

            // this should remain closed as the failure threshold is below the percentage limit
            Time.Wait( 150);
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            output.WriteLine("Current CircuitBreaker Status : " + cmd1.Metrics.Healthcounts);
            Assert.True(cb.AllowRequest);
            Assert.False(cb.IsOpen);
    
        }

        /**
         * Test that if all 'marks' are timeouts that it will trip the circuit.
         */
        [Fact]
        public void TestTripCircuitOnTimeouts()
        {
            String key = "cmd-D";
    
            HystrixCommand<Boolean> cmd1 = new TimeoutCommand(key);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            // this should start as allowing requests
            Assert.True(cb.AllowRequest);
            Assert.False(cb.IsOpen);

            // success with high latency
            cmd1.Execute();
            HystrixCommand<Boolean> cmd2 = new TimeoutCommand(key);
            cmd2.Execute();
            HystrixCommand<Boolean> cmd3 = new TimeoutCommand(key);
            cmd3.Execute();
            HystrixCommand<Boolean> cmd4 = new TimeoutCommand(key);
            cmd4.Execute();

            // everything has been a timeout so we should not allow any requests
            Time.Wait( 150);
            Assert.False(cb.AllowRequest);
            Assert.True(cb.IsOpen);

        }

        /**
         * Test that if the % of timeouts is higher than the threshold that the circuit trips.
         */
        [Fact]
        public void TestTripCircuitOnTimeoutsAboveThreshold()
        {
            String key = "cmd-E";
 
            HystrixCommand<Boolean> cmd1 = new SuccessCommand(key, 60);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            // this should start as allowing requests
            Assert.True(cb.AllowRequest);
            Assert.False(cb.IsOpen);

            // success with high latency
            cmd1.Execute();
            HystrixCommand<Boolean> cmd2 = new SuccessCommand(key, 1);
            cmd2.Execute();
            HystrixCommand<Boolean> cmd3 = new TimeoutCommand(key);
            cmd3.Execute();
            HystrixCommand<Boolean> cmd4 = new SuccessCommand(key, 1);
            cmd4.Execute();
            HystrixCommand<Boolean> cmd5 = new TimeoutCommand(key);
            cmd5.Execute();
            HystrixCommand<Boolean> cmd6 = new TimeoutCommand(key);
            cmd6.Execute();
            HystrixCommand<Boolean> cmd7 = new SuccessCommand(key, 1);
            cmd7.Execute();
            HystrixCommand<Boolean> cmd8 = new TimeoutCommand(key);
            cmd8.Execute();
            HystrixCommand<Boolean> cmd9 = new TimeoutCommand(key);
            cmd9.Execute();

            // this should trip the circuit as the error percentage is above the threshold
            Time.Wait( 150);
            Assert.False(cb.AllowRequest);
            Assert.True(cb.IsOpen);
    
        }

        /**
         * Test that on an open circuit that a single attempt will be allowed after a window of time to see if issues are resolved.
         */
        [Fact]
        public void TestSingleTestOnOpenCircuitAfterTimeWindow()
        {
            String key = "cmd-F";
  
            int sleepWindow = 200;
            HystrixCommand<Boolean> cmd1 = new FailureCommand(key, 60);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            // this should start as allowing requests
            Assert.True(cb.AllowRequest);
            Assert.False(cb.IsOpen);

            cmd1.Execute();
            HystrixCommand<Boolean> cmd2 = new FailureCommand(key, 1);
            cmd2.Execute();
            HystrixCommand<Boolean> cmd3 = new FailureCommand(key, 1);
            cmd3.Execute();
            HystrixCommand<Boolean> cmd4 = new FailureCommand(key, 1);
            cmd4.Execute();

            // everything has failed in the test window so we should return false now
            Time.Wait( 150);
            Assert.False(cb.AllowRequest);
            Assert.True(cb.IsOpen);

            // wait for sleepWindow to pass
            Time.Wait( sleepWindow + 50);

            // we should now allow 1 request
            Assert.True(cb.AllowRequest);
            // but the circuit should still be open
            Assert.True(cb.IsOpen);
            // and further requests are still blocked
            Assert.False(cb.AllowRequest);


        }

        /**
         * Test that an open circuit is closed after 1 success.  This also ensures that the rolling window (containing failures) is cleared after the sleep window
         * Otherwise, the next bucket roll would produce another signal to fail unless it is explicitly cleared (via {@link HystrixCommandMetrics#resetStream()}.
         */
        [Fact]
        public async void TestCircuitClosedAfterSuccess()
        {
            String key = "cmd-G";

            int sleepWindow = 100;
            HystrixCommand<Boolean> cmd1 = new FailureCommand(key, 1, sleepWindow);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            // this should start as allowing requests
            Assert.True(cb.AllowRequest);
            Assert.False(cb.IsOpen);

            cmd1.Execute();
            HystrixCommand<Boolean> cmd2 = new FailureCommand(key, 1, sleepWindow);
            cmd2.Execute();
            HystrixCommand<Boolean> cmd3 = new FailureCommand(key, 1, sleepWindow);
            cmd3.Execute();
            HystrixCommand<Boolean> cmd4 = new TimeoutCommand(key, sleepWindow);
            cmd4.Execute();

            // everything has failed in the test window so we should return false now
            Time.Wait( 150);
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            output.WriteLine("CircuitBreaker state 1 : " + cmd1.Metrics.Healthcounts);
            Assert.False(cb.AllowRequest);
            Assert.True(cb.IsOpen);

            // wait for sleepWindow to pass
            Time.Wait( sleepWindow + 50);

            // but the circuit should still be open
            Assert.True(cb.IsOpen);

            // we should now allow 1 request, and upon success, should cause the circuit to be closed
            HystrixCommand<bool> cmd5 = new SuccessCommand(key, 60, sleepWindow);
            IObservable<bool> asyncResult = cmd5.Observe();

            // and further requests are still blocked while the singleTest command is in flight
            Assert.False(cb.AllowRequest);

            await asyncResult.SingleAsync();

            // all requests should be open again

            Time.Wait( 150);
            output.WriteLine("CircuitBreaker state 2 : " + cmd1.Metrics.Healthcounts);
            Assert.True(cb.AllowRequest);
            Assert.True(cb.AllowRequest);
            Assert.True(cb.AllowRequest);
            // and the circuit should be closed again
            Assert.False(cb.IsOpen);

   
        }

        /**
         * Over a period of several 'windows' a single attempt will be made and fail and then finally succeed and close the circuit.
         * <p>
         * Ensure the circuit is kept open through the entire testing period and that only the single attempt in each window is made.
         */
        [Fact]
        public async void TestMultipleTimeWindowRetriesBeforeClosingCircuit()
        {
            String key = "cmd-H";

            int sleepWindow = 200;
            HystrixCommand<Boolean> cmd1 = new FailureCommand(key, 60);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            // this should start as allowing requests
            Assert.True(cb.AllowRequest);
            Assert.False(cb.IsOpen);

            cmd1.Execute();
            HystrixCommand<Boolean> cmd2 = new FailureCommand(key, 1);
            cmd2.Execute();
            HystrixCommand<Boolean> cmd3 = new FailureCommand(key, 1);
            cmd3.Execute();
            HystrixCommand<Boolean> cmd4 = new TimeoutCommand(key);
            cmd4.Execute();

            // everything has failed in the test window so we should return false now
            output.WriteLine("!!!! 1 4 failures, circuit will open on recalc");
            Time.Wait( 150);

            Assert.False(cb.AllowRequest);
            Assert.True(cb.IsOpen);

            // wait for sleepWindow to pass
            output.WriteLine("!!!! 2 Sleep window starting where all commands fail-fast");
            Time.Wait( sleepWindow + 50);
            output.WriteLine("!!!! 3 Sleep window over, should allow singleTest()");

            // but the circuit should still be open
            Assert.True(cb.IsOpen);

            // we should now allow 1 request, and upon failure, should not affect the circuit breaker, which should remain open
            HystrixCommand<bool> cmd5 = new FailureCommand(key, 60);
            IObservable<bool> asyncResult5 = cmd5.Observe();
            output.WriteLine("!!!! Kicked off the single-test");

            // and further requests are still blocked while the singleTest command is in flight
            Assert.False(cb.AllowRequest);
            output.WriteLine("!!!! Confirmed that no other requests go out during single-test");

            await asyncResult5.SingleAsync();
            output.WriteLine("!!!! SingleTest just completed");

            // all requests should still be blocked, because the singleTest failed
            Assert.False(cb.AllowRequest);
            Assert.False(cb.AllowRequest);
            Assert.False(cb.AllowRequest);

            // wait for sleepWindow to pass
            output.WriteLine("!!!! 2nd sleep window START");
            Time.Wait( sleepWindow + 50);
            output.WriteLine("!!!! 2nd sleep window over");

            // we should now allow 1 request, and upon failure, should not affect the circuit breaker, which should remain open
            HystrixCommand<bool> cmd6 = new FailureCommand(key, 60);
            IObservable<bool> asyncResult6 = cmd6.Observe();
            output.WriteLine("2nd singleTest just kicked off");

            //and further requests are still blocked while the singleTest command is in flight
            Assert.False(cb.AllowRequest);
            output.WriteLine("confirmed that 2nd singletest only happened once");

            await asyncResult6.SingleAsync();
            output.WriteLine("2nd singleTest now over");

            // all requests should still be blocked, because the singleTest failed
            Assert.False(cb.AllowRequest);
            Assert.False(cb.AllowRequest);
            Assert.False(cb.AllowRequest);

            // wait for sleepWindow to pass
            Time.Wait( sleepWindow + 50);

            // but the circuit should still be open
            Assert.True(cb.IsOpen);

            // we should now allow 1 request, and upon success, should cause the circuit to be closed
            HystrixCommand<bool> cmd7 = new SuccessCommand(key, 60);
            IObservable<bool> asyncResult7 = cmd7.Observe();

            // and further requests are still blocked while the singleTest command is in flight
            Assert.False(cb.AllowRequest);

            await asyncResult7.SingleAsync();

            // all requests should be open again
            Assert.True(cb.AllowRequest);
            Assert.True(cb.AllowRequest);
            Assert.True(cb.AllowRequest);
            // and the circuit should be closed again
            Assert.False(cb.IsOpen);

            // and the circuit should be closed again
            Assert.False(cb.IsOpen);
   
        }

        /**
         * When volume of reporting during a statistical window is lower than a defined threshold the circuit
         * will not trip regardless of whatever statistics are calculated.
         */
        [Fact]
        public void TestLowVolumeDoesNotTripCircuit()
        {
            String key = "cmd-I";
    
            int sleepWindow = 200;
            int lowVolume = 5;

            HystrixCommand<Boolean> cmd1 = new FailureCommand(key, 60, sleepWindow, lowVolume);
            IHystrixCircuitBreaker cb = cmd1._circuitBreaker;

            // this should start as allowing requests
            Assert.True(cb.AllowRequest);
            Assert.False(cb.IsOpen);

            cmd1.Execute();
            HystrixCommand<Boolean> cmd2 = new FailureCommand(key, 1, sleepWindow, lowVolume);
            cmd2.Execute();
            HystrixCommand<Boolean> cmd3 = new FailureCommand(key, 1, sleepWindow, lowVolume);
            cmd3.Execute();
            HystrixCommand<Boolean> cmd4 = new FailureCommand(key, 1, sleepWindow, lowVolume);
            cmd4.Execute();

            // even though it has all failed we won't trip the circuit because the volume is low
            Time.Wait( 150);
            Assert.True(cb.AllowRequest);
            Assert.False(cb.IsOpen);

        }



        internal static HystrixCommandMetrics GetMetrics(HystrixCommandOptions properties)
        {
            return HystrixCommandMetrics.GetInstance(CommandKeyForUnitTest.KEY_ONE, CommandOwnerForUnitTest.OWNER_ONE, ThreadPoolKeyForUnitTest.THREAD_POOL_ONE, properties);
        }
        internal static HystrixCommandMetrics GetMetrics(IHystrixCommandKey commandKey, HystrixCommandOptions properties)
        {
            return HystrixCommandMetrics.GetInstance(commandKey, CommandOwnerForUnitTest.OWNER_ONE, ThreadPoolKeyForUnitTest.THREAD_POOL_ONE, properties);
        }

        public class TestCircuitBreaker : IHystrixCircuitBreaker
        {

            readonly HystrixCommandMetrics metrics;
            private bool forceShortCircuit = false;

            public TestCircuitBreaker()
            {
                this.metrics = GetMetrics(HystrixCommandOptionsTest.GetUnitTestOptions());
                forceShortCircuit = false;
            }

            public TestCircuitBreaker(IHystrixCommandKey commandKey)
            {
                this.metrics = GetMetrics(commandKey, HystrixCommandOptionsTest.GetUnitTestOptions());
                forceShortCircuit = false;
            }

            public TestCircuitBreaker SetForceShortCircuit(bool value)
            {
                this.forceShortCircuit = value;
                return this;
            }

            public bool IsOpen
            {
                get
                {
                    //output.WriteLine("metrics : " + metrics.CommandKey.Name + " : " + metrics.Healthcounts);
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


            public bool AllowRequest
            {
                get { return !IsOpen; }
            }

        }

        class Command : HystrixCommand<bool>
        {

            protected readonly bool shouldFail;
            protected readonly bool shouldFailWithBadRequest;
            protected readonly int latencyToAdd;

            public Command(String commandKey, bool shouldFail, bool shouldFailWithBadRequest, int latencyToAdd, int sleepWindow, int requestVolumeThreshold)
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
            public Command(String commandKey, bool shouldFail, int latencyToAdd)
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
        class SuccessCommand : Command
        {

            public SuccessCommand(String commandKey, int latencyToAdd) :
                base(commandKey, false, latencyToAdd)
            { }


            public SuccessCommand(String commandKey, int latencyToAdd, int sleepWindow) :
                base(commandKey, false, false, latencyToAdd, sleepWindow, 1)
            { }
        }
        class FailureCommand : Command
        {

            public FailureCommand(String commandKey, int latencyToAdd) :
            base(commandKey, true, latencyToAdd)
            { }


            public FailureCommand(String commandKey, int latencyToAdd, int sleepWindow) :

            base(commandKey, true, false, latencyToAdd, sleepWindow, 1)
            { }


            public FailureCommand(String commandKey, int latencyToAdd, int sleepWindow, int requestVolumeThreshold) :

            base(commandKey, true, false, latencyToAdd, sleepWindow, requestVolumeThreshold)
            { }

        }
        class TimeoutCommand : Command
        {

            public TimeoutCommand(String commandKey) :
                base(commandKey, false, 2000)
            { }


            public TimeoutCommand(String commandKey, int sleepWindow) :
            base(commandKey, false, false, 2000, sleepWindow, 1)
            { }

        }
        class BadRequestCommand : Command
        {
            public BadRequestCommand(String commandKey, int latencyToAdd) :
            base(commandKey, false, true, latencyToAdd, 200, 1)
            { }


            public BadRequestCommand(String commandKey, int latencyToAdd, int sleepWindow) :
            base(commandKey, false, true, latencyToAdd, sleepWindow, 1)
            { }

        }

        class MyHystrixCommandExecutionHook : HystrixCommandExecutionHook
        {


            public override T OnEmit<T>(IHystrixInvokable command, T response)
            {
                LogHC(command, response);
                return base.OnEmit<T>(command, response);
            }


            private void LogHC<T>(IHystrixInvokable command, T response)
            {
                if (command is IHystrixInvokableInfo)
                {
                    IHystrixInvokableInfo commandInfo = (IHystrixInvokableInfo)command;
                    HystrixCommandMetrics metrics = commandInfo.Metrics;
                    //output.WriteLine("cb/error-count/%/total: "
                    //        + commandInfo.IsCircuitBreakerOpen + " "
                    //        + metrics.Healthcounts.ErrorCount + " "
                    //        + metrics.Healthcounts.ErrorPercentage + " "
                    //        + metrics.Healthcounts.TotalRequests + "  => " + response + "  " + commandInfo.ExecutionEvents);
                }
            }
        }
    }
}


