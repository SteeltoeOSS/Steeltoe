// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reactive.Linq;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency;
using Steeltoe.Common.Util;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test.Strategy.Concurrency;

public class HystrixConcurrencyStrategyTest : HystrixTestBase
{
    private readonly ITestOutputHelper _output;

    public HystrixConcurrencyStrategyTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task TestThreadContextOnTimeout()
    {
        var isInitialized = new AtomicBoolean();

        await Assert.ThrowsAsync<HystrixRuntimeException>(async () =>
        {
            await new TimeoutCommand(_output).ToObservable().Do(n =>
            {
                _output.WriteLine("OnNext = " + n);
            }, _ =>
            {
                _output.WriteLine("OnError = " + HystrixRequestContext.IsCurrentThreadInitialized);
                isInitialized.Value = HystrixRequestContext.IsCurrentThreadInitialized;
            }).SingleAsync();
        });

        _output.WriteLine("initialized = " + HystrixRequestContext.IsCurrentThreadInitialized);
        _output.WriteLine("initialized inside onError = " + isInitialized.Value);
        Assert.True(isInitialized.Value);
    }

    [Fact]
    public void TestNoRequestContextOnSimpleConcurrencyStrategyWithoutException()
    {
        Dispose();

        var opts = new HystrixCommandOptions
        {
            RequestLogEnabled = false,
            GroupKey = HystrixCommandGroupKeyDefault.AsKey("SimpleCommand")
        };

        new SimpleCommand(_output, opts).Execute();

        Assert.True(true, "Nothing blew up");
    }

    private sealed class SimpleCommand : HystrixCommand<string>
    {
        private readonly ITestOutputHelper _output;

        public SimpleCommand(ITestOutputHelper output, IHystrixCommandOptions opts)
            : base(opts)
        {
            _output = output;
        }

        protected override string Run()
        {
            if (HystrixRequestContext.IsCurrentThreadInitialized)
            {
                _output.WriteLine("Executing => Commands: " + HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
            }

            return "Hello";
        }
    }

    private sealed class TimeoutCommand : HystrixCommand
    {
        private readonly ITestOutputHelper _output;

        public TimeoutCommand(ITestOutputHelper output)
            : base(GetCommandOptions())
        {
            _output = output;
        }

        private static IHystrixCommandOptions GetCommandOptions()
        {
            var opts = new HystrixCommandOptions
            {
                GroupKey = HystrixCommandGroupKeyDefault.AsKey("TimeoutTest"),
                ExecutionTimeoutInMilliseconds = 50
            };

            return opts;
        }

        protected override void Run()
        {
            _output.WriteLine("TimeoutCommand - run() start");
            Time.Wait(500);
            _output.WriteLine("TimeoutCommand - run() finish");
        }
    }
}
