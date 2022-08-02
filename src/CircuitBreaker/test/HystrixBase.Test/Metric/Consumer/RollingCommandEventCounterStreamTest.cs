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

public class RollingCommandEventCounterStreamTest : CommandStreamTest
{
    private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("RollingCommandCounter");
    private readonly ITestOutputHelper _output;
    private RollingCommandEventCounterStream _stream;
    private IDisposable _latchSubscription;

    public RollingCommandEventCounterStreamTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestEmptyStreamProducesZeros()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-A");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        Assert.False(HasData(_stream.Latest), "Stream has events when it should not");
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSingleSuccess()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-B");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd = Command.From(GroupKey, key, HystrixEventType.Success, 20);

        await cmd.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Success] = 1;
        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSingleFailure()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-C");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd = Command.From(GroupKey, key, HystrixEventType.Failure, 20);

        await cmd.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Failure] = 1;
        expected[(int)HystrixEventType.FallbackSuccess] = 1;
        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSingleTimeout()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-D");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Timeout] = 1;
        expected[(int)HystrixEventType.FallbackSuccess] = 1;

        _stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd = Command.From(GroupKey, key, HystrixEventType.Timeout);
        await cmd.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSingleBadRequest()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-E");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd = Command.From(GroupKey, key, HystrixEventType.BadRequest);

        await Assert.ThrowsAsync<HystrixBadRequestException>(async () => await cmd.Observe());

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.BadRequest] = 1;
        expected[(int)HystrixEventType.ExceptionThrown] = 1;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestRequestFromCache()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-F");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd1 = Command.From(GroupKey, key, HystrixEventType.Success, 20);
        Command cmd2 = Command.From(GroupKey, key, HystrixEventType.ResponseFromCache);
        Command cmd3 = Command.From(GroupKey, key, HystrixEventType.ResponseFromCache);

        await cmd1.Observe();
        await cmd2.Observe();
        await cmd3.Observe();

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Success] = 1;
        expected[(int)HystrixEventType.ResponseFromCache] = 2;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public async Task TestShortCircuited()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-G");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
        // should see 3 FAILUREs and 2 SHORT_CIRCUITs and then 5 FALLBACK_SUCCESSes
        Command failure1 = Command.From(GroupKey, key, HystrixEventType.Failure, 0);
        Command failure2 = Command.From(GroupKey, key, HystrixEventType.Failure, 0);
        Command failure3 = Command.From(GroupKey, key, HystrixEventType.Failure, 0);

        Command shortCircuit1 = Command.From(GroupKey, key, HystrixEventType.Success);
        Command shortCircuit2 = Command.From(GroupKey, key, HystrixEventType.Success);

        await failure1.Observe();
        await failure2.Observe();
        await failure3.Observe();

        Assert.True(WaitForHealthCountToUpdate(key.Name, 500, _output), "health count took to long to update");

        _output.WriteLine(Time.CurrentTimeMillis + " running failures");

        await shortCircuit1.Observe();
        await shortCircuit2.Observe();

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.True(shortCircuit1.IsResponseShortCircuited, "Circuit 1 not shorted as was expected");
        Assert.True(shortCircuit2.IsResponseShortCircuited, "Circuit 2 not shorted as was expected");
        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);

        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Failure] = 3;
        expected[(int)HystrixEventType.ShortCircuited] = 2;
        expected[(int)HystrixEventType.FallbackSuccess] = 5;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestSemaphoreRejected()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-H");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // 10 commands will saturate semaphore when called from different threads.
        // submit 2 more requests and they should be SEMAPHORE_REJECTED
        // should see 10 SUCCESSes, 2 SEMAPHORE_REJECTED and 2 FALLBACK_SUCCESSes
        var saturators = new List<Command>();

        for (int i = 0; i < 10; i++)
        {
            saturators.Add(Command.From(GroupKey, key, HystrixEventType.Success, 500, ExecutionIsolationStrategy.Semaphore));
        }

        Command rejected1 = Command.From(GroupKey, key, HystrixEventType.Success, 0, ExecutionIsolationStrategy.Semaphore);
        Command rejected2 = Command.From(GroupKey, key, HystrixEventType.Success, 0, ExecutionIsolationStrategy.Semaphore);

        var tasks = new List<Task>();

        foreach (Command saturator in saturators)
        {
            tasks.Add(Task.Run(() => saturator.Execute()));
        }

        await Task.Delay(50);

        await Task.Run(() => rejected1.Execute());
        await Task.Run(() => rejected2.Execute());

        Task.WaitAll(tasks.ToArray());

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.True(rejected1.IsResponseSemaphoreRejected, "Response not semaphore rejected as was expected (1)");
        Assert.True(rejected2.IsResponseSemaphoreRejected, "Response not semaphore rejected as was expected (2)");
        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);

        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Success] = 10;
        expected[(int)HystrixEventType.SemaphoreRejected] = 2;
        expected[(int)HystrixEventType.FallbackSuccess] = 2;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestThreadPoolRejected()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-I");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // 10 commands will saturate threadpools when called concurrently.
        // submit 2 more requests and they should be THREADPOOL_REJECTED
        // should see 10 SUCCESSes, 2 THREADPOOL_REJECTED and 2 FALLBACK_SUCCESSes
        var saturators = new List<Command>();

        for (int i = 0; i < 10; i++)
        {
            saturators.Add(Command.From(GroupKey, key, HystrixEventType.Success, 500));
        }

        Command rejected1 = Command.From(GroupKey, key, HystrixEventType.Success, 0);
        Command rejected2 = Command.From(GroupKey, key, HystrixEventType.Success, 0);

        var tasks = new List<Task>();

        foreach (Command saturator in saturators)
        {
            tasks.Add(saturator.ExecuteAsync());
        }

        await rejected1.Observe();
        await rejected2.Observe();

        Task.WaitAll(tasks.ToArray());

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.True(rejected1.IsResponseThreadPoolRejected, "Not ThreadPoolRejected as was expected (1)");
        Assert.True(rejected2.IsResponseThreadPoolRejected, "Not ThreadPoolRejected as was expected (2)");
        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);

        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Success] = 10;
        expected[(int)HystrixEventType.ThreadPoolRejected] = 2;
        expected[(int)HystrixEventType.FallbackSuccess] = 2;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestFallbackFailure()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-J");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd = Command.From(GroupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackFailure);

        await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Failure] = 1;
        expected[(int)HystrixEventType.FallbackFailure] = 1;
        expected[(int)HystrixEventType.ExceptionThrown] = 1;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestFallbackMissing()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-K");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd = Command.From(GroupKey, key, HystrixEventType.Failure, 20, HystrixEventType.FallbackMissing);
        await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Failure] = 1;
        expected[(int)HystrixEventType.FallbackMissing] = 1;
        expected[(int)HystrixEventType.ExceptionThrown] = 1;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestFallbackRejection()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-L");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // fallback semaphore size is 5.  So let 5 commands saturate that semaphore, then
        // let 2 more commands go to fallback.  they should get rejected by the fallback-semaphore
        var fallbackSaturators = new List<Command>();

        for (int i = 0; i < 5; i++)
        {
            fallbackSaturators.Add(Command.From(GroupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackSuccess, 500));
        }

        Command rejection1 = Command.From(GroupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackSuccess, 0);
        Command rejection2 = Command.From(GroupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackSuccess, 0);

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
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Failure] = 7;
        expected[(int)HystrixEventType.FallbackSuccess] = 5;
        expected[(int)HystrixEventType.FallbackRejection] = 2;
        expected[(int)HystrixEventType.ExceptionThrown] = 2;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestCollapsed()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("BatchCommand");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        var tasks = new List<Task>();

        for (int i = 0; i < 3; i++)
        {
            tasks.Add(Collapser.From(_output, i).ExecuteAsync());
        }

        Task.WaitAll(tasks.ToArray());

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Success] = 1;
        expected[(int)HystrixEventType.Collapsed] = 3;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestMultipleEventsOverTimeGetStoredAndAgeOut()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-RollingCounter-M");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingCommandEventCounterStream.GetInstance(key, 10, 100);
        _latchSubscription = _stream.Observe().Take(30 + LatchedObserver.StableTickCount).Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd1 = Command.From(GroupKey, key, HystrixEventType.Success, 20);
        Command cmd2 = Command.From(GroupKey, key, HystrixEventType.Failure, 10);

        await cmd1.Observe();
        await cmd2.Observe();
        Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
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
