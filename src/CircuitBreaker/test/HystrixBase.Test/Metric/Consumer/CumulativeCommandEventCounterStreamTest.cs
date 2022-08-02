// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reactive.Linq;
using Steeltoe.CircuitBreaker.Hystrix.Exceptions;
using Steeltoe.CircuitBreaker.Hystrix.Metric.Test;
using Steeltoe.CircuitBreaker.Hystrix.Test;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Consumer.Test;

public class CumulativeCommandEventCounterStreamTest : CommandStreamTest
{
    private static readonly IHystrixCommandGroupKey GroupKey = HystrixCommandGroupKeyDefault.AsKey("CumulativeCommandCounter");
    private readonly ITestOutputHelper _output;
    private CumulativeCommandEventCounterStream _stream;
    private IDisposable _latchSubscription;

    public CumulativeCommandEventCounterStreamTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestEmptyStreamProducesZeros()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-A");

        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        Assert.False(HasData(_stream.Latest));
    }

    [Fact]
    public async Task TestSingleSuccess()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-B");

        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500); // Stream should start
        Command cmd = Command.From(GroupKey, key, HystrixEventType.Success, 0);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        await cmd.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Success] = 1;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public async Task TestSingleFailure()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-C");

        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
        Command cmd = Command.From(GroupKey, key, HystrixEventType.Failure, 0);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        await cmd.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Failure] = 1;
        expected[(int)HystrixEventType.FallbackSuccess] = 1;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public async Task TestSingleTimeout()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-D");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
        Command cmd = Command.From(GroupKey, key, HystrixEventType.Timeout);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        await cmd.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Timeout] = 1;
        expected[(int)HystrixEventType.FallbackSuccess] = 1;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public async Task TestSingleBadRequest()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-E");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
        Command cmd = Command.From(GroupKey, key, HystrixEventType.BadRequest);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        await Assert.ThrowsAsync<HystrixBadRequestException>(async () => await cmd.Observe());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.BadRequest] = 1;
        expected[(int)HystrixEventType.ExceptionThrown] = 1;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public async Task TestRequestFromCache()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-F");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
        Command cmd1 = Command.From(GroupKey, key, HystrixEventType.Success, 0);
        Command cmd2 = Command.From(GroupKey, key, HystrixEventType.ResponseFromCache);
        Command cmd3 = Command.From(GroupKey, key, HystrixEventType.ResponseFromCache);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        await cmd1.Observe();
        await cmd2.Observe();
        await cmd3.Observe();
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Success] = 1;
        expected[(int)HystrixEventType.ResponseFromCache] = 2;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public void TestShortCircuited()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-G");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);

        Command failure1 = Command.From(GroupKey, key, HystrixEventType.Failure, 0);
        Command failure2 = Command.From(GroupKey, key, HystrixEventType.Failure, 0);
        Command failure3 = Command.From(GroupKey, key, HystrixEventType.Failure, 0);
        Command shortCircuit1 = Command.From(GroupKey, key, HystrixEventType.Success);
        Command shortCircuit2 = Command.From(GroupKey, key, HystrixEventType.Success);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        // 3 failures in a row will trip circuit.  let bucket roll once then submit 2 requests.
        // should see 3 FAILUREs and 2 SHORT_CIRCUITs and then 5 FALLBACK_SUCCESSes
        failure1.Execute();
        failure2.Execute();
        failure3.Execute();

        Assert.True(WaitForHealthCountToUpdate(key.Name, 500, _output), "health count took to long to update");

        _output.WriteLine(Time.CurrentTimeMillis + " running failures");
        shortCircuit1.Execute();
        shortCircuit2.Execute();

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");
        Assert.True(shortCircuit1.IsResponseShortCircuited);
        Assert.True(shortCircuit2.IsResponseShortCircuited);
        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Failure] = 3;
        expected[(int)HystrixEventType.ShortCircuited] = 2;
        expected[(int)HystrixEventType.FallbackSuccess] = 5;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public async Task TestSemaphoreRejected()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-H");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        var saturators = new List<Command>();
        _stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);

        for (int i = 0; i < 10; i++)
        {
            saturators.Add(Command.From(GroupKey, key, HystrixEventType.Success, 500, ExecutionIsolationStrategy.Semaphore));
        }

        Command rejected1 = Command.From(GroupKey, key, HystrixEventType.Success, 0, ExecutionIsolationStrategy.Semaphore);
        Command rejected2 = Command.From(GroupKey, key, HystrixEventType.Success, 0, ExecutionIsolationStrategy.Semaphore);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        // 10 commands will saturate semaphore when called From different threads.
        // submit 2 more requests and they should be SEMAPHORE_REJECTED
        // should see 10 SUCCESSes, 2 SEMAPHORE_REJECTED and 2 FALLBACK_SUCCESSes
        var tasks = new List<Task>();

        foreach (Command c in saturators)
        {
            tasks.Add(Task.Run(() => c.Execute()));
        }

        await Task.Delay(50);

        tasks.Add(Task.Run(() => rejected1.Execute()));
        tasks.Add(Task.Run(() => rejected2.Execute()));

        Task.WaitAll(tasks.ToArray());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.True(rejected1.IsResponseSemaphoreRejected, "rejected1 not rejected");
        Assert.True(rejected2.IsResponseSemaphoreRejected, "rejected2 not rejected");
        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Success] = 10;
        expected[(int)HystrixEventType.SemaphoreRejected] = 2;
        expected[(int)HystrixEventType.FallbackSuccess] = 2;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public void TestThreadPoolRejected()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-I");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        var saturators = new List<Command>();
        _stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);

        for (int i = 0; i < 10; i++)
        {
            saturators.Add(Command.From(GroupKey, key, HystrixEventType.Success, 500));
        }

        Command rejected1 = Command.From(GroupKey, key, HystrixEventType.Success, 0);
        Command rejected2 = Command.From(GroupKey, key, HystrixEventType.Success, 0);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        // 10 commands will saturate threadpools when called concurrently.
        // submit 2 more requests and they should be THREADPOOL_REJECTED
        // should see 10 SUCCESSes, 2 THREADPOOL_REJECTED and 2 FALLBACK_SUCCESSes
        var tasks = new List<Task>();

        foreach (Command saturator in saturators)
        {
            tasks.Add(saturator.ExecuteAsync());
        }

        tasks.Add(rejected1.ExecuteAsync());
        tasks.Add(rejected2.ExecuteAsync());

        Task.WaitAll(tasks.ToArray());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.True(rejected1.IsResponseThreadPoolRejected);
        Assert.True(rejected2.IsResponseThreadPoolRejected);
        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Success] = 10;
        expected[(int)HystrixEventType.ThreadPoolRejected] = 2;
        expected[(int)HystrixEventType.FallbackSuccess] = 2;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public async Task TestFallbackFailure()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-J");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
        Command cmd = Command.From(GroupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackFailure);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Failure] = 1;
        expected[(int)HystrixEventType.FallbackFailure] = 1;
        expected[(int)HystrixEventType.ExceptionThrown] = 1;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public async Task TestFallbackMissing()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-K");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
        Command cmd = Command.From(GroupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackMissing);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        await Assert.ThrowsAsync<HystrixRuntimeException>(async () => await cmd.Observe());

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Failure] = 1;
        expected[(int)HystrixEventType.FallbackMissing] = 1;
        expected[(int)HystrixEventType.ExceptionThrown] = 1;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public async Task TestFallbackRejection()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-L");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        var fallbackSaturators = new List<Command>();

        _stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);

        for (int i = 0; i < 5; i++)
        {
            fallbackSaturators.Add(Command.From(GroupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackSuccess, 500));
        }

        Command rejection1 = Command.From(GroupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackSuccess, 0);
        Command rejection2 = Command.From(GroupKey, key, HystrixEventType.Failure, 0, HystrixEventType.FallbackSuccess, 0);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        // fallback semaphore size is 5.  So let 5 commands saturate that semaphore, then
        // let 2 more commands go to fallback.  they should get rejected by the fallback-semaphore
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

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Failure] = 7;
        expected[(int)HystrixEventType.FallbackSuccess] = 5;
        expected[(int)HystrixEventType.FallbackRejection] = 2;
        expected[(int)HystrixEventType.ExceptionThrown] = 2;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public void TestCancelled()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-M");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);
        Command toCancel = Command.From(GroupKey, key, HystrixEventType.Success, 500);

        _latchSubscription = _stream.Observe().Take(5 + LatchedObserver.StableTickCount).Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : about to Observe and Subscribe");

        IDisposable s = toCancel.Observe().OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " : UnSubscribe From command.Observe()");
        }).Subscribe(i =>
        {
            _output.WriteLine("Command OnNext : " + i);
        }, e =>
        {
            _output.WriteLine("Command OnError : " + e);
        }, () =>
        {
            _output.WriteLine("Command OnCompleted");
        });

        _output.WriteLine(Time.CurrentTimeMillis + " : " + Task.CurrentId + " : about to unSubscribe");
        s.Dispose();

        Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Cancelled] = 1;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public void TestCollapsed()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("BatchCommand");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);
        _stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 500);

        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 2000), "Stream failed to start");

        var tasks = new List<Task>();

        for (int i = 0; i < 3; i++)
        {
            tasks.Add(Collapser.From(_output, i).ExecuteAsync());
        }

        Task.WaitAll(tasks.ToArray());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 2000, _output), "Latch took to long to update");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Success] = 1;
        expected[(int)HystrixEventType.Collapsed] = 3;
        Assert.Equal(expected, _stream.Latest);
    }

    [Fact]
    public async Task TestMultipleEventsOverTimeGetStoredAndNeverAgeOut()
    {
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("CMD-CumulativeCounter-N");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = CumulativeCommandEventCounterStream.GetInstance(key, 10, 100);
        Command cmd1 = Command.From(GroupKey, key, HystrixEventType.Success, 20);
        Command cmd2 = Command.From(GroupKey, key, HystrixEventType.Failure, 10);

        // by doing a Take(30), we ensure that no rolling out of window takes place
        _latchSubscription = _stream.Observe().Take(30 + LatchedObserver.StableTickCount).Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        await cmd1.Observe();
        await cmd2.Observe();

        Assert.True(latch.Wait(10000), "CountdownEvent was not set!");

        Assert.Equal(HystrixEventTypeHelper.Values.Count, _stream.Latest.Length);
        long[] expected = new long[HystrixEventTypeHelper.Values.Count];
        expected[(int)HystrixEventType.Success] = 1;
        expected[(int)HystrixEventType.Failure] = 1;
        expected[(int)HystrixEventType.FallbackSuccess] = 1;
        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
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
