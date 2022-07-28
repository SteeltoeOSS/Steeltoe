// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.Common.Util;
using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#pragma warning disable S3966 // Objects should not be disposed more than once

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test;

public class HealthCountsStreamTest : CommandStreamTest
{
    private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("HealthCounts");
    private readonly ITestOutputHelper _output;
    private HealthCountsStream _stream;
    private IDisposable _latchSubscription;

    private sealed class LatchedObserver : TestObserverBase<HealthCounts>
    {
        public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
            : base(output, latch)
        {
        }
    }

    public HealthCountsStreamTest(ITestOutputHelper output)
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
    public void TestEmptyStreamProducesZeros()
    {
        var key = HystrixCommandKeyDefault.AsKey("CMD-Health-A");

        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = HealthCountsStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.Equal(0L, _stream.Latest.ErrorCount);
        Assert.Equal(0L, _stream.Latest.TotalRequests);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSingleSuccess()
    {
        var key = HystrixCommandKeyDefault.AsKey("CMD-Health-B");

        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = HealthCountsStream.GetInstance(key, 10, 100);

        var cmd = Command.From(GroupKey, key, HystrixEventType.Success, 20);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        await cmd.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.Equal(0L, _stream.Latest.ErrorCount);
        Assert.Equal(1L, _stream.Latest.TotalRequests);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSingleFailure()
    {
        var key = HystrixCommandKeyDefault.AsKey("CMD-Health-C");

        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = HealthCountsStream.GetInstance(key, 10, 100);
        var cmd = Command.From(GroupKey, key, HystrixEventType.Failure, 0);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        await cmd.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.Equal(1L, _stream.Latest.ErrorCount);
        Assert.Equal(1L, _stream.Latest.TotalRequests);
    }

    [Fact]
    public async Task TestSingleTimeout()
    {
        var key = HystrixCommandKeyDefault.AsKey("CMD-Health-D");

        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = HealthCountsStream.GetInstance(key, 10, 100);

        var cmd = Command.From(GroupKey, key, HystrixEventType.Timeout);  // Timeout 1000
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        await cmd.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.Equal(1L, _stream.Latest.ErrorCount);
        Assert.Equal(1L, _stream.Latest.TotalRequests);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSingleBadRequest()
    {
        var key = HystrixCommandKeyDefault.AsKey("CMD-Health-E");

        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = HealthCountsStream.GetInstance(key, 10, 100);

        var cmd = Command.From(GroupKey, key, HystrixEventType.BadRequest);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        await Assert.ThrowsAsync<HystrixBadRequestException>(async () => await cmd.Observe());

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(0L, _stream.Latest.ErrorCount);
        Assert.Equal(0L, _stream.Latest.TotalRequests);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestRequestFromCache()
    {
        var key = HystrixCommandKeyDefault.AsKey("CMD-Health-F");

        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = HealthCountsStream.GetInstance(key, 10, 100);

        var cmd1 = Command.From(GroupKey, key, HystrixEventType.Success, 0);
        var cmd2 = Command.From(GroupKey, key, HystrixEventType.ResponseFromCache);
        var cmd3 = Command.From(GroupKey, key, HystrixEventType.ResponseFromCache);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        await cmd1.Observe();
        await cmd2.Observe();
        await cmd3.Observe();

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.Equal(0L, _stream.Latest.ErrorCount);
        Assert.Equal(1L, _stream.Latest.TotalRequests); // responses from cache should not show up here
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestShortCircuited()
    {
        var key = HystrixCommandKeyDefault.AsKey("CMD-Health-G");

        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = HealthCountsStream.GetInstance(key, 10, 100);

        var failure1 = Command.From(GroupKey, key, HystrixEventType.Failure, 0);
        var failure2 = Command.From(GroupKey, key, HystrixEventType.Failure, 0);
        var failure3 = Command.From(GroupKey, key, HystrixEventType.Failure, 0);
        var shortCircuit1 = Command.From(GroupKey, key, HystrixEventType.Success);
        var shortCircuit2 = Command.From(GroupKey, key, HystrixEventType.Success);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
        // should see 3 FAILUREs and 2 SHORT_CIRCUITs and then 5 FALLBACK_SUCCESSes
        await failure1.Observe();
        await failure2.Observe();
        await failure3.Observe();

        _output.WriteLine(Time.CurrentTimeMillis + " Waiting for health window to change");
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        _output.WriteLine(Time.CurrentTimeMillis + " Running short circuits");

        await shortCircuit1.Observe();
        await shortCircuit2.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.True(shortCircuit1.IsResponseShortCircuited);
        Assert.True(shortCircuit2.IsResponseShortCircuited);

        // should only see failures here, not SHORT-CIRCUITS
        Assert.Equal(3L, _stream.Latest.ErrorCount);
        Assert.Equal(3L, _stream.Latest.TotalRequests);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSemaphoreRejected()
    {
        var key = HystrixCommandKeyDefault.AsKey("CMD-Health-H");
        var saturators = new List<Command>();

        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = HealthCountsStream.GetInstance(key, 10, 100);

        for (var i = 0; i < 10; i++)
        {
            saturators.Add(Command.From(GroupKey, key, HystrixEventType.Success, 500, ExecutionIsolationStrategy.Semaphore));
        }

        var rejected1 = Command.From(GroupKey, key, HystrixEventType.Success, 0, ExecutionIsolationStrategy.Semaphore);
        var rejected2 = Command.From(GroupKey, key, HystrixEventType.Success, 0, ExecutionIsolationStrategy.Semaphore);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // 10 commands will saturate semaphore when called from different threads.
        // submit 2 more requests and they should be SEMAPHORE_REJECTED
        // should see 10 SUCCESSes, 2 SEMAPHORE_REJECTED and 2 FALLBACK_SUCCESSes
        var tasks = new List<Task>();
        foreach (var saturator in saturators)
        {
            tasks.Add(Task.Run(() => saturator.Execute()));
        }

        await Task.Delay(50);

        tasks.Add(Task.Run(() => rejected1.Execute()));
        tasks.Add(Task.Run(() => rejected2.Execute()));

        Task.WaitAll(tasks.ToArray());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.True(rejected1.IsResponseSemaphoreRejected, "rejected1 not rejected");
        Assert.True(rejected2.IsResponseSemaphoreRejected, "rejected2 not rejected");

        // should only see failures here, not SHORT-CIRCUITS
        Assert.Equal(2L, _stream.Latest.ErrorCount);
        Assert.Equal(12L, _stream.Latest.TotalRequests);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestThreadPoolRejected()
    {
        var key = HystrixCommandKeyDefault.AsKey("CMD-Health-I");
        var saturators = new List<Command>();

        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = HealthCountsStream.GetInstance(key, 10, 100);

        for (var i = 0; i < 10; i++)
        {
            saturators.Add(Command.From(GroupKey, key, HystrixEventType.Success, 400));
        }

        var rejected1 = Command.From(GroupKey, key, HystrixEventType.Success, 0);
        var rejected2 = Command.From(GroupKey, key, HystrixEventType.Success, 0);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // 10 commands will saturate thread-pools when called concurrently.
        // submit 2 more requests and they should be THREADPOOL_REJECTED
        // should see 10 SUCCESSes, 2 THREADPOOL_REJECTED and 2 FALLBACK_SUCCESSes
        var tasks = new List<Task>();
        foreach (var saturator in saturators)
        {
            tasks.Add(saturator.ExecuteAsync());
        }

        tasks.Add(rejected1.ExecuteAsync());
        tasks.Add(rejected2.ExecuteAsync());

        Task.WaitAll(tasks.ToArray());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.True(rejected1.IsResponseThreadPoolRejected, "rejected1 not rejected");
        Assert.True(rejected2.IsResponseThreadPoolRejected, "rejected2 not rejected");
        Assert.Equal(2L, _stream.Latest.ErrorCount);
        Assert.Equal(12L, _stream.Latest.TotalRequests);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestFallbackFailure()
    {
        var key = HystrixCommandKeyDefault.AsKey("CMD-Health-J");

        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = HealthCountsStream.GetInstance(key, 10, 100);

        var cmd = Command.From(GroupKey, key, HystrixEventType.Failure, 20, HystrixEventType.FallbackFailure);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(1L, _stream.Latest.ErrorCount);
        Assert.Equal(1L, _stream.Latest.TotalRequests);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestFallbackMissing()
    {
        var key = HystrixCommandKeyDefault.AsKey("CMD-Health-K");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = HealthCountsStream.GetInstance(key, 10, 100);

        var cmd = Command.From(GroupKey, key, HystrixEventType.Failure, 20, HystrixEventType.FallbackMissing);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(1L, _stream.Latest.ErrorCount);
        Assert.Equal(1L, _stream.Latest.TotalRequests);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestFallbackRejection()
    {
        var key = HystrixCommandKeyDefault.AsKey("CMD-Health-L");
        var fallbackSaturators = new List<Command>();
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = HealthCountsStream.GetInstance(key, 10, 100);

        for (var i = 0; i < 5; i++)
        {
            fallbackSaturators.Add(Command.From(GroupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackSuccess, 500));
        }

        var rejection1 = Command.From(GroupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackSuccess, 0);
        var rejection2 = Command.From(GroupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackSuccess, 0);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // fallback semaphore size is 5.  So let 5 commands saturate that semaphore, then
        // let 2 more commands go to fallback.  they should get rejected by the fallback-semaphore
        var tasks = new List<Task>();
        foreach (var saturator in fallbackSaturators)
        {
            tasks.Add(saturator.ExecuteAsync());
        }

        await Task.Delay(50);

        _output.WriteLine("ReqLog1 @ " + Time.CurrentTimeMillis + " " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection1.Observe());
        await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection2.Observe());

        _output.WriteLine("ReqLog2 @ " + Time.CurrentTimeMillis + " " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

        Task.WaitAll(tasks.ToArray());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.Equal(7L, _stream.Latest.ErrorCount);
        Assert.Equal(7L, _stream.Latest.TotalRequests);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestMultipleEventsOverTimeGetStoredAndAgeOut()
    {
        var key = HystrixCommandKeyDefault.AsKey("CMD-Health-M");

        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = HealthCountsStream.GetInstance(key, 10, 100);

        var cmd1 = Command.From(GroupKey, key, HystrixEventType.Success, 20);
        var cmd2 = Command.From(GroupKey, key, HystrixEventType.Failure, 10);

        // by doing a take(30), we ensure that all rolling counts go back to 0
        _latchSubscription = _stream.Observe().Take(30 + LatchedObserver.StableTickCount).Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        await cmd1.Observe();
        await cmd2.Observe();
        Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.Equal(0L, _stream.Latest.ErrorCount);
        Assert.Equal(0L, _stream.Latest.TotalRequests);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestSharedSourceStream()
    {
        var key = HystrixCommandKeyDefault.AsKey("CMD-Health-N");

        _stream = HealthCountsStream.GetInstance(key, 10, 100);

        var latch = new CountdownEvent(1);
        var allEqual = new AtomicBoolean(false);

        var o1 = _stream
            .Observe()
            .Take(10)
            .ObserveOn(TaskPoolScheduler.Default);

        var o2 = _stream
            .Observe()
            .Take(10)
            .ObserveOn(TaskPoolScheduler.Default);

        var zipped = o1.Zip(o2, (healthCounts, healthCounts2) => healthCounts == healthCounts2);
        var reduced = zipped.Aggregate(true, (a, b) => a && b).Select(n => n);

        var disposable = reduced.Subscribe(
            b =>
            {
                _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Reduced OnNext : " + b);
                allEqual.Value = b;
            },
            e =>
            {
                _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Reduced OnError : " + e);
                _output.WriteLine(e.ToString());
                latch.SignalEx();
            },
            () =>
            {
                _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Reduced OnCompleted");
                latch.SignalEx();
            });

        for (var i = 0; i < 10; i++)
        {
            HystrixCommand<int> cmd = Command.From(GroupKey, key, HystrixEventType.Success, 20);
            cmd.Execute();
        }

        Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
        Assert.True(allEqual.Value);

        disposable.Dispose();

        // we should be getting the same object from both streams.  this ensures that multiple subscribers don't induce extra work
    }

    [Fact]
    public void TestTwoSubscribersOneUnsubscribes()
    {
        var key = HystrixCommandKeyDefault.AsKey("CMD-Health-O");
        _stream = HealthCountsStream.GetInstance(key, 10, 100);

        var latch1 = new CountdownEvent(1);
        var latch2 = new CountdownEvent(1);
        var healthCounts1 = new AtomicInteger(0);
        var healthCounts2 = new AtomicInteger(0);

        var s1 = _stream
            .Observe()
            .Take(10)
            .ObserveOn(TaskPoolScheduler.Default)
            .Finally(() =>
            {
                latch1.SignalEx();
            })
            .Subscribe(
                healthCounts =>
                {
                    _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 1 OnNext : " + healthCounts);
                    healthCounts1.IncrementAndGet();
                },
                e =>
                {
                    _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 1 OnError : " + e);
                    latch1.SignalEx();
                },
                () =>
                {
                    _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 1 OnCompleted");
                    latch1.SignalEx();
                });
        var s2 = _stream
            .Observe()
            .Take(10)
            .ObserveOn(TaskPoolScheduler.Default)
            .Finally(() =>
            {
                latch2.SignalEx();
            })
            .Subscribe(
                healthCounts =>
                {
                    _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 2 OnNext : " + healthCounts + " : " + healthCounts2.Value);
                    healthCounts2.IncrementAndGet();
                },
                e =>
                {
                    _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 2 OnError : " + e);
                    latch2.SignalEx();
                },
                () =>
                {
                    _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : Health 2 OnCompleted");
                    latch2.SignalEx();
                });

        // execute 5 commands, then unsubscribe from first stream. then execute the rest
        for (var i = 0; i < 10; i++)
        {
            HystrixCommand<int> cmd = Command.From(GroupKey, key, HystrixEventType.Success, 20);
            cmd.Execute();
            if (i == 5)
            {
                s1.Dispose();
            }
        }

        Assert.True(_stream.IsSourceCurrentlySubscribed);  // only 1/2 subscriptions has been cancelled

        Assert.True(latch1.Wait(10000));
        Assert.True(latch2.Wait(10000));
        _output.WriteLine("s1 got : " + healthCounts1.Value + ", s2 got : " + healthCounts2.Value);
        Assert.True(healthCounts1.Value >= 0);
        Assert.True(healthCounts2.Value > 0);
        Assert.True(healthCounts2.Value > healthCounts1.Value);

        s2.Dispose();
    }
}
