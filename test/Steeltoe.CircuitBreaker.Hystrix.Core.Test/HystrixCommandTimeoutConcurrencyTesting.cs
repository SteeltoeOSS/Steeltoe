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

using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixCommandTimeoutConcurrencyTesting : HystrixTestBase
    {
        private const int NUM_CONCURRENT_COMMANDS = 30;
        ITestOutputHelper output;

        public HystrixCommandTimeoutConcurrencyTesting(ITestOutputHelper output) : base()
        {
            this.output = output;
        }

        [Fact]
        public async void TestTimeoutRace()
        {
            int NUM_TRIALS = 10;

            for (int i = 0; i < NUM_TRIALS; i++) {
                List<IObservable<string>> observables = new List<IObservable<string>>();
                HystrixRequestContext context = null;

                try {
                    context = HystrixRequestContext.InitializeContext();
                    for (int j = 0; j < NUM_CONCURRENT_COMMANDS; j++) {
                        observables.Add(new TestCommand().Observe());
                    }

                    IObservable<string> overall = Observable.Merge(observables);

                    IList<String> results = await overall.ToList().FirstAsync(); //wait for all commands to complete

                    foreach (String s in results) {
                        if (s == null) {
                            output.WriteLine("Received NULL!");
                            throw new Exception("Received NULL");
                        }
                    }

                    foreach (IHystrixInvokableInfo hi in HystrixRequestLog.CurrentRequestLog.AllExecutedCommands) {
                        if (!hi.IsResponseTimedOut) {
                            output.WriteLine("Timeout not found in executed command");
                            throw new Exception("Timeout not found in executed command");
                        }
                        if (hi.IsResponseTimedOut && hi.ExecutionEvents.Count == 1) {
                            output.WriteLine("Missing fallback status!");
                            throw new Exception("Missing fallback status on timeout.");
                        }
                    }

                } catch (Exception e) {
                    output.WriteLine("Error: " + e.Message);
                    output.WriteLine(e.ToString());
                    throw ;
                } finally {
                    output.WriteLine(HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
                    if (context != null) {
                        context.Dispose();
                    }
                }

                output.WriteLine("*************** TRIAL " + i + " ******************");
                output.WriteLine(" ");
                Time.Wait( 50);
            }

            base.Reset();
        }

        class TestCommand : HystrixCommand<string>
        {

            public TestCommand()
            : base(GetOptions(), GetThreadPoolOptions())
            {
                this.IsFallbackUserDefined = true;
            }
            private static IHystrixCommandOptions GetOptions()
            {
                HystrixCommandOptions opts = new HystrixCommandOptions()
                {
                    GroupKey = HystrixCommandGroupKeyDefault.AsKey("testTimeoutConcurrency"),
                    CommandKey = HystrixCommandKeyDefault.AsKey("testTimeoutConcurrencyCommand"),
                    ExecutionTimeoutInMilliseconds = 3,
                    CircuitBreakerEnabled = false,
                    FallbackIsolationSemaphoreMaxConcurrentRequests = NUM_CONCURRENT_COMMANDS
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
            protected override string Run()
            {
                //System.out.println(System.currentTimeMillis() + " : " + Thread.currentThread().getName() + " sleeping");
                Time.Wait( 500);
                //System.out.println(System.currentTimeMillis() + " : " + Thread.currentThread().getName() + " awake and returning");
                return "hello";
            }

            //@Override
            protected override String RunFallback()
            {
                return "failed";
            }

        }
    }
}

