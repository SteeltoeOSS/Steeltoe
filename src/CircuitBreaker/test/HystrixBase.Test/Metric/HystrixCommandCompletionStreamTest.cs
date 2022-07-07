// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Test;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Test;

public class HystrixCommandCompletionStreamTest : CommandStreamTest
{
    private sealed class LatchedObserver : ObserverBase<HystrixCommandCompletion>
    {
        private readonly CountdownEvent _latch;

        public LatchedObserver(CountdownEvent latch)
        {
            _latch = latch;
        }

        protected override void OnCompletedCore()
        {
            _latch.SignalEx();
        }

        protected override void OnErrorCore(Exception error)
        {
            Assert.False(true, error.Message);
        }

        protected override void OnNextCore(HystrixCommandCompletion value)
        {
        }
    }

    private static readonly IHystrixCommandKey CommandKey = HystrixCommandKeyDefault.AsKey("COMMAND");
    private static readonly IHystrixThreadPoolKey ThreadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool");
    private readonly HystrixCommandCompletionStream _commandStream = new (CommandKey);
    private readonly ITestOutputHelper _output;

    public HystrixCommandCompletionStreamTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestNoEvents()
    {
        var latch = new CountdownEvent(1);
        IObserver<HystrixCommandCompletion> subscriber = new LatchedObserver(latch);

        _commandStream.Observe().Take(1).Subscribe(subscriber);

        // no writes
        Assert.False(latch.Wait(TimeSpan.FromMilliseconds(1000)));
    }

    [Fact]
    public void TestSingleWriteSingleSubscriber()
    {
        var latch = new CountdownEvent(1);
        IObserver<HystrixCommandCompletion> subscriber = new LatchedObserver(latch);

        _commandStream.Observe().Take(1).Subscribe(subscriber);

        var result = ExecutionResult.From(HystrixEventType.SUCCESS).SetExecutedInThread();
        var @event = HystrixCommandCompletion.From(result, CommandKey, ThreadPoolKey);
        _commandStream.Write(@event);

        Assert.True(latch.Wait(TimeSpan.FromMilliseconds(1000)));
    }

    [Fact]
    public void TestSingleWriteMultipleSubscribers()
    {
        var latch1 = new CountdownEvent(1);
        IObserver<HystrixCommandCompletion> subscriber1 = new LatchedObserver(latch1);

        var latch2 = new CountdownEvent(1);
        IObserver<HystrixCommandCompletion> subscriber2 = new LatchedObserver(latch2);

        _commandStream.Observe().Take(1).Subscribe(subscriber1);
        _commandStream.Observe().Take(1).Subscribe(subscriber2);

        var result = ExecutionResult.From(HystrixEventType.SUCCESS).SetExecutedInThread();
        var @event = HystrixCommandCompletion.From(result, CommandKey, ThreadPoolKey);
        _commandStream.Write(@event);

        Assert.True(latch1.Wait(TimeSpan.FromMilliseconds(1000)));
        Assert.True(latch2.Wait(TimeSpan.FromMilliseconds(10)));
    }
}
