// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Observable.Aliases;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable S3966 // Objects should not be disposed more than once

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Sample.Test;

public class HystrixUtilizationStreamTest : CommandStreamTest
{
    private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("Util");
    private static readonly IHystrixCommandKey CommandKey = HystrixCommandKeyDefault.AsKey("Command");
    private readonly HystrixUtilizationStream _stream;

    private readonly ITestOutputHelper _output;

    public HystrixUtilizationStreamTest(ITestOutputHelper output)
    {
        _stream = HystrixUtilizationStream.GetNonSingletonInstanceOnlyUsedInUnitTests(10);
        _output = output;
    }

    [Fact]
    public void TestStreamHasData()
    {
        var commandShowsUp = new AtomicBoolean(false);
        var threadPoolShowsUp = new AtomicBoolean(false);
        var latch = new CountdownEvent(1);
        int num = 10;

        for (int i = 0; i < 2; i++)
        {
            HystrixCommand<int> cmd = Command.From(GroupKey, CommandKey, HystrixEventType.Success, 50);
            cmd.Observe();
        }

        _stream.Observe().Take(num).Subscribe(utilization =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : Received data with : " + " : Received data with : " + utilization.CommandUtilizationMap.Count +
                " commands");

            if (utilization.CommandUtilizationMap.ContainsKey(CommandKey))
            {
                commandShowsUp.Value = true;
            }

            if (utilization.ThreadPoolUtilizationMap.Count != 0)
            {
                threadPoolShowsUp.Value = true;
            }
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " OnError : " + e);
            latch.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " OnCompleted");
            latch.SignalEx();
        });

        Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
        Assert.True(commandShowsUp.Value);
        Assert.True(threadPoolShowsUp.Value);
    }

    [Fact]
    [Trait("Category", "SkipOnMacOS")]
    public void TestTwoSubscribersOneUnsubscribes()
    {
        var latch1 = new CountdownEvent(1);
        var latch2 = new CountdownEvent(1);
        var payloads1 = new AtomicInteger(0);
        var payloads2 = new AtomicInteger(0);

        IDisposable s1 = _stream.Observe().Take(100).OnDispose(() =>
        {
            latch1.SignalEx();
        }).Subscribe(utilization =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnNext : " + utilization);
            payloads1.IncrementAndGet();
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 1 OnError : " + e);
            latch1.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 1 OnCompleted");
            latch1.SignalEx();
        });

        _stream.Observe().Take(100).OnDispose(() =>
        {
            latch2.SignalEx();
        }).Subscribe(utilization =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnNext : " + utilization);
            payloads2.IncrementAndGet();
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 2 OnError : " + e);
            latch2.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 2 OnCompleted");
            latch2.SignalEx();
        });

        // execute 1 command, then unsubscribe from first stream. then execute the rest
        for (int i = 0; i < 50; i++)
        {
            HystrixCommand<int> cmd = Command.From(GroupKey, CommandKey, HystrixEventType.Success, 50);
            cmd.Execute();

            if (i == 1)
            {
                s1.Dispose();
            }
        }

        Assert.True(latch1.Wait(10000));
        Assert.True(latch2.Wait(10000));
        _output.WriteLine("s1 got : " + payloads1.Value + ", s2 got : " + payloads2.Value);
        Assert.True(payloads1.Value > 0); // "s1 got data",
        Assert.True(payloads2.Value > 0); // "s2 got data",
        Assert.True(payloads2.Value > payloads1.Value); // "s1 got less data than s2",
    }

    [Fact]
    public void TestTwoSubscribersBothUnsubscribe()
    {
        var latch1 = new CountdownEvent(1);
        var latch2 = new CountdownEvent(1);
        var payloads1 = new AtomicInteger(0);
        var payloads2 = new AtomicInteger(0);

        IDisposable s1 = _stream.Observe().Take(100).OnDispose(() =>
        {
            latch1.SignalEx();
        }).Subscribe(utilization =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 1 OnNext : " + utilization);
            payloads1.IncrementAndGet();
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 1 OnError : " + e);
            latch1.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 1 OnCompleted");
            latch1.SignalEx();
        });

        IDisposable s2 = _stream.Observe().Take(100).OnDispose(() =>
        {
            latch2.SignalEx();
        }).Subscribe(utilization =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Dashboard 2 OnNext : " + utilization);
            payloads2.IncrementAndGet();
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 2 OnError : " + e);
            latch2.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Dashboard 2 OnCompleted");
            latch2.SignalEx();
        });

        // execute 2 commands, then unsubscribe from both streams, then execute the rest
        for (int i = 0; i < 10; i++)
        {
            HystrixCommand<int> cmd = Command.From(GroupKey, CommandKey, HystrixEventType.Success, 50);
            cmd.Execute();

            if (i == 2)
            {
                s1.Dispose();
                s2.Dispose();
            }
        }

        Assert.False(_stream.IsSourceCurrentlySubscribed); // both subscriptions have been cancelled - source should be too

        Assert.True(latch1.Wait(10000));
        Assert.True(latch2.Wait(10000));
        _output.WriteLine("s1 got : " + payloads1.Value + ", s2 got : " + payloads2.Value);
        Assert.True(payloads1.Value > 0); // "s1 got data",
        Assert.True(payloads2.Value > 0); // "s2 got data",
    }

    [Fact]
    public void TestTwoSubscribersOneSlowOneFast()
    {
        var latch = new CountdownEvent(1);
        var foundError = new AtomicBoolean(false);

        IObservable<HystrixUtilization> fast = _stream.Observe().ObserveOn(NewThreadScheduler.Default);

        IObservable<HystrixUtilization> slow = _stream.Observe().ObserveOn(NewThreadScheduler.Default).Map(util =>
        {
            try
            {
                Time.Wait(100);
                return util;
            }
            catch (Exception)
            {
                return util;
            }
        });

        IObservable<bool> checkZippedEqual = fast.Zip(slow, (payload, payload2) => payload == payload2);

        IDisposable s1 = checkZippedEqual.Take(10000).Subscribe(b =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : OnNext : " + b);
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " OnError : " + e);
            _output.WriteLine(e.ToString());
            foundError.Value = true;
            latch.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " OnCompleted");
            latch.SignalEx();
        });

        for (int i = 0; i < 50; i++)
        {
            HystrixCommand<int> cmd = Command.From(GroupKey, CommandKey, HystrixEventType.Success, 50);
            cmd.Execute();
        }

        latch.Wait(10000);
        Assert.False(foundError.Value);
        s1.Dispose();
    }
}
