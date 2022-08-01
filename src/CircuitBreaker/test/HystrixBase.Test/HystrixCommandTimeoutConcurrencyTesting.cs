// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.Common.Util;
using System.Reactive.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public class HystrixCommandTimeoutConcurrencyTesting : HystrixTestBase
{
    private const int NumConcurrentCommands = 30;
    private readonly ITestOutputHelper _output;

    public HystrixCommandTimeoutConcurrencyTesting(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestTimeoutRace()
    {
        var numTrials = 10;

        for (var i = 0; i < numTrials; i++)
        {
            var observables = new List<IObservable<string>>();
            HystrixRequestContext context = null;

            try
            {
                context = HystrixRequestContext.InitializeContext();
                for (var j = 0; j < NumConcurrentCommands; j++)
                {
                    observables.Add(new TestCommand().Observe());
                }

                var overall = observables.Merge();

                var results = await overall.ToList().FirstAsync(); // wait for all commands to complete

                foreach (var s in results)
                {
                    if (s == null)
                    {
                        _output.WriteLine("Received NULL!");
                        Assert.True(false, "Received NULL result");
                    }
                }

                foreach (var hi in HystrixRequestLog.CurrentRequestLog.AllExecutedCommands)
                {
                    if (!hi.IsResponseTimedOut)
                    {
                        _output.WriteLine("Timeout not found in executed command");
                        Assert.True(false, "Timeout not found in executed command");
                    }

                    if (hi.IsResponseTimedOut && hi.ExecutionEvents.Count == 1)
                    {
                        _output.WriteLine("Missing fallback status!");
                        Assert.True(false, "Missing fallback status on timeout.");
                    }
                }
            }
            catch (Exception e)
            {
                _output.WriteLine("Error: " + e.Message);
                _output.WriteLine(e.ToString());
                throw;
            }
            finally
            {
                _output.WriteLine(HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                if (context != null)
                {
                    context.Dispose();
                }
            }

            _output.WriteLine("*************** TRIAL " + i + " ******************");
            _output.WriteLine(" ");
            Time.Wait(50);
        }

        Reset();
    }

    private sealed class TestCommand : HystrixCommand<string>
    {
        public TestCommand()
            : base(GetOptions())
        {
            IsFallbackUserDefined = true;
        }

        protected override string Run()
        {
            Time.Wait(500);
            return "hello";
        }

        protected override string RunFallback()
        {
            return "failed";
        }

        private static IHystrixCommandOptions GetOptions()
        {
            var opts = new HystrixCommandOptions
            {
                GroupKey = HystrixCommandGroupKeyDefault.AsKey("testTimeoutConcurrency"),
                CommandKey = HystrixCommandKeyDefault.AsKey("testTimeoutConcurrencyCommand"),
                ExecutionTimeoutInMilliseconds = 3,
                CircuitBreakerEnabled = false,
                FallbackIsolationSemaphoreMaxConcurrentRequests = NumConcurrentCommands,
                ThreadPoolOptions = GetThreadPoolOptions()
            };
            return opts;
        }

        private static IHystrixThreadPoolOptions GetThreadPoolOptions()
        {
            var opts = new HystrixThreadPoolOptions
            {
                CoreSize = NumConcurrentCommands,
                MaxQueueSize = NumConcurrentCommands,
                QueueSizeRejectionThreshold = NumConcurrentCommands
            };
            return opts;
        }
    }
}
