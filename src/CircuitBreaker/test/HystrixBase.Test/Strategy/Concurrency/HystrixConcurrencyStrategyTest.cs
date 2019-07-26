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
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency.Test
{
    public class HystrixConcurrencyStrategyTest : HystrixTestBase
    {
        private ITestOutputHelper output;

        public HystrixConcurrencyStrategyTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        // If the RequestContext does not get transferred across threads correctly this blows up.
        // No specific assertions are necessary.
        [Fact]
        public void TestRequestContextPropagatesAcrossObserveOnPool()
        {
            var s1 = new SimpleCommand(output).Execute();
            var s2 = new SimpleCommand(output).Observe().Map((s) =>
            {
                output.WriteLine("Map => Commands: " + HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
                return s;
            }).ForEachAsync((s) =>
            {
                output.WriteLine("Result [" + s + "] => Commands: " + HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
            });
        }

        [Fact]
        public async Task TestThreadContextOnTimeout()
        {
            AtomicBoolean isInitialized = new AtomicBoolean();
            await Assert.ThrowsAsync<HystrixRuntimeException>(async () =>
          {
              await new TimeoutCommand(output).ToObservable()
                .Do(
                (n) =>
                {
                    output.WriteLine("OnNext = " + n);
                }, (e) =>
                {
                    output.WriteLine("OnError = " + HystrixRequestContext.IsCurrentThreadInitialized);
                    isInitialized.Value = HystrixRequestContext.IsCurrentThreadInitialized;
                }).SingleAsync();
          });

            output.WriteLine("initialized = " + HystrixRequestContext.IsCurrentThreadInitialized);
            output.WriteLine("initialized inside onError = " + isInitialized.Value);
            Assert.True(isInitialized.Value);
        }

        [Fact]
        public void TestNoRequestContextOnSimpleConcurencyStrategyWithoutException()
        {
            Dispose();
            var opts = new HystrixCommandOptions()
            {
                RequestLogEnabled = false,
                GroupKey = HystrixCommandGroupKeyDefault.AsKey("SimpleCommand")
            };
            new SimpleCommand(output, opts).Execute();

            Assert.True(true, "Nothing blew up");
        }

        private class SimpleCommand : HystrixCommand<string>
        {
            private ITestOutputHelper output;

            public SimpleCommand(ITestOutputHelper output, IHystrixCommandOptions opts)
                : base(opts)
            {
                this.output = output;
            }

            public SimpleCommand(ITestOutputHelper output)
                : base(HystrixCommandGroupKeyDefault.AsKey("SimpleCommand")) => this.output = output;

            protected override string Run()
            {
                if (HystrixRequestContext.IsCurrentThreadInitialized)
                {
                    output.WriteLine("Executing => Commands: " + HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
                }

                return "Hello";
            }
        }

        private class TimeoutCommand : HystrixCommand
        {
            private ITestOutputHelper output;

            private static IHystrixCommandOptions GetCommandOptions()
            {
                var opts = new HystrixCommandOptions()
                {
                    GroupKey = HystrixCommandGroupKeyDefault.AsKey("TimeoutTest"),
                    ExecutionTimeoutInMilliseconds = 50
                };
                return opts;
            }

            public TimeoutCommand(ITestOutputHelper output)
                : base(GetCommandOptions())
            {
                this.output = output;
            }

            protected override void Run()
            {
                output.WriteLine("TimeoutCommand - run() start");
                Time.Wait(500);
                output.WriteLine("TimeoutCommand - run() finish");
            }
        }
    }
}
