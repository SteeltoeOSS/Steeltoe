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

public class RollingThreadPoolMaxConcurrencyStreamTest : CommandStreamTest
{
    private readonly ITestOutputHelper _output;
    private RollingThreadPoolMaxConcurrencyStream _stream;
    private IDisposable _latchSubscription;

    public RollingThreadPoolMaxConcurrencyStreamTest(ITestOutputHelper output)
    {
        _output = output;

        HystrixThreadPoolStartStream.Reset();
        RollingThreadPoolMaxConcurrencyStream.Reset();
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestEmptyStreamProducesZeros()
    {
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-A");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        Assert.Equal(0, _stream.LatestRollingMax);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestStartsAndEndsInSameBucketProduceValue()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-B");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-B");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-B");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd1 = Command.From(groupKey, key, HystrixEventType.Success, 50);
        Command cmd2 = Command.From(groupKey, key, HystrixEventType.Success, 40);

        Task t1 = cmd1.ExecuteAsync();
        Task t2 = cmd2.ExecuteAsync();

        Task.WaitAll(t1, t2);
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.Equal(2, _stream.LatestRollingMax);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestStartsAndEndsInSameBucketSemaphoreIsolated()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-C");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-C");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-C");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd1 = Command.From(groupKey, key, HystrixEventType.Success, 10, ExecutionIsolationStrategy.Semaphore);
        Command cmd2 = Command.From(groupKey, key, HystrixEventType.Success, 14, ExecutionIsolationStrategy.Semaphore);

        Task t1 = cmd1.ExecuteAsync();
        Task t2 = cmd2.ExecuteAsync();

        Task.WaitAll(t1, t2);
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        // since commands run in semaphore isolation, they are not tracked by threadpool metrics
        Assert.Equal(0, _stream.LatestRollingMax);
    }

    /*
     * 3 Commands,
     * Command 1 gets started in Bucket A and not completed until Bucket B
     * Commands 2 and 3 both start and end in Bucket B, and there should be a max-concurrency of 3
     */
    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestOneCommandCarriesOverToNextBucket()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-D");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-D");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-D");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd1 = Command.From(groupKey, key, HystrixEventType.Success, 560);
        Command cmd2 = Command.From(groupKey, key, HystrixEventType.Success, 50);
        Command cmd3 = Command.From(groupKey, key, HystrixEventType.Success, 75);

        Task t1 = cmd1.ExecuteAsync();

        // Time.Wait(150); // bucket roll
        Assert.True(WaitForObservableToUpdate(_stream.Observe(), 1, 500, _output), "Stream update took to long");
        Task t2 = cmd2.ExecuteAsync();
        Task t3 = cmd3.ExecuteAsync();

        Task.WaitAll(t1, t2, t3);
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.Equal(3, _stream.LatestRollingMax);
    }

    // BUCKETS
    //      A    |    B    |    C    |    D    |    E    |
    //  1:  [-------------------------------]
    //  2:          [-------------------------------]
    //  3:                      [--]
    //  4:                              [--]
    //  Max concurrency should be 3
    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestMultipleCommandsCarryOverMultipleBuckets()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-E");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-E");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-E");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd1 = Command.From(groupKey, key, HystrixEventType.Success, 300);
        Command cmd2 = Command.From(groupKey, key, HystrixEventType.Success, 300);
        Command cmd3 = Command.From(groupKey, key, HystrixEventType.Success, 10);
        Command cmd4 = Command.From(groupKey, key, HystrixEventType.Success, 10);

        Task t1 = cmd1.ExecuteAsync();

        WaitForLatchedObserverToUpdate(observer, 1, 100, 125, _output);
        Task t2 = cmd2.ExecuteAsync();

        WaitForLatchedObserverToUpdate(observer, 1, 100, 125, _output);
        Task t3 = cmd3.ExecuteAsync();

        WaitForLatchedObserverToUpdate(observer, 1, 100, 125, _output);
        Task t4 = cmd4.ExecuteAsync();

        Task.WaitAll(t1, t2, t3, t4);
        WaitForLatchedObserverToUpdate(observer, 1, 100, 125, _output);
        Assert.Equal(3, _stream.LatestRollingMax);
    }

    // BUCKETS
    //      A    |    B    |    C    |    D    |    E    |
    //  1:  [-------------------------------]              ThreadPool x
    //  2:          [-------------------------------]                 y
    //  3:                      [--]                                  x
    //  4:                              [--]                          x
    //  Same input data as above test, just that command 2 runs in a separate threadpool, so concurrency should not get tracked
    //  Max concurrency should be 2 for x
    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestMultipleCommandsCarryOverMultipleBucketsForMultipleThreadPools()
    {
        IHystrixCommandGroupKey groupKeyX = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-X");
        IHystrixCommandGroupKey groupKeyY = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-Y");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-X");
        IHystrixCommandKey keyX = HystrixCommandKeyDefault.AsKey("RollingConcurrency-X");
        IHystrixCommandKey keyY = HystrixCommandKeyDefault.AsKey("RollingConcurrency-Y");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
        _latchSubscription = _stream.Observe().Take(10 + LatchedObserver.StableTickCount).Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd1 = Command.From(groupKeyX, keyX, HystrixEventType.Success, 300);
        Command cmd2 = Command.From(groupKeyY, keyY, HystrixEventType.Success, 300);
        Command cmd3 = Command.From(groupKeyX, keyY, HystrixEventType.Success, 10);
        Command cmd4 = Command.From(groupKeyX, keyY, HystrixEventType.Success, 10);

        Task t1 = cmd1.ExecuteAsync();

        // Time.Wait(100); // bucket roll
        WaitForLatchedObserverToUpdate(observer, 1, 100, 125, _output);
        Task t2 = cmd2.ExecuteAsync();

        // Time.Wait(100); // bucket roll
        WaitForLatchedObserverToUpdate(observer, 1, 100, 125, _output);
        Task t3 = cmd3.ExecuteAsync();

        // Time.Wait(100); // bucket roll
        WaitForLatchedObserverToUpdate(observer, 1, 100, 125, _output);
        Task t4 = cmd4.ExecuteAsync();

        Task.WaitAll(t1, t2, t3, t4);
        WaitForLatchedObserverToUpdate(observer, 1, 100, 125, _output);
        Assert.Equal(2, _stream.LatestRollingMax);
    }

    // BUCKETS
    //  1:  [-------------------------------]
    //  2:          [-------------------------------]
    //  3:                      [--]
    //  4:                              [--]
    //  Max concurrency should be 3, but by waiting for 30 bucket rolls, final max concurrency should be 0
    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestMultipleCommandsCarryOverMultipleBucketsAndThenAgeOut()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-F");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-F");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-F");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
        _latchSubscription = _stream.Observe().Take(20 + LatchedObserver.StableTickCount).Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd1 = Command.From(groupKey, key, HystrixEventType.Success, 300);
        Command cmd2 = Command.From(groupKey, key, HystrixEventType.Success, 300);
        Command cmd3 = Command.From(groupKey, key, HystrixEventType.Success, 10);
        Command cmd4 = Command.From(groupKey, key, HystrixEventType.Success, 10);

        Task t1 = cmd1.ExecuteAsync();

        WaitForLatchedObserverToUpdate(observer, 1, 100, 125, _output);
        Task t2 = cmd2.ExecuteAsync();

        WaitForLatchedObserverToUpdate(observer, 1, 100, 125, _output);
        Task t3 = cmd3.ExecuteAsync();

        WaitForLatchedObserverToUpdate(observer, 1, 100, 125, _output);
        Task t4 = cmd4.ExecuteAsync();

        Task.WaitAll(t1, t2, t3, t4);

        Assert.True(latch.Wait(10000), "CountdownEvent was not set!");
        Assert.Equal(0, _stream.LatestRollingMax);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestConcurrencyStreamProperlyFiltersOutResponseFromCache()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-G");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-G");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-G");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        Command cmd1 = Command.From(groupKey, key, HystrixEventType.Success, 40);
        Command cmd2 = Command.From(groupKey, key, HystrixEventType.ResponseFromCache);
        Command cmd3 = Command.From(groupKey, key, HystrixEventType.ResponseFromCache);
        Command cmd4 = Command.From(groupKey, key, HystrixEventType.ResponseFromCache);

        cmd1.Execute();
        cmd2.Execute();
        cmd3.Execute();
        cmd4.Execute();

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");
        Assert.True(cmd2.IsResponseFromCache);
        Assert.True(cmd3.IsResponseFromCache);
        Assert.True(cmd4.IsResponseFromCache);
        Assert.Equal(1, _stream.LatestRollingMax);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestConcurrencyStreamProperlyFiltersOutShortCircuits()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-H");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-H");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-H");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // after 3 failures, next command should short-circuit.
        // to prove short-circuited commands don't contribute to concurrency, execute 3 FAILURES in the first bucket sequentially
        // then when circuit is open, execute 20 concurrent commands.  they should all get short-circuited, and max concurrency should be 1
        Command failure1 = Command.From(groupKey, key, HystrixEventType.Failure);
        Command failure2 = Command.From(groupKey, key, HystrixEventType.Failure);
        Command failure3 = Command.From(groupKey, key, HystrixEventType.Failure);

        var shortCircuited = new List<Command>();

        for (int i = 0; i < 20; i++)
        {
            shortCircuited.Add(Command.From(groupKey, key, HystrixEventType.Success, 0));
        }

        failure1.Execute();
        failure2.Execute();
        failure3.Execute();

        Assert.True(WaitForHealthCountToUpdate(key.Name, 500, _output), "Health count stream update took to long");

        var shorts = new List<Task<int>>();

        foreach (Command cmd in shortCircuited)
        {
            shorts.Add(cmd.ExecuteAsync());
        }

        Task.WaitAll(shorts.ToArray());

        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        foreach (Command cmd in shortCircuited)
        {
            Assert.True(cmd.IsResponseShortCircuited);
        }

        Assert.Equal(1, _stream.LatestRollingMax);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public async Task TestConcurrencyStreamProperlyFiltersOutSemaphoreRejections()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-I");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-I");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-I");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
        _latchSubscription = _stream.Observe().Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // 10 commands executed concurrently on different caller threads should saturate semaphore
        // once these are in-flight, execute 10 more concurrently on new caller threads.
        // since these are semaphore-rejected, the max concurrency should be 10
        var saturators = new List<Command>();

        for (int i = 0; i < 10; i++)
        {
            saturators.Add(Command.From(groupKey, key, HystrixEventType.Success, 500, ExecutionIsolationStrategy.Semaphore));
        }

        var rejected = new List<Command>();

        for (int i = 0; i < 10; i++)
        {
            rejected.Add(Command.From(groupKey, key, HystrixEventType.Success, 0, ExecutionIsolationStrategy.Semaphore));
        }

        var tasks = new List<Task>();

        foreach (Command saturatingCmd in saturators)
        {
            tasks.Add(Task.Run(() => saturatingCmd.Execute()));
        }

        await Task.Delay(50);

        foreach (Command rejectedCmd in rejected)
        {
            await Task.Run(() => rejectedCmd.Execute());
        }

        Task.WaitAll(tasks.ToArray());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        foreach (Command rejectedCmd in rejected)
        {
            Assert.True(rejectedCmd.IsResponseSemaphoreRejected || rejectedCmd.IsResponseShortCircuited);
        }

        // should be 0 since all are executed in a semaphore
        Assert.Equal(0, _stream.LatestRollingMax);
    }

    [Fact]
    [Trait("Category", "FlakyOnHostedAgents")]
    public void TestConcurrencyStreamProperlyFiltersOutThreadPoolRejections()
    {
        IHystrixCommandGroupKey groupKey = HystrixCommandGroupKeyDefault.AsKey("ThreadPool-Concurrency-J");
        IHystrixThreadPoolKey threadPoolKey = HystrixThreadPoolKeyDefault.AsKey("ThreadPool-Concurrency-J");
        IHystrixCommandKey key = HystrixCommandKeyDefault.AsKey("RollingConcurrency-J");
        var latch = new CountdownEvent(1);
        var observer = new LatchedObserver(_output, latch);

        _stream = RollingThreadPoolMaxConcurrencyStream.GetInstance(threadPoolKey, 10, 100);
        _latchSubscription = _stream.Observe().Take(10 + LatchedObserver.StableTickCount).Subscribe(observer);
        Assert.True(Time.WaitUntil(() => observer.StreamRunning, 1000), "Stream failed to start");

        // 10 commands executed concurrently should saturate the Hystrix threadpool
        // once these are in-flight, execute 10 more concurrently
        // since these are threadpool-rejected, the max concurrency should be 10
        var saturators = new List<Command>();

        for (int i = 0; i < 10; i++)
        {
            saturators.Add(Command.From(groupKey, key, HystrixEventType.Success, 400));
        }

        var rejected = new List<Command>();

        for (int i = 0; i < 10; i++)
        {
            rejected.Add(Command.From(groupKey, key, HystrixEventType.Success, 100));
        }

        var tasks = new List<Task>();

        foreach (Command saturatingCmd in saturators)
        {
            tasks.Add(saturatingCmd.ExecuteAsync());
        }

        Time.Wait(30);

        foreach (Command rejectedCmd in rejected)
        {
            rejectedCmd.Observe();
        }

        Task.WaitAll(tasks.ToArray());
        Assert.True(WaitForLatchedObserverToUpdate(observer, 1, 500, _output), "Latch took to long to update");

        foreach (Command rejectedCmd in rejected)
        {
            Assert.True(rejectedCmd.IsResponseThreadPoolRejected);
        }

        // this should not count rejected commands
        Assert.Equal(10, _stream.LatestRollingMax);
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

    private sealed class LatchedObserver : TestObserverBase<int>
    {
        public LatchedObserver(ITestOutputHelper output, CountdownEvent latch)
            : base(output, latch)
        {
        }
    }
}
