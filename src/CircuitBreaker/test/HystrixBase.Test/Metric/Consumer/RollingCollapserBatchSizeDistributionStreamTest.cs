// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test;

public class RollingCollapserBatchSizeDistributionStreamTest : CommandStreamTest
{
    private readonly ITestOutputHelper _output;
    private RollingCollapserBatchSizeDistributionStream _stream;
    private IDisposable _latchSubscription;

    private sealed class LatchedObserver : TestObserverBase<CachedValuesHistogram>
    {
        public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
            : base(output, latch)
        {
        }
    }

    public RollingCollapserBatchSizeDistributionStreamTest(ITestOutputHelper output)
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
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestEmptyStreamProducesEmptyDistributions()
    {
        var key = HystrixCollapserKeyDefault.AsKey("Collapser-Batch-Size-A");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCollapserBatchSizeDistributionStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.Equal(0, _stream.Latest.GetTotalCount());
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestBatches()
    {
        var key = HystrixCollapserKeyDefault.AsKey("Collapser-Batch-Size-B");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCollapserBatchSizeDistributionStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // First collapser created with key will be used for all command creations
        var tasks = new List<Task>();

        var c1 = Collapser.From(_output, key, 1);
        tasks.Add(c1.ExecuteAsync());
        var c2 = Collapser.From(_output, key, 2);
        tasks.Add(c2.ExecuteAsync());
        var c3 = Collapser.From(_output, key, 3);
        tasks.Add(c3.ExecuteAsync());
        Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 1 too long to start");
        c1.CommandCreated = false;

        var c4 = Collapser.From(_output, key, 4);
        tasks.Add(c4.ExecuteAsync());
        Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 2 too long to start");
        c1.CommandCreated = false;

        var c5 = Collapser.From(_output, key, 5);
        tasks.Add(c5.ExecuteAsync());
        var c6 = Collapser.From(_output, key, 6);
        tasks.Add(c6.ExecuteAsync());
        var c7 = Collapser.From(_output, key, 7);
        tasks.Add(c7.ExecuteAsync());
        var c8 = Collapser.From(_output, key, 8);
        tasks.Add(c8.ExecuteAsync());
        var c9 = Collapser.From(_output, key, 9);
        tasks.Add(c9.ExecuteAsync());
        Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 3 too long to start");
        c1.CommandCreated = false;

        var c10 = Collapser.From(_output, key, 10);
        tasks.Add(c10.ExecuteAsync());
        var c11 = Collapser.From(_output, key, 11);
        tasks.Add(c11.ExecuteAsync());
        var c12 = Collapser.From(_output, key, 12);
        tasks.Add(c12.ExecuteAsync());
        Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 4 too long to start");

        Task.WaitAll(tasks.ToArray());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        // should have 4 batches: 3, 1, 5, 3
        Assert.Equal(4, _stream.Latest.GetTotalCount());
        Assert.Equal(3, _stream.LatestMean);
        Assert.Equal(1, _stream.GetLatestPercentile(0));
        Assert.Equal(5, _stream.GetLatestPercentile(100));
    }

    // by doing a take(30), all metrics should fall out of window and we should observe an empty histogram
    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestBatchesAgeOut()
    {
        var key = HystrixCollapserKeyDefault.AsKey("Collapser-Batch-Size-B");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCollapserBatchSizeDistributionStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Take(20 + LatchedObserver.StableTickCount).Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // First collapser created with key will be used for all command creations
        var tasks = new List<Task>();

        var c1 = Collapser.From(_output, key, 1);
        tasks.Add(c1.ExecuteAsync());
        var c2 = Collapser.From(_output, key, 2);
        tasks.Add(c2.ExecuteAsync());
        var c3 = Collapser.From(_output, key, 3);
        tasks.Add(c3.ExecuteAsync());
        Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 1 too long to start");
        c1.CommandCreated = false;

        var c4 = Collapser.From(_output, key, 4);
        tasks.Add(c4.ExecuteAsync());
        Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 2 too long to start");
        c1.CommandCreated = false;

        var c5 = Collapser.From(_output, key, 5);
        tasks.Add(c5.ExecuteAsync());
        var c6 = Collapser.From(_output, key, 6);
        tasks.Add(c6.ExecuteAsync());
        var c7 = Collapser.From(_output, key, 7);
        tasks.Add(c7.ExecuteAsync());
        var c8 = Collapser.From(_output, key, 8);
        tasks.Add(c8.ExecuteAsync());
        var c9 = Collapser.From(_output, key, 9);
        tasks.Add(c9.ExecuteAsync());
        Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 3 too long to start");
        c1.CommandCreated = false;

        var c10 = Collapser.From(_output, key, 10);
        tasks.Add(c10.ExecuteAsync());
        var c11 = Collapser.From(_output, key, 11);
        tasks.Add(c11.ExecuteAsync());
        var c12 = Collapser.From(_output, key, 12);
        tasks.Add(c12.ExecuteAsync());
        Assert.True(Time.WaitUntil(() => c1.CommandCreated, 500), "Batch 4 too long to start");

        Task.WaitAll(tasks.ToArray());

        Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

        Assert.Equal(0, _stream.Latest.GetTotalCount());
        Assert.Equal(0, _stream.LatestMean);
    }
}
