// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixCommandTimeoutConcurrencyTesting : HystrixTestBase
    {
        private const int NUM_CONCURRENT_COMMANDS = 30;
        private ITestOutputHelper output;

        public HystrixCommandTimeoutConcurrencyTesting(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        [Fact]
        public async Task TestTimeoutRace()
        {
            int num_trials = 10;

            for (int i = 0; i < num_trials; i++)
            {
                List<IObservable<string>> observables = new List<IObservable<string>>();
                HystrixRequestContext context = null;

                try
                {
                    context = HystrixRequestContext.InitializeContext();
                    for (int j = 0; j < NUM_CONCURRENT_COMMANDS; j++)
                    {
                        observables.Add(new TestCommand().Observe());
                    }

                    IObservable<string> overall = Observable.Merge(observables);

                    IList<string> results = await overall.ToList().FirstAsync(); // wait for all commands to complete

                    foreach (string s in results)
                    {
                        if (s == null)
                        {
                            output.WriteLine("Received NULL!");
                            Assert.True(false, "Received NULL result");
                        }
                    }

                    foreach (IHystrixInvokableInfo hi in HystrixRequestLog.CurrentRequestLog.AllExecutedCommands)
                    {
                        if (!hi.IsResponseTimedOut)
                        {
                            output.WriteLine("Timeout not found in executed command");
                            Assert.True(false, "Timeout not found in executed command");
                        }

                        if (hi.IsResponseTimedOut && hi.ExecutionEvents.Count == 1)
                        {
                            output.WriteLine("Missing fallback status!");
                            Assert.True(false, "Missing fallback status on timeout.");
                        }
                    }
                }
                catch (Exception e)
                {
                    output.WriteLine("Error: " + e.Message);
                    output.WriteLine(e.ToString());
                    throw;
                }
                finally
                {
                    output.WriteLine(HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                    if (context != null)
                    {
                        context.Dispose();
                    }
                }

                output.WriteLine("*************** TRIAL " + i + " ******************");
                output.WriteLine(" ");
                Time.Wait(50);
            }

            Reset();
        }

        private class TestCommand : HystrixCommand<string>
        {
            public TestCommand()
            : base(GetOptions())
            {
                this.IsFallbackUserDefined = true;
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
                HystrixCommandOptions opts = new HystrixCommandOptions()
                {
                    GroupKey = HystrixCommandGroupKeyDefault.AsKey("testTimeoutConcurrency"),
                    CommandKey = HystrixCommandKeyDefault.AsKey("testTimeoutConcurrencyCommand"),
                    ExecutionTimeoutInMilliseconds = 3,
                    CircuitBreakerEnabled = false,
                    FallbackIsolationSemaphoreMaxConcurrentRequests = NUM_CONCURRENT_COMMANDS,
                    ThreadPoolOptions = GetThreadPoolOptions()
                };
                return opts;
            }

            private static IHystrixThreadPoolOptions GetThreadPoolOptions()
            {
                HystrixThreadPoolOptions opts = new HystrixThreadPoolOptions()
                {
                    CoreSize = NUM_CONCURRENT_COMMANDS,
                    MaxQueueSize = NUM_CONCURRENT_COMMANDS,
                    QueueSizeRejectionThreshold = NUM_CONCURRENT_COMMANDS
                };
                return opts;
            }
        }
    }
}
