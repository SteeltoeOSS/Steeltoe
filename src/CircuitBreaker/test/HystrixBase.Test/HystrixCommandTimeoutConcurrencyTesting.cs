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

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.Common.Util;
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
        private readonly ITestOutputHelper output;

        public HystrixCommandTimeoutConcurrencyTesting(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        [Fact]
        public async Task TestTimeoutRace()
        {
            var num_trials = 10;

            for (var i = 0; i < num_trials; i++)
            {
                var observables = new List<IObservable<string>>();
                HystrixRequestContext context = null;

                try
                {
                    context = HystrixRequestContext.InitializeContext();
                    for (var j = 0; j < NUM_CONCURRENT_COMMANDS; j++)
                    {
                        observables.Add(new TestCommand().Observe());
                    }

                    var overall = Observable.Merge(observables);

                    var results = await overall.ToList().FirstAsync(); // wait for all commands to complete

                    foreach (var s in results)
                    {
                        if (s == null)
                        {
                            output.WriteLine("Received NULL!");
                            Assert.True(false, "Received NULL result");
                        }
                    }

                    foreach (var hi in HystrixRequestLog.CurrentRequestLog.AllExecutedCommands)
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
                var opts = new HystrixCommandOptions()
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
                var opts = new HystrixThreadPoolOptions()
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
