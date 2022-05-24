// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixCommandMetricsTest : HystrixTestBase
    {
        private readonly ITestOutputHelper output;

        public HystrixCommandMetricsTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void TestGetErrorPercentage()
        {
            var key = "cmd-metrics-A";

            HystrixCommand<bool> cmd1 = new SuccessCommand(key, 0);
            var metrics = cmd1._metrics;

            Assert.True(WaitForHealthCountToUpdate(key, 1000), "Health count stream took to long");

            cmd1.Execute();
            Assert.True(WaitForHealthCountToUpdate(key, 250), "Health count stream took to long");

            output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(0, metrics.Healthcounts.ErrorPercentage);

            HystrixCommand<bool> cmd2 = new FailureCommand(key, 0);
            cmd2.Execute();
            Assert.True(WaitForHealthCountToUpdate(key, 250), "Health count stream took to long");

            output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(50, metrics.Healthcounts.ErrorPercentage);

            HystrixCommand<bool> cmd3 = new SuccessCommand(key, 0);
            HystrixCommand<bool> cmd4 = new SuccessCommand(key, 0);
            cmd3.Execute();
            cmd4.Execute();
            Assert.True(WaitForHealthCountToUpdate(key, 250), "Health count stream took to long");

            output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(25, metrics.Healthcounts.ErrorPercentage);

            HystrixCommand<bool> cmd5 = new TimeoutCommand(key);
            HystrixCommand<bool> cmd6 = new TimeoutCommand(key);
            cmd5.Execute();
            cmd6.Execute();
            Assert.True(WaitForHealthCountToUpdate(key, 250), "Health count stream took to long");
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
            Assert.True(WaitForHealthCountToUpdate(key, 250), "Health count stream took to long");
            Assert.Equal(30, metrics.Healthcounts.ErrorPercentage);
        }

        [Fact]
        public void TestBadRequestsDoNotAffectErrorPercentage()
        {
            var key = "cmd-metrics-B";

            HystrixCommand<bool> cmd1 = new SuccessCommand(key, 0);
            var metrics = cmd1._metrics;

            Assert.True(WaitForHealthCountToUpdate(key, 1000), "Health count stream took to long");
            cmd1.Execute();
            Assert.True(WaitForHealthCountToUpdate(key, 250), "Health count stream took to long");

            output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(0, metrics.Healthcounts.ErrorPercentage);

            HystrixCommand<bool> cmd2 = new FailureCommand(key, 0);
            cmd2.Execute();
            Assert.True(WaitForHealthCountToUpdate(key, 250), "Health count stream took to long");

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

            Assert.True(WaitForHealthCountToUpdate(key, 250), "Health count stream took to long");

            output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(50, metrics.Healthcounts.ErrorPercentage);

            HystrixCommand<bool> cmd5 = new FailureCommand(key, 0);
            HystrixCommand<bool> cmd6 = new FailureCommand(key, 0);
            cmd5.Execute();
            cmd6.Execute();
            Assert.True(WaitForHealthCountToUpdate(key, 250), "Health count stream took to long");

            output.WriteLine("ReqLog" + "@ " + Time.CurrentTimeMillis + " : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(75, metrics.Healthcounts.ErrorPercentage);
        }

        [Fact]
        public void TestCurrentConcurrentExecutionCount()
        {
            var key = "cmd-metrics-C";

            HystrixCommandMetrics metrics = null;
            var cmdResults = new List<IObservable<bool>>();

            for (var i = 0; i < 8; i++)
            {
                HystrixCommand<bool> cmd = new SuccessCommand(key, 900);
                metrics ??= cmd._metrics;

                var eagerObservable = cmd.Observe();
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

            var latch = new CountdownEvent(1);
            cmdResults.Merge().Subscribe(
                n =>
                {
                },
                e =>
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
            private readonly bool shouldFail;
            private readonly bool shouldFailWithBadRequest;
            private readonly int latencyToAdd;

            public Command(string commandKey, bool shouldFail, bool shouldFailWithBadRequest, int latencyToAdd)
                : base(GetUnitTestSettings(commandKey))
            {
                this.shouldFail = shouldFail;
                this.shouldFailWithBadRequest = shouldFailWithBadRequest;
                this.latencyToAdd = latencyToAdd;
                IsFallbackUserDefined = true;
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
                var opts = HystrixCommandOptionsTest.GetUnitTestOptions();
                opts.GroupKey = HystrixCommandGroupKeyDefault.AsKey("Command");
                opts.CommandKey = HystrixCommandKeyDefault.AsKey(commandKey);
                opts.ExecutionTimeoutInMilliseconds = 1000;
                opts.CircuitBreakerRequestVolumeThreshold = 20;
                return opts;
            }
        }

        private sealed class SuccessCommand : Command
        {
            public SuccessCommand(string commandKey, int latencyToAdd)
                : base(commandKey, false, false, latencyToAdd)
            {
            }
        }

        private sealed class FailureCommand : Command
        {
            public FailureCommand(string commandKey, int latencyToAdd)
                : base(commandKey, true, false, latencyToAdd)
            {
            }
        }

        private sealed class TimeoutCommand : Command
        {
            public TimeoutCommand(string commandKey)
                : base(commandKey, false, false, 2000)
            {
            }
        }

        private sealed class BadRequestCommand : Command
        {
            public BadRequestCommand(string commandKey, int latencyToAdd)
                : base(commandKey, false, true, latencyToAdd)
            {
            }
        }
    }
}
