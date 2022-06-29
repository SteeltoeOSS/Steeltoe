// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test;

public class CumulativeCollapserEventCounterStreamTest : CommandStreamTest
{
    private sealed class LatchedObserver : TestObserverBase<long[]>
    {
        public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
            : base(output, latch)
        {
        }
    }

    private readonly ITestOutputHelper _output;
    private CumulativeCollapserEventCounterStream _stream;
    private IDisposable _latchSubscription;

    public CumulativeCollapserEventCounterStreamTest(ITestOutputHelper output)
    {
        _output = output;
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

    [Fact]
    public void TestEmptyStreamProducesZeros()
    {
        var key = HystrixCollapserKeyDefault.AsKey("CumulativeCollapser-A");

        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = CumulativeCollapserEventCounterStream.GetInstance(key, 10, 100);

        _latchSubscription = _stream.Observe().Subscribe(observer);

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.Equal(CollapserEventTypeHelper.Values.Count, _stream.Latest.Length);

        Assert.Equal(0, _stream.GetLatest(CollapserEventType.ADDED_TO_BATCH));
        Assert.Equal(0, _stream.GetLatest(CollapserEventType.BATCH_EXECUTED));
        Assert.Equal(0, _stream.GetLatest(CollapserEventType.RESPONSE_FROM_CACHE));
    }

    [Fact]
    public void TestCollapsed()
    {
        var key = HystrixCollapserKeyDefault.AsKey("CumulativeCollapser-B");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = CumulativeCollapserEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);

        var tasks = new List<Task>();
        for (var i = 0; i < 3; i++)
        {
            tasks.Add(Collapser.From(_output, key, i).ExecuteAsync());
        }

        Task.WaitAll(tasks.ToArray());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(CollapserEventTypeHelper.Values.Count, _stream.Latest.Length);
        var expected = new long[CollapserEventTypeHelper.Values.Count];
        expected[(int)CollapserEventType.BATCH_EXECUTED] = 1;
        expected[(int)CollapserEventType.ADDED_TO_BATCH] = 3;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public void TestCollapsedAndResponseFromCache()
    {
        var key = HystrixCollapserKeyDefault.AsKey("CumulativeCollapser-C");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = CumulativeCollapserEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);

        var tasks = new List<Task>();
        for (var i = 0; i < 3; i++)
        {
            tasks.Add(Collapser.From(_output, key, i).ExecuteAsync());
            tasks.Add(Collapser.From(_output, key, i).ExecuteAsync()); // same arg - should get a response from cache
            tasks.Add(Collapser.From(_output, key, i).ExecuteAsync()); // same arg - should get a response from cache
        }

        Task.WaitAll(tasks.ToArray());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(CollapserEventTypeHelper.Values.Count, _stream.Latest.Length);
        var expected = new long[CollapserEventTypeHelper.Values.Count];
        expected[(int)CollapserEventType.BATCH_EXECUTED] = 1;
        expected[(int)CollapserEventType.ADDED_TO_BATCH] = 3;
        expected[(int)CollapserEventType.RESPONSE_FROM_CACHE] = 6;
        Assert.Equal(expected, _stream.Latest);
    }

    // by doing a take(30), we expect all values to stay in the stream, as cumulative counters never age out of window
    [Fact]
    public void TestCollapsedAndResponseFromCacheAgeOutOfCumulativeWindow()
    {
        var key = HystrixCollapserKeyDefault.AsKey("CumulativeCollapser-D");
        _stream = CumulativeCollapserEventCounterStream.GetInstance(key, 10, 100);
        _stream.StartCachingStreamValuesIfUnstarted();

        var latch = new CountdownEvent(1);
        _latchSubscription = _stream.Observe().Take(20 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(new LatchedObserver(_output, latch));

        for (var i = 0; i < 3; i++)
        {
            Collapser.From(_output, key, i).Observe();
            Collapser.From(_output, key, i).Observe(); // same arg - should get a response from cache
            Collapser.From(_output, key, i).Observe(); // same arg - should get a response from cache
        }

        Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

        Assert.Equal(CollapserEventTypeHelper.Values.Count, _stream.Latest.Length);
        var expected = new long[CollapserEventTypeHelper.Values.Count];
        expected[(int)CollapserEventType.BATCH_EXECUTED] = 1;
        expected[(int)CollapserEventType.ADDED_TO_BATCH] = 3;
        expected[(int)CollapserEventType.RESPONSE_FROM_CACHE] = 6;
        Assert.Equal(expected, _stream.Latest);
    }
}
