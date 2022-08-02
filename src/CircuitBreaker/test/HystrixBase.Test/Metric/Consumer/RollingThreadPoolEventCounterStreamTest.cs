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

public class RollingThreadPoolEventCounterStreamTest : CommandStreamTest
{
    private readonly ITestOutputHelper _output;
    private RollingThreadPoolEventCounterStream _stream;
    private IDisposable _latchSubscription;

    public RollingThreadPoolEventCounterStreamTest(ITestOutputHelper output)
    {
        _output = output;
        HystrixThreadPoolCompletionStream.Reset();
        RollingThreadPoolEventCounterStream.Reset();
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestEmptyStreamProducesZeros()
    {
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-A");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.Executed) + _stream.GetLatestCount(ThreadPoolEventType.Rejected));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSingleSuccess()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-B");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-B");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-B");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        Command cmd = Command.From(groupKey, key, HystrixEventType.Success, 20);

        await cmd.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(1, _stream.GetLatestCount(ThreadPoolEventType.Executed));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.Rejected));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSingleFailure()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-C");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-C");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-C");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        Command cmd = Command.From(groupKey, key, HystrixEventType.Failure, 20);

        await cmd.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(1, _stream.GetLatestCount(ThreadPoolEventType.Executed));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.Rejected));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSingleTimeout()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-D");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-D");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-D");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        Command cmd = Command.From(groupKey, key, HystrixEventType.Timeout);

        await cmd.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(1, _stream.GetLatestCount(ThreadPoolEventType.Executed));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.Rejected));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSingleBadRequest()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-E");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-E");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-E");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        Command cmd = Command.From(groupKey, key, HystrixEventType.BadRequest);

        await Assert.ThrowsAsync<HystrixBadRequestException>(async () => await cmd.Observe());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(1, _stream.GetLatestCount(ThreadPoolEventType.Executed));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.Rejected));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestRequestFromCache()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-F");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-F");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-F");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        Command cmd1 = Command.From(groupKey, key, HystrixEventType.Success, 0);
        Command cmd2 = Command.From(groupKey, key, HystrixEventType.ResponseFromCache);
        Command cmd3 = Command.From(groupKey, key, HystrixEventType.ResponseFromCache);

        await cmd1.Observe();
        await cmd2.Observe();
        await cmd3.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        // RESPONSE_FROM_CACHE should not show up at all in thread pool counters - just the success
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(1, _stream.GetLatestCount(ThreadPoolEventType.Executed));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.Rejected));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestShortCircuited()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-G");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-G");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-G");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
        // should see 3 FAILUREs and 2 SHORT_CIRCUITs and each should see a FALLBACK_SUCCESS
        Command failure1 = Command.From(groupKey, key, HystrixEventType.Failure, 0);
        Command failure2 = Command.From(groupKey, key, HystrixEventType.Failure, 0);
        Command failure3 = Command.From(groupKey, key, HystrixEventType.Failure, 0);

        Command shortCircuit1 = Command.From(groupKey, key, HystrixEventType.Success);
        Command shortCircuit2 = Command.From(groupKey, key, HystrixEventType.Success);

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
        Assert.Equal(3, _stream.GetLatestCount(ThreadPoolEventType.Executed));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.Rejected));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSemaphoreRejected()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-H");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-H");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-H");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        // 10 commands will saturate semaphore when called from different threads.
        // submit 2 more requests and they should be SEMAPHORE_REJECTED
        // should see 10 SUCCESSes, 2 SEMAPHORE_REJECTED and 2 FALLBACK_SUCCESSes
        var saturators = new List<Command>();

        for (int i = 0; i < 10; i++)
        {
            saturators.Add(Command.From(groupKey, key, HystrixEventType.Success, 500, ExecutionIsolationStrategy.Semaphore));
        }

        Command rejected1 = Command.From(groupKey, key, HystrixEventType.Success, 0, ExecutionIsolationStrategy.Semaphore);
        Command rejected2 = Command.From(groupKey, key, HystrixEventType.Success, 0, ExecutionIsolationStrategy.Semaphore);

        var tasks = new List<Task>();

        foreach (Command saturator in saturators)
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
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.Executed));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.Rejected));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestThreadPoolRejected()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-I");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-I");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-I");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        // 10 commands will saturate threadpools when called concurrently.
        // submit 2 more requests and they should be THREADPOOL_REJECTED
        // should see 10 SUCCESSes, 2 THREADPOOL_REJECTED and 2 FALLBACK_SUCCESSes
        var saturators = new List<Command>();

        for (int i = 0; i < 10; i++)
        {
            saturators.Add(Command.From(groupKey, key, HystrixEventType.Success, 500));
        }

        Command rejected1 = Command.From(groupKey, key, HystrixEventType.Success, 0);
        Command rejected2 = Command.From(groupKey, key, HystrixEventType.Success, 0);

        var tasks = new List<Task>();

        foreach (Command saturator in saturators)
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
        Assert.Equal(10, _stream.GetLatestCount(ThreadPoolEventType.Executed));
        Assert.Equal(2, _stream.GetLatestCount(ThreadPoolEventType.Rejected));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestFallbackFailure()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-J");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-J");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-J");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        Command cmd = Command.From(groupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackFailure);

        await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(1, _stream.GetLatestCount(ThreadPoolEventType.Executed));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.Rejected));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestFallbackMissing()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-K");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-K");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-K");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        Command cmd = Command.From(groupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackMissing);

        await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(1, _stream.GetLatestCount(ThreadPoolEventType.Executed));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.Rejected));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestFallbackRejection()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-L");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-L");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-L");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        // fallback semaphore size is 5.  So let 5 commands saturate that semaphore, then
        // let 2 more commands go to fallback.  they should get rejected by the fallback-semaphore
        var fallbackSaturators = new List<Command>();

        for (int i = 0; i < 5; i++)
        {
            fallbackSaturators.Add(Command.From(groupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackSuccess, 500));
        }

        Command rejection1 = Command.From(groupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackSuccess, 0);
        Command rejection2 = Command.From(groupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackSuccess, 0);

        var tasks = new List<Task>();

        foreach (Command saturator in fallbackSaturators)
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
        Assert.Equal(7, _stream.GetLatestCount(ThreadPoolEventType.Executed));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.Rejected));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestMultipleEventsOverTimeGetStoredAndAgeOut()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-M");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-M");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingCounter-M");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolEventCounterStream.GetInstance(threadPoolKey, 10, 250);
        _latchSubscription = _stream.Observe().Take(20 + LatchedObserver.StableTickCount).Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        Command cmd1 = Command.From(groupKey, key, HystrixEventType.Success, 20);
        Command cmd2 = Command.From(groupKey, key, HystrixEventType.Failure, 10);

        await cmd1.Observe();
        await cmd2.Observe();

        Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());

        // all commands should have aged out
        Assert.Equal(2, _stream.Latest.Length);
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.Executed));
        Assert.Equal(0, _stream.GetLatestCount(ThreadPoolEventType.Rejected));
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
