// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reactive.Linq;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.Common.Util;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test;

public class RollingCommandLatencyDistributionStreamTest : CommandStreamTest
{
    private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("CommandLatency");
    private readonly ITestOutputHelper _output;
    private RollingCommandLatencyDistributionStream _stream;
    private IDisposable _latchSubscription;

    public RollingCommandLatencyDistributionStreamTest(ITestOutputHelper output)
    {
        _output = output;
        RollingCommandLatencyDistributionStream.Reset();
        HystrixCommandCompletionStream.Reset();
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestEmptyStreamProducesEmptyDistributions()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-A");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.Equal(0, _stream.Latest.GetTotalCount());
    }

    [Fact]
    public async Task TestSingleBucketGetsStored()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-B");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd1 = Command.From(GroupKey, key, HystrixEventType.Success, 10);
        Command cmd2 = Command.From(GroupKey, key, HystrixEventType.Timeout); // latency = 600
        await cmd1.Observe();
        await cmd2.Observe();

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        AssertBetween(100, 400, _stream.LatestMean);
        AssertBetween(10, 100, _stream.GetLatestPercentile(0.0));
        AssertBetween(300, 800, _stream.GetLatestPercentile(100.0));
    }

    // The following event types should not have their latency measured:
    // THREAD_POOL_REJECTED
    // SEMAPHORE_REJECTED
    // SHORT_CIRCUITED
    // RESPONSE_FROM_CACHE
    // Newly measured (as of 1.5)
    // BAD_REQUEST
    // FAILURE
    // TIMEOUT
    [Fact]
    public async Task TestSingleBucketWithMultipleEventTypes()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-C");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd1 = Command.From(GroupKey, key, HystrixEventType.Success, 10);
        Command cmd2 = Command.From(GroupKey, key, HystrixEventType.Timeout); // latency = 600
        Command cmd3 = Command.From(GroupKey, key, HystrixEventType.Failure, 30);
        Command cmd4 = Command.From(GroupKey, key, HystrixEventType.BadRequest, 40);

        await cmd1.Observe();
        await cmd3.Observe();
        await Assert.ThrowsAsync<HystrixBadRequestException>(async () => await cmd4.Observe());
        await cmd2.Observe(); // Timeout should run last

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        AssertBetween(100, 400, _stream.LatestMean); // now timeout latency of 600ms is there
        AssertBetween(10, 100, _stream.GetLatestPercentile(0.0));
        AssertBetween(300, 800, _stream.GetLatestPercentile(100.0));
    }

    [Fact]
    public async Task TestShortCircuitedCommandDoesNotGetLatencyTracked()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-D");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // 3 failures is enough to trigger short-circuit.  execute those, then wait for bucket to roll
        // next command should be a short-circuit
        var commands = new List<Command>();

        for (int i = 0; i < 3; i++)
        {
            commands.Add(Command.From(GroupKey, key, HystrixEventType.Failure, 0));
        }

        Command shortCircuit = Command.From(GroupKey, key, HystrixEventType.Success);

        foreach (Command cmd in commands)
        {
            await cmd.Observe();
        }

        Assert.True(WaitForHealthCountToUpdate(key.Name, 500, _output), "health count took to long to update");

        try
        {
            await shortCircuit.Observe();
        }
        catch (Exception ie)
        {
            Assert.True(false, ie.Message);
        }

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.Equal(3, _stream.Latest.GetTotalCount());
        AssertBetween(0, 75, _stream.LatestMean);

        Assert.True(shortCircuit.IsResponseShortCircuited);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestThreadPoolRejectedCommandDoesNotGetLatencyTracked()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-E");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // 10 commands with latency should occupy the entire thread-pool.  execute those, then wait for bucket to roll
        // next command should be a thread-pool rejection
        var commands = new List<Command>();

        for (int i = 0; i < 10; i++)
        {
            commands.Add(Command.From(GroupKey, key, HystrixEventType.Success, 500));
        }

        Command threadPoolRejected = Command.From(GroupKey, key, HystrixEventType.Success);

        var satTasks = new List<Task>();

        foreach (Command cmd in commands)
        {
            satTasks.Add(cmd.ExecuteAsync());
        }

        await threadPoolRejected.Observe();
        Task.WaitAll(satTasks.ToArray());

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.Equal(10, _stream.Latest.GetTotalCount());
        AssertBetween(500, 750, _stream.LatestMean);
        Assert.True(threadPoolRejected.IsResponseThreadPoolRejected);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSemaphoreRejectedCommandDoesNotGetLatencyTracked()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-F");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // 10 commands with latency should occupy all semaphores.  execute those, then wait for bucket to roll
        // next command should be a semaphore rejection
        var commands = new List<Command>();

        for (int i = 0; i < 10; i++)
        {
            commands.Add(Command.From(GroupKey, key, HystrixEventType.Success, 500, ExecutionIsolationStrategy.Semaphore));
        }

        Command semaphoreRejected = Command.From(GroupKey, key, HystrixEventType.Success, 0, ExecutionIsolationStrategy.Semaphore);
        var satTasks = new List<Task>();

        foreach (Command saturator in commands)
        {
            satTasks.Add(Task.Run(() => saturator.Execute()));
        }

        await Task.Delay(50);

        await Task.Run(() => semaphoreRejected.Execute());

        Task.WaitAll(satTasks.ToArray());

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.Equal(10, _stream.Latest.GetTotalCount());
        AssertBetween(500, 750, _stream.LatestMean);
        Assert.True(semaphoreRejected.IsResponseSemaphoreRejected);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestResponseFromCacheDoesNotGetLatencyTracked()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-G");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // should get 1 SUCCESS and 1 RESPONSE_FROM_CACHE
        List<Command> commands = Command.GetCommandsWithResponseFromCache(GroupKey, key);

        foreach (Command cmd in commands)
        {
            _ = cmd.ExecuteAsync();
        }

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.Equal(1, _stream.Latest.GetTotalCount());
        AssertBetween(0, 75, _stream.LatestMean);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestMultipleBucketsBothGetStored()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-H");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd1 = Command.From(GroupKey, key, HystrixEventType.Success, 10);
        Command cmd2 = Command.From(GroupKey, key, HystrixEventType.Failure, 100);

        Command cmd3 = Command.From(GroupKey, key, HystrixEventType.Success, 60);
        Command cmd4 = Command.From(GroupKey, key, HystrixEventType.Success, 60);
        Command cmd5 = Command.From(GroupKey, key, HystrixEventType.Success, 70);

        await cmd1.Observe();
        await cmd2.Observe();

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        await cmd3.Observe();
        await cmd4.Observe();
        await cmd5.Observe();

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        AssertBetween(50, 150, _stream.LatestMean);
        AssertBetween(10, 150, _stream.GetLatestPercentile(0.0));
        AssertBetween(100, 150, _stream.GetLatestPercentile(100.0));
    }

    [Fact]
    public async Task TestMultipleBucketsBothGetStoredAndThenAgeOut()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-Latency-I");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandLatencyDistributionStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Take(20 + LatchedObserver.StableTickCount).Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd1 = Command.From(GroupKey, key, HystrixEventType.Success, 10);
        Command cmd2 = Command.From(GroupKey, key, HystrixEventType.Failure, 100);

        Command cmd3 = Command.From(GroupKey, key, HystrixEventType.Success, 60);
        Command cmd4 = Command.From(GroupKey, key, HystrixEventType.Success, 60);
        Command cmd5 = Command.From(GroupKey, key, HystrixEventType.Success, 70);

        await cmd1.Observe();
        await cmd2.Observe();

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        await cmd3.Observe();
        await cmd4.Observe();
        await cmd5.Observe();

        WaitForLatchedObserverToUpdate(observer, 1, 500, _output);

        Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

        Assert.Equal(0, _stream.Latest.GetTotalCount());
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

    private void AssertBetween(int expectedLow, int expectedHigh, int value)
    {
        _output.WriteLine("Low:" + expectedLow + " High:" + expectedHigh + " Value: " + value);
        Assert.InRange(value, expectedLow, expectedHigh);
    }

    private sealed class LatchedObserver : TestObserverBase<CachedValuesHistogram>
    {
        public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
            : base(output, latch)
        {
        }
    }
}
