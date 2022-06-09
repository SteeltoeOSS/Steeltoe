// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
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

public class RollingThreadPoolEventCounterStreamTest : CommandStreamTest
{
    private readonly ITestOutputHelper _output;
    private RollingThreadPoolEventCounterStream _stream;
    private IDisposable _latchSubscription;

    private sealed class LatchedObserver : TestObserverBase<long[]>
    {
        public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
            : base(output, latch)
        {
        }
    }

    public RollingThreadPoolEventCounterStreamTest(ITestOutputHelper output)
    {
        _output = output;
        HystrixThreadPoolCompletionStream.Reset();
        RollingThreadPoolEventCounterStream.Reset();
    }

    public override void Dispose()
    {
        _latchSubscription?.Dispose();
        _stream?.Unsubscribe();
        _latchSubscription = null;
        _stream = null;
        base.Dispose();
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestEmptyStreamProducesZeros()
    {
        var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-A");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.EXECUTED) + _stream.GetLatestCount(ThreadPoolEventType.REJECTED));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSingleSuccess()
    {
        var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-B");
        var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-B");
        var key = HystrixCommandKeyDefault.AsKey("RollingCounter-B");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        var cmd = Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);

        await cmd.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(1, _stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.REJECTED));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSingleFailure()
    {
        var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-C");
        var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-C");
        var key = HystrixCommandKeyDefault.AsKey("RollingCounter-C");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        var cmd = Command.From(groupKey, key, HystrixEventType.FAILURE, 20);

        await cmd.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(1, _stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.REJECTED));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSingleTimeout()
    {
        var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-D");
        var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-D");
        var key = HystrixCommandKeyDefault.AsKey("RollingCounter-D");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        var cmd = Command.From(groupKey, key, HystrixEventType.TIMEOUT);

        await cmd.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(1, _stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.REJECTED));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSingleBadRequest()
    {
        var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-E");
        var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-E");
        var key = HystrixCommandKeyDefault.AsKey("RollingCounter-E");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        var cmd = Command.From(groupKey, key, HystrixEventType.BAD_REQUEST);

        await Assert.ThrowsAsync<HystrixBadRequestException>(async () => await cmd.Observe());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(1, _stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.REJECTED));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestRequestFromCache()
    {
        var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-F");
        var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-F");
        var key = HystrixCommandKeyDefault.AsKey("RollingCounter-F");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        var cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0);
        var cmd2 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);
        var cmd3 = Command.From(groupKey, key, HystrixEventType.RESPONSE_FROM_CACHE);

