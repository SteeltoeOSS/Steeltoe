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
using Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixCommandMetricsTest : HystrixTestBase, IDisposable
    {
        private ITestOutputHelper output;

        public HystrixCommandMetricsTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestGetErrorPercentage()
        {
            string key = "cmd-metrics-A";

            HystrixCommand<bool> cmd1 = new SuccessCommand(key, 0);
            HystrixCommandMetrics metrics = cmd1._metrics;
            var stream = HealthCountsStream.GetInstance(HystrixCommandKeyDefault.AsKey(key), cmd1.CommandOptions);
            bool streamStarted = false;

            using (stream.Observe().Subscribe((healthCounts) =>
            {
                streamStarted = true;
                output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + healthCounts);
                output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            }))
            {
                Assert.True(Time.WaitUntil(() => streamStarted, 1000), "Stream failed to start");
                cmd1.Execute();
                Time.Wait(125);

                output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                Assert.Equal(0, metrics.Healthcounts.ErrorPercentage);

                HystrixCommand<bool> cmd2 = new FailureCommand(key, 0);
                cmd2.Execute();
                Time.Wait(125);

                output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                Assert.Equal(50, metrics.Healthcounts.ErrorPercentage);

                HystrixCommand<bool> cmd3 = new SuccessCommand(key, 0);
                HystrixCommand<bool> cmd4 = new SuccessCommand(key, 0);
                cmd3.Execute();
                cmd4.Execute();
                Time.Wait(125);

                output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                Assert.Equal(25, metrics.Healthcounts.ErrorPercentage);

                HystrixCommand<bool> cmd5 = new TimeoutCommand(key);
                HystrixCommand<bool> cmd6 = new TimeoutCommand(key);
                cmd5.Execute();
                cmd6.Execute();
                Time.Wait(125);
                output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                Assert.Equal(50, metrics.Healthcounts.ErrorPercentage);

                HystrixCommand<bool> cmd7 = new SuccessCommand(key, 0);
                HystrixCommand<bool> cmd8 = new SuccessCommand(key, 0);
                HystrixCommand<bool> cmd9 = new SuccessCommand(key, 0);
                cmd7.Execute();
                cmd8.Execute();
                cmd9.Execute();

                output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

                // latent
                HystrixCommand<bool> cmd10 = new SuccessCommand(key, 60);
                cmd10.Execute();

                output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

                // 6 success + 1 latent success + 1 failure + 2 timeout = 10 total
                // latent success not considered error
                // error percentage = 1 failure + 2 timeout / 10
                Time.Wait(125);
                Assert.Equal(30, metrics.Healthcounts.ErrorPercentage);
            }
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestBadRequestsDoNotAffectErrorPercentage()
        {
            string key = "cmd-metrics-B";

            HystrixCommand<bool> cmd1 = new SuccessCommand(key, 0);
            HystrixCommandMetrics metrics = cmd1._metrics;
            var stream = HealthCountsStream.GetInstance(HystrixCommandKeyDefault.AsKey(key), cmd1.CommandOptions);
            bool streamStarted = false;
            using (stream.Observe().Subscribe((healthCounts) =>
            {
                streamStarted = true;
                output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + healthCounts);
                output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            }))
            {
                Assert.True(Time.WaitUntil(() => streamStarted, 1000), "Stream failed to start");
                cmd1.Execute();
                Time.Wait(125);

                output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                Assert.Equal(0, metrics.Healthcounts.ErrorPercentage);

                HystrixCommand<bool> cmd2 = new FailureCommand(key, 0);
                cmd2.Execute();
                Time.Wait(125);

                output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                Assert.Equal(50, metrics.Healthcounts.ErrorPercentage);

                HystrixCommand<bool> cmd3 = new BadRequestCommand(key, 0);
                HystrixCommand<bool> cmd4 = new BadRequestCommand(key, 0);
                try
                {
                    cmd3.Execute();
                }
                catch (HystrixBadRequestException)
                {
                    output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + "Caught expected HystrixBadRequestException from cmd3");
                }

                try
                {
                    cmd4.Execute();
                }
                catch (HystrixBadRequestException)
                {
                    output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + "Caught expected HystrixBadRequestException from cmd4");
                }

                Time.Wait(125);

                output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                Assert.Equal(50, metrics.Healthcounts.ErrorPercentage);

                HystrixCommand<bool> cmd5 = new FailureCommand(key, 0);
                HystrixCommand<bool> cmd6 = new FailureCommand(key, 0);
                cmd5.Execute();
                cmd6.Execute();
                Time.Wait(125);

                output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                Assert.Equal(75, metrics.Healthcounts.ErrorPercentage);
            }
        }

        [Fact]
        public void TestCurrentConcurrentExecutionCount()
        {
            string key = "cmd-metrics-C";

            HystrixCommandMetrics metrics = null;
            List<IObservable<bool>> cmdResults = new List<IObservable<bool>>();

            for (int i = 0; i < 8; i++)
            {
                HystrixCommand<bool> cmd = new SuccessCommand(key, 900);
                if (metrics == null)
                {
                    metrics = cmd._metrics;
                }

                IObservable<bool> eagerObservable = cmd.Observe();
                cmdResults.Add(eagerObservable);
            }

            try
            {
                Time.Wait(150);
            }
            catch (Exception ie)
            {
                Assert.True(false, ie.Message);
            }

            output.WriteLine("ReqLog: " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(8, metrics.CurrentConcurrentExecutionCount);

            CountdownEvent latch = new CountdownEvent(1);
            Observable.Merge(cmdResults).Subscribe(
                (n) =>
                {
                },
                (e) =>
                {
                    output.WriteLine("Error duing command execution");
                    output.WriteLine(e.ToString());
                    latch.SignalEx();
                },
                () =>
                {
                    output.WriteLine("All commands done");
                    latch.SignalEx();
                });

            latch.Wait(10000);
            Assert.Equal(0, metrics.CurrentConcurrentExecutionCount);
        }

        private class Command : HystrixCommand<bool>
        {
            private bool shouldFail;
            private bool shouldFailWithBadRequest;
            private int latencyToAdd;

            public Command(string commandKey, bool shouldFail, bool shouldFailWithBadRequest, int latencyToAdd)
                : base(GetUnitTestSettings(commandKey))
            {
                this.shouldFail = shouldFail;
                this.shouldFailWithBadRequest = shouldFailWithBadRequest;
                this.latencyToAdd = latencyToAdd;
                this.IsFallbackUserDefined = true;
            }

            protected override bool Run()
            {
                Time.Wait(latencyToAdd);

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

            private static HystrixCommandOptions GetUnitTestSettings(string commandKey)
            {
                HystrixCommandOptions opts = HystrixCommandOptionsTest.GetUnitTestOptions();
                opts.GroupKey = HystrixCommandGroupKeyDefault.AsKey("Command");
                opts.CommandKey = HystrixCommandKeyDefault.AsKey(commandKey);
                opts.ExecutionTimeoutInMilliseconds = 1000;
                opts.CircuitBreakerRequestVolumeThreshold = 20;
                return opts;
            }
        }

        private class SuccessCommand : Command
        {
            public SuccessCommand(string commandKey, int latencyToAdd)
                : base(commandKey, false, false, latencyToAdd)
            {
            }
        }

        private class FailureCommand : Command
        {
            public FailureCommand(string commandKey, int latencyToAdd)
                : base(commandKey, true, false, latencyToAdd)
            {
            }
        }

        private class TimeoutCommand : Command
        {
            public TimeoutCommand(string commandKey)
                : base(commandKey, false, false, 2000)
            {
            }
        }

        private class BadRequestCommand : Command
        {
            public BadRequestCommand(string commandKey, int latencyToAdd)
                : base(commandKey, false, true, latencyToAdd)
            {
            }
        }
    }
}
