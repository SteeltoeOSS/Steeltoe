// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reactive.Linq;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.Common.Util;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test;

public sealed class RollingCollapserEventCounterStreamTest : CommandStreamTest
{
    private readonly ITestOutputHelper _output;
    private RollingCollapserEventCounterStream _stream;
    private IDisposable _latchSubscription;

    public RollingCollapserEventCounterStreamTest(ITestOutputHelper output)
    {
        _output = output;
        RollingCollapserEventCounterStream.Reset();
        HystrixCollapserEventStream.Reset();
    }

    [Fact]
    public void TestEmptyStreamProducesZeros()
    {
        IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("RollingCollapser-A");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCollapserEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(CollapserEventTypeHelper.Values.Count, _stream.Latest.Length);
        Assert.Equal(0, _stream.GetLatest(CollapserEventType.AddedToBatch));
        Assert.Equal(0, _stream.GetLatest(CollapserEventType.BatchExecuted));
        Assert.Equal(0, _stream.GetLatest(CollapserEventType.ResponseFromCache));
    }

    [Fact]
    public void TestCollapsed()
    {
        IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("RollingCollapser-B");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCollapserEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        var cTasks = new List<Task>();

        for (int i = 0; i < 3; i++)
        {
            cTasks.Add(Collapser.From(_output, key, i).ExecuteAsync());
        }

        Task.WaitAll(cTasks.ToArray());

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(CollapserEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[CollapserEventTypeHelper.Values.Count];
        expected[(int)CollapserEventType.BatchExecuted] = 1;
        expected[(int)CollapserEventType.AddedToBatch] = 3;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public void TestCollapsedAndResponseFromCache()
    {
        IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("RollingCollapser-C");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCollapserEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        var cTasks = new List<Task>();

        for (int i = 0; i < 3; i++)
        {
            cTasks.Add(Collapser.From(_output, key, i).ExecuteAsync());
            cTasks.Add(Collapser.From(_output, key, i).ExecuteAsync()); // same arg - should get a response from cache
            cTasks.Add(Collapser.From(_output, key, i).ExecuteAsync()); // same arg - should get a response from cache
        }

        Task.WaitAll(cTasks.ToArray());

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(CollapserEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[CollapserEventTypeHelper.Values.Count];
        expected[(int)CollapserEventType.BatchExecuted] = 1;
        expected[(int)CollapserEventType.AddedToBatch] = 3;
        expected[(int)CollapserEventType.ResponseFromCache] = 6;
        Assert.Equal(expected, _stream.Latest);
    }

    // by doing a take(30), we expect all values to return to 0 as they age out of rolling window
    [Fact]
    public void TestCollapsedAndResponseFromCacheAgeOutOfRollingWindow()
    {
        IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("RollingCollapser-D");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCollapserEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Take(20 + LatchedObserver.StableTickCount).Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        var cTasks = new List<Task>();

        for (int i = 0; i < 3; i++)
        {
            cTasks.Add(Collapser.From(_output, key, i).ExecuteAsync());
            cTasks.Add(Collapser.From(_output, key, i).ExecuteAsync()); // same arg - should get a response from cache
            cTasks.Add(Collapser.From(_output, key, i).ExecuteAsync()); // same arg - should get a response from cache
        }

        Task.WaitAll(cTasks.ToArray());

        Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.Equal(CollapserEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[CollapserEventTypeHelper.Values.Count];
        expected[(int)CollapserEventType.BatchExecuted] = 0;
        expected[(int)CollapserEventType.AddedToBatch] = 0;
        expected[(int)CollapserEventType.ResponseFromCache] = 0;
        Assert.Equal(expected, _stream.Latest);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _latchSubscription?.Dispose();
            _latchSubscription = null;

            _stream?.Unsubscribe();
            _stream = null;
        }

        base.Dispose(disposing);
    }

    private sealed class LatchedObserver : TestObserverBase<long[]>
    {
        public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
            : base(output, latch)
        {
        }
    }
}
