// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Steeltoe.Common.Util;
using Xunit;

// TODO: Fix violations and remove the next suppression, by either:
// - Removing the try with empty catch block
// - Add the next comment in the empty catch block: // Intentionally left empty.
// While you're at it, catch specific exceptions (use `when` condition to narrow down) instead of System.Exception.
#pragma warning disable S108 // Nested blocks of code should not be left empty

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public class HystrixRequestLogTest : HystrixTestBase
{
    private const string DigitsRegex = "\\[\\d+";

    [Fact]
    public void TestSuccess()
    {
        new TestCommand("A", false, true).Execute();
        string log = HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString();

        // strip the actual count so we can compare reliably
        log = Regex.Replace(log, DigitsRegex, "[");
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
        log = Regex.Replace(log, DigitsRegex, "[");
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
        log = Regex.Replace(log, DigitsRegex, "[");
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
        log = Regex.Replace(log, DigitsRegex, "[");
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
        log = Regex.Replace(log, DigitsRegex, "[");
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
        log = Regex.Replace(log, DigitsRegex, "[");

        Assert.Equal(
            "GetData[SUCCESS][ms], PutData[SUCCESS][ms], GetValues[SUCCESS][ms], GetValues[SUCCESS, RESPONSE_FROM_CACHE][ms], TestCommand[FAILURE, FALLBACK_FAILURE][ms], TestCommand[FAILURE, FALLBACK_FAILURE, RESPONSE_FROM_CACHE][ms]",
            log);
    }

    [Fact]
    public void TestMaxLimit()
    {
        for (int i = 0; i < HystrixRequestLog.MaxStorage; i++)
        {
            new TestCommand("A", false, true).Execute();
        }

        // then execute again some more
        for (int i = 0; i < 10; i++)
        {
            new TestCommand("A", false, true).Execute();
        }

        Assert.Equal(HystrixRequestLog.MaxStorage, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
    }

    private sealed class TestCommand : HystrixCommand<string>
    {
        private readonly string _value;
        private readonly bool _fail;
        private readonly bool _failOnFallback;
        private readonly bool _timeout;
        private readonly bool _useFallback;
        private readonly bool _useCache;

        protected override string CacheKey
        {
            get
            {
                if (_useCache)
                {
                    return _value;
                }

                return null;
            }
        }

        public TestCommand(string commandName, string value, bool fail, bool failOnFallback)
            : base(new HystrixCommandOptions
            {
                GroupKey = HystrixCommandGroupKeyDefault.AsKey("RequestLogTestCommand"),
                CommandKey = HystrixCommandKeyDefault.AsKey(commandName)
            })
        {
            _value = value;
            _fail = fail;
            _failOnFallback = failOnFallback;
            _timeout = false;
            _useFallback = true;
            _useCache = true;
        }

        public TestCommand(string value, bool fail, bool failOnFallback)
            : base(HystrixCommandGroupKeyDefault.AsKey("RequestLogTestCommand"))
        {
            _value = value;
            _fail = fail;
            _failOnFallback = failOnFallback;
            _timeout = false;
            _useFallback = true;
            _useCache = true;
        }

        public TestCommand(string value, bool fail, bool failOnFallback, bool timeout)
            : base(new HystrixCommandOptions
            {
                GroupKey = HystrixCommandGroupKeyDefault.AsKey("RequestLogTestCommand"),
                ExecutionTimeoutInMilliseconds = 500
            })
        {
            _value = value;
            _fail = fail;
            _failOnFallback = failOnFallback;
            _timeout = timeout;
            _useFallback = false;
            _useCache = false;
        }

        protected override string Run()
        {
            // output.WriteLine(Task.CurrentId + " : " + DateTime.Now.ToString());
            if (_fail)
            {
                throw new Exception("forced failure");
            }

            if (_timeout)
            {
                Time.WaitUntil(() => Token.IsCancellationRequested, 10000);
                Token.ThrowIfCancellationRequested();

                // output.WriteLine("Woke up from sleep!");
                // token.ThrowIfCancellationRequested();
            }

            return _value;
        }

        protected override string RunFallback()
        {
            if (_useFallback)
            {
                if (_failOnFallback)
                {
                    throw new Exception("forced fallback failure");
                }

                return $"{_value}-fallback";
            }

            throw new InvalidOperationException("no fallback implemented");
        }
    }
}