        await cmd1.Observe();
        await cmd2.Observe();
        await cmd3.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        // RESPONSE_FROM_CACHE should not show up at all in thread pool counters - just the success
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(1, _stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.REJECTED));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestShortCircuited()
    {
        var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-G");
        var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-G");
        var key = HystrixCommandKeyDefault.AsKey("RollingCounter-G");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
        // should see 3 FAILUREs and 2 SHORT_CIRCUITs and each should see a FALLBACK_SUCCESS
        var failure1 = Command.From(groupKey, key, HystrixEventType.FAILURE, 0);
        var failure2 = Command.From(groupKey, key, HystrixEventType.FAILURE, 0);
        var failure3 = Command.From(groupKey, key, HystrixEventType.FAILURE, 0);

        var shortCircuit1 = Command.From(groupKey, key, HystrixEventType.SUCCESS);
        var shortCircuit2 = Command.From(groupKey, key, HystrixEventType.SUCCESS);

        await failure1.Observe();
        await failure2.Observe();
        await failure3.Observe();

        Assert.True(WaitForHealthCountToUpdate(key.Name, 500, _output), "Health count stream update took to long");

        await shortCircuit1.Observe();
        await shortCircuit2.Observe();

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.True(shortCircuit1.IsResponseShortCircuited);
        Assert.True(shortCircuit2.IsResponseShortCircuited);

        // only the FAILUREs should show up in thread pool counters
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(3, _stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.REJECTED));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSemaphoreRejected()
    {
        var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-H");
        var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-H");
        var key = HystrixCommandKeyDefault.AsKey("RollingCounter-H");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        // 10 commands will saturate semaphore when called from different threads.
        // submit 2 more requests and they should be SEMAPHORE_REJECTED
        // should see 10 SUCCESSes, 2 SEMAPHORE_REJECTED and 2 FALLBACK_SUCCESSes
        var saturators = new List<Command>();

        for (var i = 0; i < 10; i++)
        {
            saturators.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 500, ExecutionIsolationStrategy.SEMAPHORE));
        }

        var rejected1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);
        var rejected2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0, ExecutionIsolationStrategy.SEMAPHORE);

        var tasks = new List<Task>();
        foreach (var saturator in saturators)
        {
            tasks.Add(Task.Run(() => saturator.Execute()));
        }

        await Task.Delay(50);

        await Task.Run(() => rejected1.Execute());
        await Task.Run(() => rejected2.Execute());

        Task.WaitAll(tasks.ToArray());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.True(rejected1.IsResponseSemaphoreRejected, "rejected1 not rejected");
        Assert.True(rejected2.IsResponseSemaphoreRejected, "rejected2 not rejected");

        // none of these got executed on a thread-pool, so thread pool metrics should be 0
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.REJECTED));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestThreadPoolRejected()
    {
        var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-I");
        var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-I");
        var key = HystrixCommandKeyDefault.AsKey("RollingCounter-I");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        // 10 commands will saturate threadpools when called concurrently.
        // submit 2 more requests and they should be THREADPOOL_REJECTED
        // should see 10 SUCCESSes, 2 THREADPOOL_REJECTED and 2 FALLBACK_SUCCESSes
        var saturators = new List<Command>();

        for (var i = 0; i < 10; i++)
        {
            saturators.Add(Command.From(groupKey, key, HystrixEventType.SUCCESS, 500));
        }

        var rejected1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0);
        var rejected2 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 0);

        var tasks = new List<Task>();
        foreach (var saturator in saturators)
        {
            tasks.Add(saturator.ExecuteAsync());
        }

        await Task.Delay(50);

        await rejected1.Observe();
        await rejected2.Observe();

        Task.WaitAll(tasks.ToArray());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.True(rejected1.IsResponseThreadPoolRejected, "Command1 IsResponseThreadPoolRejected");
        Assert.True(rejected2.IsResponseThreadPoolRejected, "Command2 IsResponseThreadPoolRejected");

        // none of these got executed on a thread-pool, so thread pool metrics should be 0
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(10, _stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
        Assert.Equal(2, _stream.GetLatestCount(ThreadPoolEventType.REJECTED));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestFallbackFailure()
    {
        var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-J");
        var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-J");
        var key = HystrixCommandKeyDefault.AsKey("RollingCounter-J");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        var cmd = Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_FAILURE);

        await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(1, _stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.REJECTED));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestFallbackMissing()
    {
        var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-K");
        var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-K");
        var key = HystrixCommandKeyDefault.AsKey("RollingCounter-K");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        var cmd = Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_MISSING);

        await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(1, _stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.REJECTED));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestFallbackRejection()
    {
        var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-L");
        var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-L");
        var key = HystrixCommandKeyDefault.AsKey("RollingCounter-L");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        // fallback semaphore size is 5.  So let 5 commands saturate that semaphore, then
        // let 2 more commands go to fallback.  they should get rejected by the fallback-semaphore
        var fallbackSaturators = new List<Command>();
        for (var i = 0; i < 5; i++)
        {
            fallbackSaturators.Add(Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 500));
        }

        var rejection1 = Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);
        var rejection2 = Command.From(groupKey, key, HystrixEventType.FAILURE, 0, HystrixEventType.FALLBACK_SUCCESS, 0);

        var tasks = new List<Task>();
        foreach (var saturator in fallbackSaturators)
        {
            tasks.Add(saturator.ExecuteAsync());
        }

        await Task.Delay(50);

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

        await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection1.Observe());
        await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await rejection2.Observe());

        Task.WaitAll(tasks.ToArray());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(7, _stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.REJECTED));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestMultipleEventsOverTimeGetStoredAndAgeOut()
    {
        var groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-M");
        var threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-M");
        var key = HystrixCommandKeyDefault.AsKey("RollingCounter-M");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 250);
        _latchSubscription = _stream.Observe().Take(20 + LatchedObserver.STABLE_TICK_COUNT).Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        var cmd1 = Command.From(groupKey, key, HystrixEventType.SUCCESS, 20);
        var cmd2 = Command.From(groupKey, key, HystrixEventType.FAILURE, 10);

        await cmd1.Observe();
        await cmd2.Observe();

        Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

        // all commands should have aged out
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.EXECUTED));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.REJECTED));
    }
}
