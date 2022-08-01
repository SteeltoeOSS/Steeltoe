// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Test;
using System.Reactive;
using System.Reactive.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Metric.Test;

public class HystrixThreadEventStreamTest : CommandStreamTest
{
    private sealed class LatchedObserver<T> : ObserverBase<T>
    {
        private readonly CountdownEvent _latch;

        public LatchedObserver(CountdownEvent latch)
        {
            _latch = latch;
        }

        protected override void OnCompletedCore()
        {
            _latch.SignalEx();
        }

        protected override void OnErrorCore(Exception error)
        {
            Assert.False(true, error.Message);
        }

        protected override void OnNextCore(T value)
        {
        }
    }

    private readonly IHystrixCommandKey _commandKey;
    private readonly IHystrixThreadPoolKey _threadPoolKey;

    private readonly HystrixThreadEventStream _writeToStream;
    private readonly HystrixCommandCompletionStream _readCommandStream;
    private readonly HystrixThreadPoolCompletionStream _readThreadPoolStream;
    private readonly ITestOutputHelper _output;

    public HystrixThreadEventStreamTest(ITestOutputHelper output)
    {
        _output = output;
        _commandKey = HystrixCommandKeyDefault.AsKey("CMD-ThreadStream");
        _threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("TP-ThreadStream");

        _writeToStream = HystrixThreadEventStream.GetInstance();
        _readCommandStream = HystrixCommandCompletionStream.GetInstance(_commandKey);
        _readThreadPoolStream = HystrixThreadPoolCompletionStream.GetInstance(_threadPoolKey);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void NoEvents()
    {
        var commandLatch = new CountdownEvent(1);
        var threadPoolLatch = new CountdownEvent(1);

        IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
        _readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

        IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
        _readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

        // no writes
        Assert.False(commandLatch.Wait(TimeSpan.FromMilliseconds(1000)));
        Assert.False(threadPoolLatch.Wait(TimeSpan.FromMilliseconds(1000)));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestThreadIsolatedSuccess()
    {
        var commandLatch = new CountdownEvent(1);
        var threadPoolLatch = new CountdownEvent(1);

        IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
        _readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

        IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
        _readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

        var result = ExecutionResult.From(HystrixEventType.Success).SetExecutedInThread();
        _writeToStream.ExecutionDone(result, _commandKey, _threadPoolKey);

        Assert.True(commandLatch.Wait(1000));
        Assert.True(threadPoolLatch.Wait(1000));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestSemaphoreIsolatedSuccess()
    {
        var commandLatch = new CountdownEvent(1);
        var threadPoolLatch = new CountdownEvent(1);

        IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
        _readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

        IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
        _readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

        var result = ExecutionResult.From(HystrixEventType.Success);
        _writeToStream.ExecutionDone(result, _commandKey, _threadPoolKey);

        Assert.True(commandLatch.Wait(1000));
        Assert.False(threadPoolLatch.Wait(1000));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestThreadIsolatedFailure()
    {
        var commandLatch = new CountdownEvent(1);
        var threadPoolLatch = new CountdownEvent(1);

        IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
        _readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

        IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
        _readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

        var result = ExecutionResult.From(HystrixEventType.Failure).SetExecutedInThread();
        _writeToStream.ExecutionDone(result, _commandKey, _threadPoolKey);

        Assert.True(commandLatch.Wait(1000));
        Assert.True(threadPoolLatch.Wait(1000));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestSemaphoreIsolatedFailure()
    {
        var commandLatch = new CountdownEvent(1);
        var threadPoolLatch = new CountdownEvent(1);

        IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
        _readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

        IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
        _readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

        var result = ExecutionResult.From(HystrixEventType.Failure);
        _writeToStream.ExecutionDone(result, _commandKey, _threadPoolKey);

        Assert.True(commandLatch.Wait(1000));
        Assert.False(threadPoolLatch.Wait(1000));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestThreadIsolatedTimeout()
    {
        var commandLatch = new CountdownEvent(1);
        var threadPoolLatch = new CountdownEvent(1);

        IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
        _readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

        IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
        _readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

        var result = ExecutionResult.From(HystrixEventType.Timeout).SetExecutedInThread();
        _writeToStream.ExecutionDone(result, _commandKey, _threadPoolKey);

        Assert.True(commandLatch.Wait(1000));
        Assert.True(threadPoolLatch.Wait(1000));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestSemaphoreIsolatedTimeout()
    {
        var commandLatch = new CountdownEvent(1);
        var threadPoolLatch = new CountdownEvent(1);

        IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
        _readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

        IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
        _readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

        var result = ExecutionResult.From(HystrixEventType.Timeout);
        _writeToStream.ExecutionDone(result, _commandKey, _threadPoolKey);

        Assert.True(commandLatch.Wait(1000));
        Assert.False(threadPoolLatch.Wait(1000));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestThreadIsolatedBadRequest()
    {
        var commandLatch = new CountdownEvent(1);
        var threadPoolLatch = new CountdownEvent(1);

        IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
        _readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

        IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
        _readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

        var result = ExecutionResult.From(HystrixEventType.BadRequest).SetExecutedInThread();
        _writeToStream.ExecutionDone(result, _commandKey, _threadPoolKey);

        Assert.True(commandLatch.Wait(1000));
        Assert.True(threadPoolLatch.Wait(1000));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestSemaphoreIsolatedBadRequest()
    {
        var commandLatch = new CountdownEvent(1);
        var threadPoolLatch = new CountdownEvent(1);

        IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
        _readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

        IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
        _readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

        var result = ExecutionResult.From(HystrixEventType.BadRequest);
        _writeToStream.ExecutionDone(result, _commandKey, _threadPoolKey);

        Assert.True(commandLatch.Wait(1000));
        Assert.False(threadPoolLatch.Wait(1000));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestThreadRejectedCommand()
    {
        var commandLatch = new CountdownEvent(1);
        var threadPoolLatch = new CountdownEvent(1);

        IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
        _readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

        IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
        _readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

        var result = ExecutionResult.From(HystrixEventType.ThreadPoolRejected);
        _writeToStream.ExecutionDone(result, _commandKey, _threadPoolKey);

        Assert.True(commandLatch.Wait(1000));
        Assert.True(threadPoolLatch.Wait(1000));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestSemaphoreRejectedCommand()
    {
        var commandLatch = new CountdownEvent(1);
        var threadPoolLatch = new CountdownEvent(1);

        IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
        _readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

        IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
        _readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

        var result = ExecutionResult.From(HystrixEventType.SemaphoreRejected);
        _writeToStream.ExecutionDone(result, _commandKey, _threadPoolKey);

        Assert.True(commandLatch.Wait(1000));
        Assert.False(threadPoolLatch.Wait(1000));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestThreadIsolatedResponseFromCache()
    {
        var commandLatch = new CountdownEvent(1);
        var threadPoolLatch = new CountdownEvent(1);

        IObserver<IList<HystrixCommandCompletion>> commandListSubscriber = new LatchedObserver<IList<HystrixCommandCompletion>>(commandLatch);
        _readCommandStream.Observe().Buffer(TimeSpan.FromMilliseconds(500)).Take(1)
            .Do(hystrixCommandCompletions =>
            {
                _output.WriteLine("LIST : " + hystrixCommandCompletions);
                Assert.Equal(3, hystrixCommandCompletions.Count);
            }).Subscribe(commandListSubscriber);

        IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
        _readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

        var result = ExecutionResult.From(HystrixEventType.Success).SetExecutedInThread();
        var cache1 = ExecutionResult.From(HystrixEventType.ResponseFromCache);
        var cache2 = ExecutionResult.From(HystrixEventType.ResponseFromCache);
        _writeToStream.ExecutionDone(result, _commandKey, _threadPoolKey);
        _writeToStream.ExecutionDone(cache1, _commandKey, _threadPoolKey);
        _writeToStream.ExecutionDone(cache2, _commandKey, _threadPoolKey);

        Assert.True(commandLatch.Wait(1000));
        Assert.True(threadPoolLatch.Wait(1000));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestSemaphoreIsolatedResponseFromCache()
    {
        var commandLatch = new CountdownEvent(1);
        var threadPoolLatch = new CountdownEvent(1);

        IObserver<IList<HystrixCommandCompletion>> commandListSubscriber = new LatchedObserver<IList<HystrixCommandCompletion>>(commandLatch);
        _readCommandStream.Observe().Buffer(TimeSpan.FromMilliseconds(500)).Take(1)
            .Do(hystrixCommandCompletions =>
            {
                _output.WriteLine("LIST : " + hystrixCommandCompletions);
                Assert.Equal(3, hystrixCommandCompletions.Count);
            })
            .Subscribe(commandListSubscriber);

        IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
        _readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

        var result = ExecutionResult.From(HystrixEventType.Success);
        var cache1 = ExecutionResult.From(HystrixEventType.ResponseFromCache);
        var cache2 = ExecutionResult.From(HystrixEventType.ResponseFromCache);
        _writeToStream.ExecutionDone(result, _commandKey, _threadPoolKey);
        _writeToStream.ExecutionDone(cache1, _commandKey, _threadPoolKey);
        _writeToStream.ExecutionDone(cache2, _commandKey, _threadPoolKey);

        Assert.True(commandLatch.Wait(1000));
        Assert.False(threadPoolLatch.Wait(1000));
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestShortCircuit()
    {
        var commandLatch = new CountdownEvent(1);
        var threadPoolLatch = new CountdownEvent(1);

        IObserver<HystrixCommandCompletion> commandSubscriber = new LatchedObserver<HystrixCommandCompletion>(commandLatch);
        _readCommandStream.Observe().Take(1).Subscribe(commandSubscriber);

        IObserver<HystrixCommandCompletion> threadPoolSubscriber = new LatchedObserver<HystrixCommandCompletion>(threadPoolLatch);
        _readThreadPoolStream.Observe().Take(1).Subscribe(threadPoolSubscriber);

        var result = ExecutionResult.From(HystrixEventType.ShortCircuited);
        _writeToStream.ExecutionDone(result, _commandKey, _threadPoolKey);

        Assert.True(commandLatch.Wait(1000));
        Assert.False(threadPoolLatch.Wait(1000));
    }
}
