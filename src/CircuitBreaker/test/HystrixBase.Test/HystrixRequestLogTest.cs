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

using Steeltoe.CircuitBreaker.Hystrix.Util;
using System;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixRequestLogTest : HystrixTestBase
    {
        private const string DIGITS_REGEX = "\\[\\d+";

        [Fact]
        public void TestSuccess()
        {
            new TestCommand("A", false, true).Execute();
            string log = HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString();

            // strip the actual count so we can compare reliably
            log = Regex.Replace(log, DIGITS_REGEX, "[");
            Assert.Equal("TestCommand[SUCCESS][ms]", log);
        }

        [Fact]
        public void TestSuccessFromCache()
        {
            // 1 success
            new TestCommand("A", false, true).Execute();

            // 4 success from cache
            new TestCommand("A", false, true).Execute();
            new TestCommand("A", false, true).Execute();
            new TestCommand("A", false, true).Execute();
            new TestCommand("A", false, true).Execute();
            string log = HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString();

            // strip the actual count so we can compare reliably
            log = Regex.Replace(log, DIGITS_REGEX, "[");
            Assert.Equal("TestCommand[SUCCESS][ms], TestCommand[SUCCESS, RESPONSE_FROM_CACHE][ms]x4", log);
        }

        [Fact]
        public void TestFailWithFallbackSuccess()
        {
            // 1 failure
            new TestCommand("A", true, false).Execute();

            // 4 failures from cache
            new TestCommand("A", true, false).Execute();
            new TestCommand("A", true, false).Execute();
            new TestCommand("A", true, false).Execute();
            new TestCommand("A", true, false).Execute();
            string log = HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString();

            // strip the actual count so we can compare reliably
            log = Regex.Replace(log, DIGITS_REGEX, "[");
            Assert.Equal("TestCommand[FAILURE, FALLBACK_SUCCESS][ms], TestCommand[FAILURE, FALLBACK_SUCCESS, RESPONSE_FROM_CACHE][ms]x4", log);
        }

        [Fact]
        public void TestFailWithFallbackFailure()
        {
            // 1 failure
            try
            {
                new TestCommand("A", true, true).Execute();
            }
            catch (Exception)
            {
            }

            // 1 failure from cache
            try
            {
                new TestCommand("A", true, true).Execute();
            }
            catch (Exception)
            {
            }

            string log = HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString();

            // strip the actual count so we can compare reliably
            log = Regex.Replace(log, DIGITS_REGEX, "[");
            Assert.Equal("TestCommand[FAILURE, FALLBACK_FAILURE][ms], TestCommand[FAILURE, FALLBACK_FAILURE, RESPONSE_FROM_CACHE][ms]", log);
        }

        [Fact]
        public void TestTimeout()
        {
            IObservable<string> result = null;

            // 1 timeout
            try
            {
                for (int i = 0; i < 1; i++)
                {
                    result = new TestCommand("A", false, false, true).Observe();
                }
            }
            catch (Exception)
            {
            }

            try
            {
                result.SingleAsync().Wait();
            }
            catch (Exception)
            {
            }

            // System.out.println(Thread.currentThread().getName() + " : " + System.currentTimeMillis() + " -> done with awaiting all observables");
            string log = HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString();

            // strip the actual count so we can compare reliably
            log = Regex.Replace(log, DIGITS_REGEX, "[");
            Assert.Equal("TestCommand[TIMEOUT, FALLBACK_MISSING][ms]", log);
        }

        [Fact]
        public void TestManyTimeouts()
        {
            for (int i = 0; i < 10; i++)
            {
                TestTimeout();
                Reset();
            }
        }

        [Fact]
        public void TestMultipleCommands()
        {
            // 1 success
            new TestCommand("GetData", "A", false, false).Execute();

            // 1 success
            new TestCommand("PutData", "B", false, false).Execute();

            // 1 success
            new TestCommand("GetValues", "C", false, false).Execute();

            // 1 success from cache
            new TestCommand("GetValues", "C", false, false).Execute();

            // 1 failure
            try
            {
                new TestCommand("A", true, true).Execute();
            }
            catch (Exception)
            {
            }

            // 1 failure from cache
            try
            {
                new TestCommand("A", true, true).Execute();
            }
            catch (Exception)
            {
            }

            string log = HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString();

            // strip the actual count so we can compare reliably
            log = Regex.Replace(log, DIGITS_REGEX, "[");
            Assert.Equal("GetData[SUCCESS][ms], PutData[SUCCESS][ms], GetValues[SUCCESS][ms], GetValues[SUCCESS, RESPONSE_FROM_CACHE][ms], TestCommand[FAILURE, FALLBACK_FAILURE][ms], TestCommand[FAILURE, FALLBACK_FAILURE, RESPONSE_FROM_CACHE][ms]", log);
        }

        [Fact]
        public void TestMaxLimit()
        {
            for (int i = 0; i < HystrixRequestLog.MAX_STORAGE; i++)
            {
                new TestCommand("A", false, true).Execute();
            }

            // then execute again some more
            for (int i = 0; i < 10; i++)
            {
                new TestCommand("A", false, true).Execute();
            }

            Assert.Equal(HystrixRequestLog.MAX_STORAGE, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
        }

        private class TestCommand : HystrixCommand<string>
        {
            private readonly string value;
            private readonly bool fail;
            private readonly bool failOnFallback;
            private readonly bool timeout;
            private readonly bool useFallback;
            private readonly bool useCache;

            public TestCommand(string commandName, string value, bool fail, bool failOnFallback)
                : base(new HystrixCommandOptions()
                {
                    GroupKey = HystrixCommandGroupKeyDefault.AsKey("RequestLogTestCommand"),
                    CommandKey = HystrixCommandKeyDefault.AsKey(commandName)
                })
            {
                this.value = value;
                this.fail = fail;
                this.failOnFallback = failOnFallback;
                this.timeout = false;
                this.useFallback = true;
                this.useCache = true;
            }

            public TestCommand(string value, bool fail, bool failOnFallback)
                : base(HystrixCommandGroupKeyDefault.AsKey("RequestLogTestCommand"))
            {
                this.value = value;
                this.fail = fail;
                this.failOnFallback = failOnFallback;
                this.timeout = false;
                this.useFallback = true;
                this.useCache = true;
            }

            public TestCommand(string value, bool fail, bool failOnFallback, bool timeout)
                : base(new HystrixCommandOptions()
                {
                    GroupKey = HystrixCommandGroupKeyDefault.AsKey("RequestLogTestCommand"),
                    ExecutionTimeoutInMilliseconds = 500
                })
            {
                this.value = value;
                this.fail = fail;
                this.failOnFallback = failOnFallback;
                this.timeout = timeout;
                this.useFallback = false;
                this.useCache = false;
            }

            protected override string Run()
            {
                // output.WriteLine(Task.CurrentId + " : " + DateTime.Now.ToString());
                if (fail)
                {
                    throw new Exception("forced failure");
                }
                else if (timeout)
                {
                    try
                    {
                        Time.WaitUntil(() => { return _token.IsCancellationRequested; }, 10000);
                        _token.ThrowIfCancellationRequested();

                        // output.WriteLine("Woke up from sleep!");
                        // token.ThrowIfCancellationRequested();
                    }
                    catch (Exception)
                    {
                        // output.WriteLine(Task.CurrentId + " Interrupted by timeout");
                        throw;
                    }
                }

                return value;
            }

            protected override string RunFallback()
            {
                if (useFallback)
                {
                    if (failOnFallback)
                    {
                        throw new Exception("forced fallback failure");
                    }
                    else
                    {
                        return value + "-fallback";
                    }
                }
                else
                {
                    throw new InvalidOperationException("no fallback implemented");
                }
            }

            protected override string CacheKey
            {
                get
                {
                    if (useCache)
                    {
                        return value;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }
    }
}
