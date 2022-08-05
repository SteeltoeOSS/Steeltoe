// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using Steeltoe.CircuitBreaker.Hystrix.Collapser;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test;

public class HystrixCollapserTest : HystrixTestBase
{
    private readonly ITestOutputHelper _output;

    public HystrixCollapserTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void TestTwoRequests()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(_output, timer, 1);
        Task<string> response1 = collapser1.ExecuteAsync();
        HystrixCollapser<List<string>, string, string> collapser2 = new TestRequestCollapser(_output, timer, 2);
        Task<string> response2 = collapser2.ExecuteAsync();

        timer.IncrementTime(10); // let time pass that equals the default delay/period

        Assert.Equal("1", response1.Result);

        if (response2.Wait(1000))
        {
            Assert.Equal("2", response2.Result);
        }
        else
        {
            Assert.False(true, "Timed out");
        }

        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        HystrixCollapserMetrics metrics = collapser1.Metrics;
        Assert.True(metrics == collapser2.Metrics);

        IHystrixInvokableInfo command = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.First();
        Assert.Equal(2, command.NumberCollapsed);
    }

    [Fact]
    public void TestMultipleBatches()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(_output, timer, 1);
        Task<string> response1 = collapser1.ExecuteAsync();
        Task<string> response2 = new TestRequestCollapser(_output, timer, 2).ExecuteAsync();
        timer.IncrementTime(10); // let time pass that equals the default delay/period

        Assert.Equal("1", GetResult(response1, 1000));
        Assert.Equal("2", GetResult(response2, 1000));

        // now request more
        Task<string> response3 = new TestRequestCollapser(_output, timer, 3).ExecuteAsync();
        timer.IncrementTime(10); // let time pass that equals the default delay/period

        Assert.Equal("3", GetResult(response3, 1000));

        // we should have had it execute twice now
        Assert.Equal(2, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        ICollection<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands;

        Assert.Equal(2, cmdIterator.First().NumberCollapsed);
        Assert.Equal(1, cmdIterator.Last().NumberCollapsed);
    }

    [Fact]
    public void TestMaxRequestsInBatch()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(_output, timer, 1, 2, 10);
        HystrixCollapser<List<string>, string, string> collapser2 = new TestRequestCollapser(_output, timer, 2, 2, 10);
        HystrixCollapser<List<string>, string, string> collapser3 = new TestRequestCollapser(_output, timer, 3, 2, 10);
        _output.WriteLine("*** " + Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Constructed the collapsers");
        Task<string> response1 = collapser1.ExecuteAsync();
        Task<string> response2 = collapser2.ExecuteAsync();
        Task<string> response3 = collapser3.ExecuteAsync();
        _output.WriteLine("*** " + Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " queued the collapsers");

        timer.IncrementTime(10); // let time pass that equals the default delay/period
        _output.WriteLine("*** " + Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " incremented the virtual timer");

        Assert.Equal("1", GetResult(response1, 1000));
        Assert.Equal("2", GetResult(response2, 1000));
        Assert.Equal("3", GetResult(response3, 1000));

        // we should have had it execute twice because the batch size was 2
        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.Equal(2, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        ICollection<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands;
        Assert.Equal(2, cmdIterator.First().NumberCollapsed);
        Assert.Equal(1, cmdIterator.Last().NumberCollapsed);
    }

    [Fact]
    public void TestRequestsOverTime()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(_output, timer, 1);
        Task<string> response1 = collapser1.ExecuteAsync();
        timer.IncrementTime(5);
        Task<string> response2 = new TestRequestCollapser(_output, timer, 2).ExecuteAsync();
        timer.IncrementTime(8);

        // should execute here
        Task<string> response3 = new TestRequestCollapser(_output, timer, 3).ExecuteAsync();
        timer.IncrementTime(6);
        Task<string> response4 = new TestRequestCollapser(_output, timer, 4).ExecuteAsync();
        timer.IncrementTime(8);

        // should execute here
        Task<string> response5 = new TestRequestCollapser(_output, timer, 5).ExecuteAsync();
        timer.IncrementTime(10);

        // should execute here

        // wait for all tasks to complete
        Assert.Equal("1", GetResult(response1, 1000));
        Assert.Equal("2", GetResult(response2, 1000));
        Assert.Equal("3", GetResult(response3, 1000));
        Assert.Equal("4", GetResult(response4, 1000));
        Assert.Equal("5", GetResult(response5, 1000));

        Assert.Equal(3, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        IHystrixInvokableInfo[] cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToArray();
        Assert.Equal(2, cmdIterator[0].NumberCollapsed);
        Assert.Equal(2, cmdIterator[1].NumberCollapsed);
        Assert.Equal(1, cmdIterator[2].NumberCollapsed);
    }

    [Fact]
    public void TestDuplicateArgumentsWithRequestCachingOn()
    {
        int num = 10;

        var observables = new List<IObservable<int>>();

        for (int i = 0; i < num; i++)
        {
            var c = new MyCollapser(_output, "5", true);
            IObservable<int> observable = c.ToObservable();
            observables.Add(observable);
        }

        var subscribers = new List<TestSubscriber<int>>();

        foreach (IObservable<int> o in observables)
        {
            var sub = new TestSubscriber<int>(_output);
            subscribers.Add(sub);
            o.Subscribe(sub);
        }

        Time.Wait(100);

        // all subscribers should receive the same value
        foreach (TestSubscriber<int> sub in subscribers)
        {
            sub.AwaitTerminalEvent(1000);
            _output.WriteLine("Subscriber received : " + sub.OnNextEvents.Count);
            sub.AssertNoErrors();
            sub.AssertValues(5);
        }
    }

    [Fact]
    public void TestDuplicateArgumentsWithRequestCachingOff()
    {
        int num = 10;

        var observables = new List<IObservable<int>>();

        for (int i = 0; i < num; i++)
        {
            var c = new MyCollapser(_output, "5", false, 500);
            observables.Add(c.ToObservable());
        }

        var subscribers = new List<TestSubscriber<int>>();

        foreach (IObservable<int> o in observables)
        {
            var sub = new TestSubscriber<int>(_output);
            subscribers.Add(sub);
            o.Subscribe(sub);
        }

        // Wait to make sure batch ran
        Time.Wait(100);

        var numErrors = new AtomicInteger(0);
        var numValues = new AtomicInteger(0);

        // only the first subscriber should receive the value.
        // the others should get an error that the batch contains duplicates
        foreach (TestSubscriber<int> sub in subscribers)
        {
            sub.AwaitTerminalEvent(1000);

            if (sub.OnCompletedEvents.Count == 0)
            {
                _output.WriteLine(Thread.CurrentThread.ManagedThreadId + " Error : " + sub.OnErrorEvents.Count);
                sub.AssertError(typeof(ArgumentException));
                sub.AssertNoValues();
                numErrors.GetAndIncrement();
            }
            else
            {
                _output.WriteLine(Thread.CurrentThread.ManagedThreadId + " OnNext : " + sub.OnNextEvents.Count);
                sub.AssertValues(5);
                sub.AssertCompleted();
                sub.AssertNoErrors();
                numValues.GetAndIncrement();
            }
        }

        Assert.Equal(1, numValues.Value);
        Assert.Equal(num - 1, numErrors.Value);
    }

    // public static IObservable<TSource> OnSubscribe<TSource>(IObservable<TSource> source)
    // {
    //    return Observable.Create<TSource>(o =>
    //    {
    //        var d = source.Subscribe(o);
    //        TestSubscriber<TSource> sub = o as TestSubscriber<TSource>;
    //        if (sub != null)
    //        {
    //            return Disposable.Create(() =>
    //            {
    //                d.Dispose();
    //                sub.Dispose();
    //            });
    //        }
    //        return d;
    //    });

    // }
    [Fact]
    public void TestUnsubscribeFromSomeDuplicateArgsDoesNotRemoveFromBatch()
    {
        int num = 10;

        var observables = new List<IObservable<int>>();
        var collapsers = new List<MyCollapser>();

        for (int i = 0; i < num; i++)
        {
            var c = new MyCollapser(_output, "5", true);
            collapsers.Add(c);
            IObservable<int> obs = c.ToObservable();
            observables.Add(obs);
        }

        var subscribers = new List<TestSubscriber<int>>();
        var subscriptions = new List<IDisposable>();

        foreach (IObservable<int> o in observables)
        {
            var sub = new TestSubscriber<int>(_output);
            subscribers.Add(sub);
            IDisposable s = o.Subscribe(sub);
            sub.Subscription = s;
            subscriptions.Add(s);
        }

        // unsubscribe from all but 1
        for (int i = 0; i < num - 1; i++)
        {
            subscribers[i].Unsubscribe();
        }

        Time.Wait(100);

        // all subscribers with an active subscription should receive the same value
        foreach (TestSubscriber<int> sub in subscribers)
        {
            if (!sub.IsUnsubscribed)
            {
                sub.AwaitTerminalEvent(1000);
                _output.WriteLine("Subscriber received : " + sub.OnNextEvents.Count);
                sub.AssertNoErrors();
                sub.AssertValues(5);
            }
            else
            {
                _output.WriteLine("Subscriber is unsubscribed");
            }
        }
    }

    [Fact]
    public void TestUnsubscribeOnOneDoesNotKillBatch()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(_output, timer, 1);
        var cts1 = new CancellationTokenSource();
        var cts2 = new CancellationTokenSource();
        Task<string> response1 = collapser1.ExecuteAsync(cts1.Token);
        Task<string> response2 = new TestRequestCollapser(_output, timer, 2).ExecuteAsync(cts2.Token);

        // kill the first
        cts1.Cancel();

        // response1.cancel(true);
        timer.IncrementTime(10); // let time pass that equals the default delay/period

        // the first is cancelled so should return null
        try
        {
            GetResult(response1, 1000);
            Assert.True(false, "expect CancellationException after cancelling");
        }
        catch (Exception)
        {
            // expected
        }

        // we should still get a response on the second
        Assert.Equal("2", GetResult(response2, 1000));
        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        ICollection<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands;
        Assert.Equal(1, cmdIterator.First().NumberCollapsed);
    }

    [Fact]
    public void TestShardedRequests()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestShardedRequestCollapser(_output, timer, "1a");
        Task<string> response1 = collapser1.ExecuteAsync();
        Task<string> response2 = new TestShardedRequestCollapser(_output, timer, "2b").ExecuteAsync();
        Task<string> response3 = new TestShardedRequestCollapser(_output, timer, "3b").ExecuteAsync();
        Task<string> response4 = new TestShardedRequestCollapser(_output, timer, "4a").ExecuteAsync();
        timer.IncrementTime(10); // let time pass that equals the default delay/period

        Assert.Equal("1a", GetResult(response1, 1000));
        Assert.Equal("2b", GetResult(response2, 1000));
        Assert.Equal("3b", GetResult(response3, 1000));
        Assert.Equal("4a", GetResult(response4, 1000));

        /* we should get 2 batches since it gets sharded */
        Assert.Equal(2, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        ICollection<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands;
        Assert.Equal(2, cmdIterator.First().NumberCollapsed);
        Assert.Equal(2, cmdIterator.Last().NumberCollapsed);
    }

    [Fact]
    public void TestRequestScope()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(_output, timer, "1");
        Task<string> response1 = collapser1.ExecuteAsync();
        Task<string> response2 = new TestRequestCollapser(_output, timer, "2").ExecuteAsync();

        // simulate a new request
        RequestCollapserFactory.ResetRequest();

        Task<string> response3 = new TestRequestCollapser(_output, timer, "3").ExecuteAsync();
        Task<string> response4 = new TestRequestCollapser(_output, timer, "4").ExecuteAsync();

        timer.IncrementTime(10); // let time pass that equals the default delay/period

        Assert.Equal("1", GetResult(response1, 1000));
        Assert.Equal("2", GetResult(response2, 1000));
        Assert.Equal("3", GetResult(response3, 1000));
        Assert.Equal("4", GetResult(response4, 1000));

        // 2 different batches should execute, 1 per request
        Assert.Equal(2, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        ICollection<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands;
        Assert.Equal(2, cmdIterator.First().NumberCollapsed);
        Assert.Equal(2, cmdIterator.Last().NumberCollapsed);
    }

    [Fact]
    public void TestGlobalScope()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestGloballyScopedRequestCollapser(_output, timer, "1");
        Task<string> response1 = collapser1.ExecuteAsync();
        Task<string> response2 = new TestGloballyScopedRequestCollapser(_output, timer, "2").ExecuteAsync();

        // simulate a new request
        RequestCollapserFactory.ResetRequest();

        Task<string> response3 = new TestGloballyScopedRequestCollapser(_output, timer, "3").ExecuteAsync();
        Task<string> response4 = new TestGloballyScopedRequestCollapser(_output, timer, "4").ExecuteAsync();

        timer.IncrementTime(10); // let time pass that equals the default delay/period
        Assert.Equal("1", GetResult(response1, 1000));
        Assert.Equal("2", GetResult(response2, 1000));
        Assert.Equal("3", GetResult(response3, 1000));
        Assert.Equal("4", GetResult(response4, 1000));

        // despite having cleared the cache in between we should have a single execution because this is on the global not request cache
        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        ICollection<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands;
        Assert.Equal(4, cmdIterator.First().NumberCollapsed);
    }

    [Fact]
    public void TestErrorHandlingViaFutureException()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapserWithFaultyCreateCommand(_output, timer, "1");
        Task<string> response1 = collapser1.ExecuteAsync();
        Task<string> response2 = new TestRequestCollapserWithFaultyCreateCommand(_output, timer, "2").ExecuteAsync();
        timer.IncrementTime(10); // let time pass that equals the default delay/period

        try
        {
            GetResult(response1, 1000);
            Assert.True(false, "we should have received an exception");
        }
        catch (Exception)
        {
            // what we expect
            // output.WriteLine(e.ToString());
        }

        try
        {
            GetResult(response2, 1000);
            Assert.True(false, "we should have received an exception");
        }
        catch (Exception)
        {
            // what we expect
            // output.WriteLine(e.ToString());
        }

        Assert.Equal(0, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
    }

    [Fact]
    public void TestErrorHandlingWhenMapToResponseFails()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapserWithFaultyMapToResponse(_output, timer, "1");
        Task<string> response1 = collapser1.ExecuteAsync();
        Task<string> response2 = new TestRequestCollapserWithFaultyMapToResponse(_output, timer, "2").ExecuteAsync();
        timer.IncrementTime(10); // let time pass that equals the default delay/period

        try
        {
            GetResult(response1, 1000);
            Assert.True(false, "we should have received an exception");
        }
        catch (Exception)
        {
            // what we expect
            // output.WriteLine(e.ToString());
        }

        try
        {
            GetResult(response2, 1000);
            Assert.True(false, "we should have received an exception");
        }
        catch (Exception)
        {
            // what we expect
            // output.WriteLine(e.ToString());
        }

        // the batch failed so no executions
        // but it still executed the command once
        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        ICollection<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands;
        Assert.Equal(2, cmdIterator.First().NumberCollapsed);
    }

    [Fact]
    public void TestRequestVariableLifecycle1()
    {
        // do actual work
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(_output, timer, 1);
        Task<string> response1 = collapser1.ExecuteAsync();
        timer.IncrementTime(5);
        Task<string> response2 = new TestRequestCollapser(_output, timer, 2).ExecuteAsync();
        timer.IncrementTime(8);

        // should execute here
        Task<string> response3 = new TestRequestCollapser(_output, timer, 3).ExecuteAsync();
        timer.IncrementTime(6);
        Task<string> response4 = new TestRequestCollapser(_output, timer, 4).ExecuteAsync();
        timer.IncrementTime(8);

        // should execute here
        Task<string> response5 = new TestRequestCollapser(_output, timer, 5).ExecuteAsync();
        timer.IncrementTime(10);

        // should execute here

        // wait for all tasks to complete
        Assert.Equal("1", GetResult(response1, 1000));
        Assert.Equal("2", GetResult(response2, 1000));
        Assert.Equal("3", GetResult(response3, 1000));
        Assert.Equal("4", GetResult(response4, 1000));
        Assert.Equal("5", GetResult(response5, 1000));

        // each task should have been executed 3 times
        foreach (ATask t in timer.Tasks.Values)
        {
            Assert.Equal(3, t.Task.Count.Value);
        }

        _output.WriteLine("timer.tasks.size() A: " + timer.Tasks.Count);
        _output.WriteLine("tasks in test: " + timer.Tasks);

        List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
        Assert.Equal(2, cmdIterator[0].NumberCollapsed);
        Assert.Equal(2, cmdIterator[1].NumberCollapsed);
        Assert.Equal(1, cmdIterator[2].NumberCollapsed);

        _output.WriteLine("timer.tasks.size() B: " + timer.Tasks.Count);

        RequestCollapserFactory.RequestCollapserRequestVariable<List<string>, string, string> rv =
            RequestCollapserFactory.GetRequestVariable<List<string>, string, string>(new TestRequestCollapser(_output, timer, 1).CollapserKey.Name);

        context.Dispose();

        Assert.NotNull(rv);

        // they should have all been removed as part of ThreadContext.remove()
        Assert.Empty(timer.Tasks);
    }

    [Fact]
    public void TestRequestVariableLifecycle2()
    {
        var timer = new TestCollapserTimer(_output);
        var responses = new ConcurrentDictionary<Task<string>, Task<string>>();
        var threads = new List<Task>();

        // kick off work (simulating a single request with multiple threads)
        for (int t = 0; t < 5; t++)
        {
            int outerLoop = t;

            var th = new Task(() =>
            {
                for (int i = 0; i < 100; i++)
                {
                    int uniqueInt = outerLoop * 100 + i;
                    Task<string> tsk = new TestRequestCollapser(_output, timer, uniqueInt).ExecuteAsync();
                    responses.TryAdd(tsk, tsk);
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning);

            th.Start();
            threads.Add(th);
        }

        Task.WaitAll(threads.ToArray());

        // we expect 5 threads * 100 responses each
        Assert.Equal(500, responses.Count);

        foreach (Task<string> f in responses.Values)
        {
            // they should not be done yet because the counter hasn't incremented
            Assert.False(f.IsCompleted);
        }

        timer.IncrementTime(5);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(_output, timer, 2);
        Task<string> response2 = collapser1.ExecuteAsync();
        timer.IncrementTime(8);

        // should execute here
        Task<string> response3 = new TestRequestCollapser(_output, timer, 3).ExecuteAsync();
        timer.IncrementTime(6);
        Task<string> response4 = new TestRequestCollapser(_output, timer, 4).ExecuteAsync();
        timer.IncrementTime(8);

        // should execute here
        Task<string> response5 = new TestRequestCollapser(_output, timer, 5).ExecuteAsync();
        timer.IncrementTime(10);

        // should execute here

        // wait for all tasks to complete
        foreach (Task<string> f in responses.Values)
        {
            GetResult(f, 1000);
        }

        Assert.Equal("2", GetResult(response2, 1000));
        Assert.Equal("3", GetResult(response3, 1000));
        Assert.Equal("4", GetResult(response4, 1000));
        Assert.Equal("5", GetResult(response5, 1000));

        // each task should have been executed 3 times
        foreach (ATask t in timer.Tasks.Values)
        {
            Assert.Equal(3, t.Task.Count.Value);
        }

        List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
        Assert.Equal(500, cmdIterator[0].NumberCollapsed);
        Assert.Equal(2, cmdIterator[1].NumberCollapsed);
        Assert.Equal(1, cmdIterator[2].NumberCollapsed);

        RequestCollapserFactory.RequestCollapserRequestVariable<List<string>, string, string> rv =
            RequestCollapserFactory.GetRequestVariable<List<string>, string, string>(new TestRequestCollapser(_output, timer, 1).CollapserKey.Name);

        context.Dispose();

        Assert.NotNull(rv);

        // they should have all been removed as part of ThreadContext.remove()
        Assert.Empty(timer.Tasks);
    }

    [Fact]
    public void TestRequestCache1()
    {
        var timer = new TestCollapserTimer(_output);
        var command1 = new SuccessfulCacheableCollapsedCommand(_output, timer, "A", true);
        var command2 = new SuccessfulCacheableCollapsedCommand(_output, timer, "A", true);

        Task<string> f1 = command1.ExecuteAsync();
        Task<string> f2 = command2.ExecuteAsync();

        // increment past batch time so it executes
        timer.IncrementTime(15);

        try
        {
            Assert.Equal("A", GetResult(f1, 1000));
            Assert.Equal("A", GetResult(f2, 1000));
        }
        catch (Exception e)
        {
            throw new Exception(e.ToString());
        }

        Task<string> f3 = command1.ExecuteAsync();

        // increment past batch time so it executes
        timer.IncrementTime(15);

        try
        {
            Assert.Equal("A", GetResult(f3, 1000));
        }
        catch (Exception e)
        {
            throw new Exception(e.ToString());
        }

        // we should still have executed only one command
        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        IHystrixInvokableInfo command = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.AsEnumerable().First();
        _output.WriteLine("command.getExecutionEvents(): " + command.ExecutionEvents.Count);
        Assert.Equal(2, command.ExecutionEvents.Count);
        Assert.Contains(HystrixEventType.Success, command.ExecutionEvents);
        Assert.Contains(HystrixEventType.Collapsed, command.ExecutionEvents);

        Assert.Equal(1, command.NumberCollapsed);
    }

    [Fact]
    public void TestRequestCache2()
    {
        var timer = new TestCollapserTimer(_output);
        var command1 = new SuccessfulCacheableCollapsedCommand(_output, timer, "A", true);
        var command2 = new SuccessfulCacheableCollapsedCommand(_output, timer, "B", true);

        Task<string> f1 = command1.ExecuteAsync();
        Task<string> f2 = command2.ExecuteAsync();

        // increment past batch time so it executes
        timer.IncrementTime(15);

        try
        {
            Assert.Equal("A", GetResult(f1, 1000));
            Assert.Equal("B", GetResult(f2, 1000));
        }
        catch (Exception e)
        {
            throw new Exception(e.ToString());
        }

        Task<string> f3 = command1.ExecuteAsync();
        Task<string> f4 = command2.ExecuteAsync();

        // increment past batch time so it executes
        timer.IncrementTime(15);

        try
        {
            Assert.Equal("A", GetResult(f3, 1000));
            Assert.Equal("B", GetResult(f4, 1000));
        }
        catch (Exception e)
        {
            throw new Exception(e.ToString());
        }

        // we should still have executed only one command
        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        IHystrixInvokableInfo command = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.AsEnumerable().First();
        _output.WriteLine("command.getExecutionEvents(): " + command.ExecutionEvents.Count);
        Assert.Equal(2, command.ExecutionEvents.Count);
        Assert.Contains(HystrixEventType.Success, command.ExecutionEvents);
        Assert.Contains(HystrixEventType.Collapsed, command.ExecutionEvents);
    }

    [Fact]
    public void TestRequestCache3()
    {
        var timer = new TestCollapserTimer(_output);
        var command1 = new SuccessfulCacheableCollapsedCommand(_output, timer, "A", true);
        var command2 = new SuccessfulCacheableCollapsedCommand(_output, timer, "B", true);
        var command3 = new SuccessfulCacheableCollapsedCommand(_output, timer, "B", true);

        Task<string> f1 = command1.ExecuteAsync();
        Task<string> f2 = command2.ExecuteAsync();
        Task<string> f3 = command3.ExecuteAsync();

        // increment past batch time so it executes
        timer.IncrementTime(15);

        Assert.Equal("A", GetResult(f1, 1000));
        Assert.Equal("B", GetResult(f2, 1000));
        Assert.Equal("B", GetResult(f3, 1000));

        Task<string> f4 = command1.ExecuteAsync();
        Task<string> f5 = command2.ExecuteAsync();
        Task<string> f6 = command3.ExecuteAsync();

        // increment past batch time so it executes
        timer.IncrementTime(15);

        Assert.Equal("A", GetResult(f4, 1000));
        Assert.Equal("B", GetResult(f5, 1000));
        Assert.Equal("B", GetResult(f6, 1000));

        // we should still have executed only one command
        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        IHystrixInvokableInfo command = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList()[0];
        Assert.Equal(2, command.ExecutionEvents.Count);
        Assert.Contains(HystrixEventType.Success, command.ExecutionEvents);
        Assert.Contains(HystrixEventType.Collapsed, command.ExecutionEvents);

        Assert.Equal(2, command.NumberCollapsed);
    }

    [Fact]
    public void TestNoRequestCache3()
    {
        var timer = new TestCollapserTimer(_output);
        var command1 = new SuccessfulCacheableCollapsedCommand(_output, timer, "A", false);
        var command2 = new SuccessfulCacheableCollapsedCommand(_output, timer, "B", false);
        var command3 = new SuccessfulCacheableCollapsedCommand(_output, timer, "B", false);

        Task<string> f1 = command1.ExecuteAsync();
        Task<string> f2 = command2.ExecuteAsync();
        Task<string> f3 = command3.ExecuteAsync();

        // increment past batch time so it executes
        timer.IncrementTime(15);

        Assert.Equal("A", GetResult(f1, 1000));
        Assert.Equal("B", GetResult(f2, 1000));
        Assert.Equal("B", GetResult(f3, 1000));

        Task<string> f4 = command1.ExecuteAsync();
        Task<string> f5 = command2.ExecuteAsync();
        Task<string> f6 = command3.ExecuteAsync();

        // increment past batch time so it executes
        timer.IncrementTime(15);

        Assert.Equal("A", GetResult(f4, 1000));
        Assert.Equal("B", GetResult(f5, 1000));
        Assert.Equal("B", GetResult(f6, 1000));

        // request caching is turned off on this so we expect 2 command executions
        Assert.Equal(2, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        // we expect to see it with SUCCESS and COLLAPSED and both
        IHystrixInvokableInfo commandA = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList()[0];
        Assert.Equal(2, commandA.ExecutionEvents.Count);
        Assert.Contains(HystrixEventType.Success, commandA.ExecutionEvents);
        Assert.Contains(HystrixEventType.Collapsed, commandA.ExecutionEvents);

        // we expect to see it with SUCCESS and COLLAPSED and both
        IHystrixInvokableInfo commandB = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList()[1];
        Assert.Equal(2, commandB.ExecutionEvents.Count);
        Assert.Contains(HystrixEventType.Success, commandB.ExecutionEvents);
        Assert.Contains(HystrixEventType.Collapsed, commandB.ExecutionEvents);

        List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
        Assert.Equal(2, cmdIterator[0].NumberCollapsed); // 1 for A, 1 for B.  Batch contains only unique arguments (no duplicates)
        Assert.Equal(2, cmdIterator[1].NumberCollapsed); // 1 for A, 1 for B.  Batch contains only unique arguments (no duplicates)
    }

    [Fact]
    public void TestRequestCacheWithNullRequestArgument()
    {
        var commands = new ConcurrentQueue<HystrixCommand<List<string>>>();

        var timer = new TestCollapserTimer(_output);
        var command1 = new SuccessfulCacheableCollapsedCommand(_output, timer, null, true, commands);
        var command2 = new SuccessfulCacheableCollapsedCommand(_output, timer, null, true, commands);

        Task<string> f1 = command1.ExecuteAsync();
        Task<string> f2 = command2.ExecuteAsync();

        // increment past batch time so it executes
        timer.IncrementTime(15);

        Assert.Equal("NULL", GetResult(f1, 1000));
        Assert.Equal("NULL", GetResult(f2, 1000));

        // it should have executed 1 command
        Assert.Single(commands);
        commands.TryPeek(out HystrixCommand<List<string>> peek);
        Assert.Contains(HystrixEventType.Success, peek.ExecutionEvents);
        Assert.Contains(HystrixEventType.Collapsed, peek.ExecutionEvents);

        Task<string> f3 = command1.ExecuteAsync();

        // increment past batch time so it executes
        timer.IncrementTime(15);

        Assert.Equal("NULL", GetResult(f3, 1000));

        // it should still be 1 ... no new executions
        Assert.Single(commands);
        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
        Assert.Equal(1, cmdIterator[0].NumberCollapsed);
    }

    [Fact]
    public void TestRequestCacheWithCommandError()
    {
        var commands = new ConcurrentQueue<HystrixCommand<List<string>>>();

        var timer = new TestCollapserTimer(_output);
        var command1 = new SuccessfulCacheableCollapsedCommand(_output, timer, "FAILURE", true, commands);
        var command2 = new SuccessfulCacheableCollapsedCommand(_output, timer, "FAILURE", true, commands);

        Task<string> f1 = command1.ExecuteAsync();
        Task<string> f2 = command2.ExecuteAsync();

        // increment past batch time so it executes
        timer.IncrementTime(15);

        try
        {
            Assert.Equal("A", GetResult(f1, 1000));
            Assert.Equal("A", GetResult(f2, 1000));
            Assert.True(false, "exception should have been thrown");
        }
        catch (Exception)
        {
            // expected
        }

        // it should have executed 1 command
        Assert.Single(commands);
        commands.TryPeek(out HystrixCommand<List<string>> peek);

        Assert.Contains(HystrixEventType.Failure, peek.ExecutionEvents);
        Assert.Contains(HystrixEventType.Collapsed, peek.ExecutionEvents);

        Task<string> f3 = command1.ExecuteAsync();

        // increment past batch time so it executes
        timer.IncrementTime(15);

        try
        {
            Assert.Equal("A", GetResult(f3, 1000));
            Assert.True(false, "exception should have been thrown");
        }
        catch (Exception)
        {
            // expected
        }

        // it should still be 1 ... no new executions
        Assert.Single(commands);
        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
        Assert.Equal(1, cmdIterator[0].NumberCollapsed);
    }

    [Fact]
    public void TestRequestCacheWithCommandTimeout()
    {
        var commands = new ConcurrentQueue<HystrixCommand<List<string>>>();

        var timer = new TestCollapserTimer(_output);
        var command1 = new SuccessfulCacheableCollapsedCommand(_output, timer, "TIMEOUT", true, commands);
        var command2 = new SuccessfulCacheableCollapsedCommand(_output, timer, "TIMEOUT", true, commands);

        Task<string> f1 = command1.ExecuteAsync();
        Task<string> f2 = command2.ExecuteAsync();

        // increment past batch time so it executes
        timer.IncrementTime(15);

        try
        {
            Assert.Equal("A", GetResult(f1, 1000));
            Assert.Equal("A", GetResult(f2, 1000));
            Assert.True(false, "exception should have been thrown");
        }
        catch (Exception)
        {
            // expected
        }

        // it should have executed 1 command
        Assert.Single(commands);
        commands.TryPeek(out HystrixCommand<List<string>> peek);
        Assert.Contains(HystrixEventType.Timeout, peek.ExecutionEvents);
        Assert.Contains(HystrixEventType.Collapsed, peek.ExecutionEvents);

        Task<string> f3 = command1.ExecuteAsync();

        // increment past batch time so it executes
        timer.IncrementTime(15);

        try
        {
            Assert.Equal("A", GetResult(f3, 1000));
            Assert.True(false, "exception should have been thrown");
        }
        catch (Exception)
        {
            // expected
        }

        // it should still be 1 ... no new executions
        Assert.Single(commands);
        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
        Assert.Equal(1, cmdIterator[0].NumberCollapsed);
    }

    [Fact]
    public async Task TestRequestWithCommandShortCircuited()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapserWithShortCircuitedCommand(_output, timer, "1");
        IObservable<string> response1 = collapser1.Observe();
        IObservable<string> response2 = new TestRequestCollapserWithShortCircuitedCommand(_output, timer, "2").Observe();
        timer.IncrementTime(10); // let time pass that equals the default delay/period

        try
        {
            await response1.FirstAsync();
            Assert.True(false, "we should have received an exception");
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());

            // what we expect
        }

        try
        {
            await response2.FirstAsync();
            Assert.True(false, "we should have received an exception");
        }
        catch (Exception e)
        {
            _output.WriteLine(e.ToString());

            // what we expect
        }

        // it will execute once (short-circuited)
        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
        Assert.Equal(2, cmdIterator[0].NumberCollapsed);
    }

    [Fact]
    public void TestVoidResponseTypeFireAndForgetCollapsing1()
    {
        var timer = new TestCollapserTimer(_output);
        var collapser1 = new TestCollapserWithVoidResponseType(_output, timer, 1);
        Task<object> response1 = collapser1.ExecuteAsync();
        Task<object> response2 = new TestCollapserWithVoidResponseType(_output, timer, 2).ExecuteAsync();
        timer.IncrementTime(100); // let time pass that equals the default delay/period

        // normally someone wouldn't wait on these, but we need to make sure they do in fact return
        // and not block indefinitely in case someone does call get()
        Assert.Null(GetResult(response1, 1000));
        Assert.Null(GetResult(response2, 1000));

        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
        Assert.Equal(2, cmdIterator[0].NumberCollapsed);
    }

    [Fact]
    public void TestVoidResponseTypeFireAndForgetCollapsing2()
    {
        var timer = new TestCollapserTimer(_output);
        var collapser1 = new TestCollapserWithVoidResponseTypeAndMissingMapResponseToRequests(_output, timer, 1);
        Task<object> response1 = collapser1.ExecuteAsync();
        new TestCollapserWithVoidResponseTypeAndMissingMapResponseToRequests(_output, timer, 2).ExecuteAsync();
        timer.IncrementTime(100); // let time pass that equals the default delay/period

        // we will fetch one of these just so we wait for completion ... but expect an error
        try
        {
            Assert.Null(GetResult(response1, 1000));
            Assert.False(true, "expected an error as mapResponseToRequests did not set responses");
        }
        catch (Exception e)
        {
            Assert.True(e.InnerException is InvalidOperationException);
            Assert.StartsWith("No response set by", e.InnerException.Message);
        }

        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
        Assert.Equal(2, cmdIterator[0].NumberCollapsed);
    }

    [Fact]
    public void TestVoidResponseTypeFireAndForgetCollapsing3()
    {
        ICollapserTimer timer = new RealCollapserTimer();
        var collapser1 = new TestCollapserWithVoidResponseType(_output, timer, 1);
        Assert.Null(collapser1.Execute());

        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
        Assert.Equal(1, cmdIterator[0].NumberCollapsed);
    }

    [Fact]
    public void TestEarlyUnsubscribeExecutedViaToObservable()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(_output, timer, 1);
        IObservable<string> response1 = collapser1.ToObservable();
        HystrixCollapser<List<string>, string, string> collapser2 = new TestRequestCollapser(_output, timer, 2);
        IObservable<string> response2 = collapser2.ToObservable();

        var latch1 = new CountdownEvent(1);
        var latch2 = new CountdownEvent(1);

        var value1 = new AtomicReference<string>(null);
        var value2 = new AtomicReference<string>(null);

        IDisposable s1 = response1.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 Unsubscribed!");
            latch1.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnNext : " + s);
            value1.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnError : " + e);
            latch1.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnCompleted!");
            latch1.SignalEx();
        });

        response2.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 Unsubscribed!");
            latch2.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnNext : " + s);
            value2.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnError : " + e);
            latch2.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnCompleted!");
            latch2.SignalEx();
        });

        s1.Dispose();

        timer.IncrementTime(10); // let time pass that equals the default delay/period

        Assert.True(latch1.Wait(1000));
        Assert.True(latch2.Wait(1000));

        Assert.Null(value1.Value);
        Assert.Equal("2", value2.Value);

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
        HystrixCollapserMetrics metrics = collapser1.Metrics;
        Assert.True(metrics == collapser2.Metrics);

        List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
        Assert.Equal(1, cmdIterator[0].NumberCollapsed);
    }

    [Fact]
    public void TestEarlyUnsubscribeExecutedViaObserve()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(_output, timer, 1);
        IObservable<string> response1 = collapser1.Observe();
        HystrixCollapser<List<string>, string, string> collapser2 = new TestRequestCollapser(_output, timer, 2);
        IObservable<string> response2 = collapser2.Observe();

        var latch1 = new CountdownEvent(1);
        var latch2 = new CountdownEvent(1);

        var value1 = new AtomicReference<string>(null);
        var value2 = new AtomicReference<string>(null);

        IDisposable s1 = response1.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 Unsubscribed!");
            latch1.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnNext : " + s);
            value1.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnError : " + e);
            latch1.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnCompleted!");
            latch1.SignalEx();
        });

        response2.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 Unsubscribed!");
            latch2.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnNext : " + s);
            value2.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnError : " + e);
            latch2.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnCompleted!");
            latch2.SignalEx();
        });

        s1.Dispose();

        timer.IncrementTime(10); // let time pass that equals the default delay/period

        Assert.True(latch1.Wait(1000));
        Assert.True(latch2.Wait(1000));

        Assert.Null(value1.Value);
        Assert.Equal("2", value2.Value);

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
        HystrixCollapserMetrics metrics = collapser1.Metrics;
        Assert.True(metrics == collapser2.Metrics);

        List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
        Assert.Equal(1, cmdIterator[0].NumberCollapsed);
    }

    [Fact]
    public void TestEarlyUnsubscribeFromAllCancelsBatch()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(_output, timer, 1);
        IObservable<string> response1 = collapser1.Observe();
        HystrixCollapser<List<string>, string, string> collapser2 = new TestRequestCollapser(_output, timer, 2);
        IObservable<string> response2 = collapser2.Observe();

        var latch1 = new CountdownEvent(1);
        var latch2 = new CountdownEvent(1);

        var value1 = new AtomicReference<string>(null);
        var value2 = new AtomicReference<string>(null);

        IDisposable s1 = response1.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 Unsubscribed!");
            latch1.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnNext : " + s);
            value1.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnError : " + e);
            latch1.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnCompleted!");
            latch1.SignalEx();
        });

        IDisposable s2 = response2.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 Unsubscribed!");
            latch2.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnNext : " + s);
            value2.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnError : " + e);
            latch2.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnCompleted!");
            latch2.SignalEx();
        });

        s1.Dispose();
        s2.Dispose();

        timer.IncrementTime(10); // let time pass that equals the default delay/period

        Assert.True(latch1.Wait(1000));
        Assert.True(latch2.Wait(1000));

        Assert.Null(value1.Value);
        Assert.Null(value2.Value);

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.Equal(0, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
    }

    [Fact]
    public void TestRequestThenCacheHitAndCacheHitUnsubscribed()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new SuccessfulCacheableCollapsedCommand(_output, timer, "foo", true);
        IObservable<string> response1 = collapser1.Observe();
        HystrixCollapser<List<string>, string, string> collapser2 = new SuccessfulCacheableCollapsedCommand(_output, timer, "foo", true);
        IObservable<string> response2 = collapser2.Observe();

        var latch1 = new CountdownEvent(1);
        var latch2 = new CountdownEvent(1);

        var value1 = new AtomicReference<string>(null);
        var value2 = new AtomicReference<string>(null);

        response1.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 Unsubscribed!");
            latch1.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnNext : " + s);
            value1.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnError : " + e);
            latch1.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnCompleted!");
            latch1.SignalEx();
        });

        IDisposable s2 = response2.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 Unsubscribed!");
            latch2.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnNext : " + s);
            value2.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnError : " + e);
            latch2.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnCompleted!");
            latch2.SignalEx();
        });

        s2.Dispose();

        timer.IncrementTime(10); // let time pass that equals the default delay/period

        Assert.True(latch1.Wait(1000));
        Assert.True(latch2.Wait(1000));

        Assert.Equal("foo", value1.Value);
        Assert.Null(value2.Value);

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
        AssertCommandExecutionEvents(cmdIterator[0], HystrixEventType.Success, HystrixEventType.Collapsed);
        Assert.Equal(1, cmdIterator[0].NumberCollapsed); // should only be 1 collapsed - other came from cache, then was cancelled
    }

    [Fact]
    public void TestRequestThenCacheHitAndOriginalUnsubscribed()
    {
        // TODO:
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new SuccessfulCacheableCollapsedCommand(_output, timer, "foo", true);
        IObservable<string> response1 = collapser1.Observe();
        HystrixCollapser<List<string>, string, string> collapser2 = new SuccessfulCacheableCollapsedCommand(_output, timer, "foo", true);
        IObservable<string> response2 = collapser2.Observe();

        var latch1 = new CountdownEvent(1);
        var latch2 = new CountdownEvent(1);

        var value1 = new AtomicReference<string>(null);
        var value2 = new AtomicReference<string>(null);

        IDisposable s1 = response1.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 Unsubscribed!");
            latch1.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnNext : " + s);
            value1.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnError : " + e);
            latch1.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnCompleted!");
            latch1.SignalEx();
        });

        response2.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 Unsubscribed!");
            latch2.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnNext : " + s);
            value2.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnError : " + e);
            latch2.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnCompleted!");
            latch2.SignalEx();
        });

        s1.Dispose();

        timer.IncrementTime(10); // let time pass that equals the default delay/period

        Assert.True(latch1.Wait(1000));
        Assert.True(latch2.Wait(1000));

        Assert.Null(value1.Value);
        Assert.Equal("foo", value2.Value);

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
        AssertCommandExecutionEvents(cmdIterator[0], HystrixEventType.Success, HystrixEventType.Collapsed);
        Assert.Equal(1, cmdIterator[0].NumberCollapsed); // should only be 1 collapsed - other came from cache, then was cancelled
    }

    [Fact]
    public void TestRequestThenTwoCacheHitsOriginalAndOneCacheHitUnsubscribed()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new SuccessfulCacheableCollapsedCommand(_output, timer, "foo", true);
        IObservable<string> response1 = collapser1.Observe();
        HystrixCollapser<List<string>, string, string> collapser2 = new SuccessfulCacheableCollapsedCommand(_output, timer, "foo", true);
        IObservable<string> response2 = collapser2.Observe();
        HystrixCollapser<List<string>, string, string> collapser3 = new SuccessfulCacheableCollapsedCommand(_output, timer, "foo", true);
        IObservable<string> response3 = collapser3.Observe();

        var latch1 = new CountdownEvent(1);
        var latch2 = new CountdownEvent(1);
        var latch3 = new CountdownEvent(1);

        var value1 = new AtomicReference<string>(null);
        var value2 = new AtomicReference<string>(null);
        var value3 = new AtomicReference<string>(null);

        IDisposable s1 = response1.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 Unsubscribed!");
            latch1.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnNext : " + s);
            value1.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnError : " + e);
            latch1.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnCompleted!");
            latch1.SignalEx();
        });

        response2.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 Unsubscribed!");
            latch2.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnNext : " + s);
            value2.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnError : " + e);
            latch2.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnCompleted!");
            latch2.SignalEx();
        });

        IDisposable s3 = response3.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s3 Unsubscribed!");
            latch3.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s3 OnNext : " + s);
            value3.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s3 OnError : " + e);
            latch3.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s3 OnCompleted!");
            latch3.SignalEx();
        });

        s1.Dispose();
        s3.Dispose();

        timer.IncrementTime(10); // let time pass that equals the default delay/period

        Assert.True(latch1.Wait(1000));
        Assert.True(latch2.Wait(1000));
        Assert.True(latch3.Wait(1000));

        Assert.Null(value1.Value);
        Assert.Equal("foo", value2.Value);
        Assert.Null(value3.Value);

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

        List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
        AssertCommandExecutionEvents(cmdIterator[0], HystrixEventType.Success, HystrixEventType.Collapsed);
        Assert.Equal(1, cmdIterator[0].NumberCollapsed); // should only be 1 collapsed - other came from cache, then was cancelled
    }

    [Fact]
    public void TestRequestThenTwoCacheHitsAllUnsubscribed()
    {
        var timer = new TestCollapserTimer(_output);
        HystrixCollapser<List<string>, string, string> collapser1 = new SuccessfulCacheableCollapsedCommand(_output, timer, "foo", true);
        IObservable<string> response1 = collapser1.Observe();
        HystrixCollapser<List<string>, string, string> collapser2 = new SuccessfulCacheableCollapsedCommand(_output, timer, "foo", true);
        IObservable<string> response2 = collapser2.Observe();
        HystrixCollapser<List<string>, string, string> collapser3 = new SuccessfulCacheableCollapsedCommand(_output, timer, "foo", true);
        IObservable<string> response3 = collapser3.Observe();

        var latch1 = new CountdownEvent(1);
        var latch2 = new CountdownEvent(1);
        var latch3 = new CountdownEvent(1);

        var value1 = new AtomicReference<string>(null);
        var value2 = new AtomicReference<string>(null);
        var value3 = new AtomicReference<string>(null);

        IDisposable s1 = response1.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 Unsubscribed!");
            latch1.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnNext : " + s);
            value1.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnError : " + e);
            latch1.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s1 OnCompleted!");
            latch1.SignalEx();
        });

        IDisposable s2 = response2.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 Unsubscribed!");
            latch2.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnNext : " + s);
            value2.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnError : " + e);
            latch2.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s2 OnCompleted!");
            latch2.SignalEx();
        });

        IDisposable s3 = response3.OnDispose(() =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s3 Unsubscribed!");
            latch3.SignalEx();
        }).Subscribe(s =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s3 OnNext : " + s);
            value3.Value = s;
        }, e =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s3 OnError : " + e);
            latch3.SignalEx();
        }, () =>
        {
            _output.WriteLine(Time.CurrentTimeMillis + " : s3 OnCompleted!");
            latch3.SignalEx();
        });

        s1.Dispose();
        s2.Dispose();
        s3.Dispose();

        timer.IncrementTime(10); // let time pass that equals the default delay/period

        Assert.True(latch1.Wait(1000));
        Assert.True(latch2.Wait(1000));
        Assert.True(latch3.Wait(1000));

        Assert.Null(value1.Value);
        Assert.Null(value2.Value);
        Assert.Null(value3.Value);

        _output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
        Assert.Equal(0, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            HystrixCollapserMetrics.Reset();
        }
    }

    protected void AssertCommandExecutionEvents(IHystrixInvokableInfo command, params HystrixEventType[] expectedEventTypes)
    {
        bool emitExpected = false;
        int expectedEmitCount = 0;

        bool fallbackEmitExpected = false;
        int expectedFallbackEmitCount = 0;

        var condensedEmitExpectedEventTypes = new List<HystrixEventType>();

        foreach (HystrixEventType expectedEventType in expectedEventTypes)
        {
            if (expectedEventType.Equals(HystrixEventType.Emit))
            {
                if (!emitExpected)
                {
                    // first EMIT encountered, add it to condensedEmitExpectedEventTypes
                    condensedEmitExpectedEventTypes.Add(HystrixEventType.Emit);
                }

                emitExpected = true;
                expectedEmitCount++;
            }
            else if (expectedEventType.Equals(HystrixEventType.FallbackEmit))
            {
                if (!fallbackEmitExpected)
                {
                    // first FALLBACK_EMIT encountered, add it to condensedEmitExpectedEventTypes
                    condensedEmitExpectedEventTypes.Add(HystrixEventType.FallbackEmit);
                }

                fallbackEmitExpected = true;
                expectedFallbackEmitCount++;
            }
            else
            {
                condensedEmitExpectedEventTypes.Add(expectedEventType);
            }
        }

        List<HystrixEventType> actualEventTypes = command.ExecutionEvents;
        Assert.Equal(expectedEmitCount, command.NumberEmissions);
        Assert.Equal(expectedFallbackEmitCount, command.NumberFallbackEmissions);
        Assert.Equal(condensedEmitExpectedEventTypes, actualEventTypes);
    }

    private static T GetResult<T>(Task<T> task, int timeout)
    {
        if (task.Wait(timeout))
        {
            return task.Result;
        }

        return default;
    }

    private static IHystrixCollapserKey CollapserKeyFromString(object o)
    {
        return new HystrixCollapserKeyDefault(o.ToString() + o.GetHashCode());
    }

    private class TestRequestCollapser : HystrixCollapser<List<string>, string, string>
    {
        protected readonly string Value;
        protected readonly ConcurrentQueue<HystrixCommand<List<string>>> CommandsExecuted;
        protected readonly ITestOutputHelper Output;

        public override string RequestArgument => Value;

        public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, int value)
            : this(output, timer, value.ToString())
        {
        }

        public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value)
            : this(output, timer, value, 10000, 10)
        {
        }

        public TestRequestCollapser(ITestOutputHelper output, RequestCollapserScope scope, TestCollapserTimer timer, string value)
            : this(output, scope, timer, value, 10000, 10)
        {
        }

        public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value,
            ConcurrentQueue<HystrixCommand<List<string>>> executionLog)
            : this(output, timer, value, 10000, 10, executionLog)
        {
        }

        public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, int value, int defaultMaxRequestsInBatch,
            int defaultTimerDelayInMilliseconds)
            : this(output, timer, value.ToString(), defaultMaxRequestsInBatch, defaultTimerDelayInMilliseconds)
        {
        }

        public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value, int defaultMaxRequestsInBatch,
            int defaultTimerDelayInMilliseconds)
            : this(output, timer, value, defaultMaxRequestsInBatch, defaultTimerDelayInMilliseconds, null)
        {
        }

        public TestRequestCollapser(ITestOutputHelper output, RequestCollapserScope scope, TestCollapserTimer timer, string value,
            int defaultMaxRequestsInBatch, int defaultTimerDelayInMilliseconds)
            : this(output, scope, timer, value, defaultMaxRequestsInBatch, defaultTimerDelayInMilliseconds, null)
        {
        }

        public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value, int defaultMaxRequestsInBatch,
            int defaultTimerDelayInMilliseconds, ConcurrentQueue<HystrixCommand<List<string>>> executionLog)
            : this(output, RequestCollapserScope.Request, timer, value, defaultMaxRequestsInBatch, defaultTimerDelayInMilliseconds, executionLog)
        {
        }

        public TestRequestCollapser(ITestOutputHelper output, RequestCollapserScope scope, TestCollapserTimer timer, string value,
            int defaultMaxRequestsInBatch, int defaultTimerDelayInMilliseconds, ConcurrentQueue<HystrixCommand<List<string>>> executionLog)
            : base(CollapserKeyFromString(timer), scope, timer,
                GetOptions(CollapserKeyFromString(timer), defaultMaxRequestsInBatch, defaultTimerDelayInMilliseconds), CreateMetrics())
        {
            Value = value;
            CommandsExecuted = executionLog;
            Output = output;
        }

        private static HystrixCollapserMetrics CreateMetrics()
        {
            IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("COLLAPSER_ONE");
            return HystrixCollapserMetrics.GetInstance(key, new HystrixCollapserOptions(key));
        }

        public static IHystrixCollapserOptions GetOptions(IHystrixCollapserKey key, int defaultMaxRequestsInBatch, int defaultTimerDelayInMilliseconds)
        {
            var opts = new HystrixCollapserOptions(key)
            {
                MaxRequestsInBatch = defaultMaxRequestsInBatch,
                TimerDelayInMilliseconds = defaultTimerDelayInMilliseconds
            };

            return opts;
        }

        protected override HystrixCommand<List<string>> CreateCommand(ICollection<ICollapsedRequest<string, string>> requests)
        {
            /* return a mocked command */
            HystrixCommand<List<string>> command = new TestCollapserCommand(Output, requests);

            if (CommandsExecuted != null)
            {
                CommandsExecuted.Enqueue(command);
            }

            return command;
        }

        protected override void MapResponseToRequests(List<string> batchResponse, ICollection<ICollapsedRequest<string, string>> requests)
        {
            // for simplicity I'll assume it's a 1:1 mapping between lists ... in real implementations they often need to index to maps
            // to allow random access as the response size does not match the request size
            if (batchResponse.Count != requests.Count)
            {
                throw new Exception($"lists don't match in size => {batchResponse.Count} : {requests.Count}");
            }

            int i = 0;

            foreach (ICollapsedRequest<string, string> request in requests)
            {
                request.Response = batchResponse[i++];
            }
        }
    }

    private sealed class TestShardedRequestCollapser : TestRequestCollapser
    {
        public TestShardedRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value)
            : base(output, timer, value)
        {
        }

        protected override ICollection<ICollection<ICollapsedRequest<string, string>>> ShardRequests(ICollection<ICollapsedRequest<string, string>> requests)
        {
            ICollection<ICollapsedRequest<string, string>> typeA = new List<ICollapsedRequest<string, string>>();
            ICollection<ICollapsedRequest<string, string>> typeB = new List<ICollapsedRequest<string, string>>();

            foreach (ICollapsedRequest<string, string> request in requests)
            {
                if (request.Argument.EndsWith("a"))
                {
                    typeA.Add(request);
                }
                else if (request.Argument.EndsWith("b"))
                {
                    typeB.Add(request);
                }
            }

            var shards = new List<ICollection<ICollapsedRequest<string, string>>>
            {
                typeA,
                typeB
            };

            return shards;
        }
    }

    private sealed class TestGloballyScopedRequestCollapser : TestRequestCollapser
    {
        public TestGloballyScopedRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value)
            : base(output, RequestCollapserScope.Global, timer, value)
        {
        }
    }

    private sealed class TestRequestCollapserWithFaultyCreateCommand : TestRequestCollapser
    {
        public TestRequestCollapserWithFaultyCreateCommand(ITestOutputHelper output, TestCollapserTimer timer, string value)
            : base(output, timer, value)
        {
        }

        protected override HystrixCommand<List<string>> CreateCommand(ICollection<ICollapsedRequest<string, string>> requests)
        {
            throw new Exception("some failure");
        }
    }

    private sealed class TestRequestCollapserWithShortCircuitedCommand : TestRequestCollapser
    {
        public TestRequestCollapserWithShortCircuitedCommand(ITestOutputHelper output, TestCollapserTimer timer, string value)
            : base(output, timer, value)
        {
        }

        protected override HystrixCommand<List<string>> CreateCommand(ICollection<ICollapsedRequest<string, string>> requests)
        {
            // args don't matter as it's short-circuited
            return new ShortCircuitedCommand(Output);
        }
    }

    private sealed class TestRequestCollapserWithFaultyMapToResponse : TestRequestCollapser
    {
        public TestRequestCollapserWithFaultyMapToResponse(ITestOutputHelper output, TestCollapserTimer timer, string value)
            : base(output, timer, value)
        {
        }

        protected override void MapResponseToRequests(List<string> batchResponse, ICollection<ICollapsedRequest<string, string>> requests)
        {
            // pretend we blow up with an NPE
            throw new Exception("batchResponse was null and we blew up");
        }
    }

    private sealed class TestCollapserCommand : TestHystrixCommand<List<string>>
    {
        private readonly ICollection<ICollapsedRequest<string, string>> _requests;
        private readonly ITestOutputHelper _outputHelper;

        public TestCollapserCommand(ITestOutputHelper outputHelper, ICollection<ICollapsedRequest<string, string>> requests)
            : base(TestPropsBuilder().SetCommandOptionDefaults(GetCommandOptions()))
        {
            _requests = requests;
            _outputHelper = outputHelper;
        }

        protected override List<string> Run()
        {
            _outputHelper.WriteLine(">>> TestCollapserCommand run() ... batch size: " + _requests.Count);

            // simulate a batch request
            var response = new List<string>();

            foreach (ICollapsedRequest<string, string> request in _requests)
            {
                if (request.Argument == null)
                {
                    response.Add("NULL");
                }
                else
                {
                    if (request.Argument.Equals("FAILURE"))
                    {
                        throw new Exception("Simulated Error");
                    }

                    if (request.Argument.Equals("TIMEOUT"))
                    {
                        try
                        {
                            Time.Wait(800);
                        }
                        catch (Exception e)
                        {
                            _outputHelper.WriteLine(e.ToString());
                        }
                    }

                    response.Add(request.Argument);
                }
            }

            return response;
        }

        private static IHystrixCommandOptions GetCommandOptions()
        {
            HystrixCommandOptions opts = HystrixCommandOptionsTest.GetUnitTestOptions();
            opts.ExecutionTimeoutInMilliseconds = 500;
            return opts;
        }
    }

    private sealed class SuccessfulCacheableCollapsedCommand : TestRequestCollapser
    {
        private readonly bool _cacheEnabled;

        protected override string CacheKey
        {
            get
            {
                if (_cacheEnabled)
                {
                    return $"aCacheKey_{Value}";
                }

                return null;
            }
        }

        public SuccessfulCacheableCollapsedCommand(ITestOutputHelper output, TestCollapserTimer timer, string value, bool cacheEnabled)
            : base(output, timer, value)
        {
            _cacheEnabled = cacheEnabled;
        }

        public SuccessfulCacheableCollapsedCommand(ITestOutputHelper output, TestCollapserTimer timer, string value, bool cacheEnabled,
            ConcurrentQueue<HystrixCommand<List<string>>> executionLog)
            : base(output, timer, value, executionLog)
        {
            _cacheEnabled = cacheEnabled;
        }
    }

    private sealed class ShortCircuitedCommand : HystrixCommand<List<string>>
    {
        private readonly ITestOutputHelper _output;

        public ShortCircuitedCommand(ITestOutputHelper output)
            : base(GetCommandOptions())
        {
            _output = output;
        }

        protected override List<string> Run()
        {
            _output.WriteLine("*** execution (this shouldn't happen)");

            // this won't ever get called as we're forcing short-circuiting
            var values = new List<string>
            {
                "hello"
            };

            return values;
        }

        private static IHystrixCommandOptions GetCommandOptions()
        {
            IHystrixCommandOptions opts = HystrixCommandOptionsTest.GetUnitTestOptions();

            opts.CircuitBreakerForceOpen = true;
            opts.GroupKey = HystrixCommandGroupKeyDefault.AsKey("shortCircuitedCommand");

            return opts;
        }
    }

    private sealed class FireAndForgetCommand : HystrixCommand<object>
    {
        private readonly ITestOutputHelper _output;

        public FireAndForgetCommand(ITestOutputHelper output, List<int> values)
            : base(GetCommandOptions())
        {
            _output = output;
        }

        protected override object Run()
        {
            _output.WriteLine("*** FireAndForgetCommand execution: " + Thread.CurrentThread.ManagedThreadId);
            return null;
        }

        private static IHystrixCommandOptions GetCommandOptions()
        {
            IHystrixCommandOptions opts = HystrixCommandOptionsTest.GetUnitTestOptions();
            opts.GroupKey = HystrixCommandGroupKeyDefault.AsKey("fireAndForgetCommand");

            return opts;
        }
    }

    private sealed class TestCollapserTimer : ICollapserTimer
    {
        private readonly object _lock = new();
        private readonly ITestOutputHelper _output;
        public readonly ConcurrentDictionary<ATask, ATask> Tasks = new();

        public TestCollapserTimer(ITestOutputHelper output)
        {
            _output = output;
        }

        public TimerReference AddListener(ITimerListener collapseTask)
        {
            var listener = new TestTimerListener(collapseTask);
            var t = new ATask(_output, listener);
            Tasks.TryAdd(t, t);

            var reference = new TestTimerReference(this, listener, TimeSpan.FromMilliseconds(0));
            return reference;
        }

        public void IncrementTime(int timeInMilliseconds)
        {
            lock (_lock)
            {
                foreach (ATask t in Tasks.Values)
                {
                    t.IncrementTime(timeInMilliseconds);
                }
            }
        }
    }

    private sealed class TestTimerReference : TimerReference
    {
        private readonly TestCollapserTimer _collapserTimer;

        public TestTimerReference(TestCollapserTimer collapserTimer, ITimerListener listener, TimeSpan period)
            : base(listener, period)
        {
            _collapserTimer = collapserTimer;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Called when context is disposed
                foreach (ATask v in _collapserTimer.Tasks.Values)
                {
                    if (v.Task == Listener)
                    {
                        _ = _collapserTimer.Tasks.TryRemove(v, out _);
                    }
                }
            }

            base.Dispose(disposing);
        }
    }

    private sealed class ATask
    {
        private readonly int _delay = 10;
        private readonly object _lock = new();
        private readonly ITestOutputHelper _output;
        public readonly TestTimerListener Task;

        // our relative time that we'll use
        public volatile int Time;
        public volatile int ExecutionCount;

        public ATask(ITestOutputHelper output, TestTimerListener task)
        {
            Task = task;
            _output = output;
        }

        public void IncrementTime(int timeInMilliseconds)
        {
            lock (_lock)
            {
                Time += timeInMilliseconds;

                if (Task != null)
                {
                    if (ExecutionCount == 0)
                    {
                        _output.WriteLine("ExecutionCount 0 => Time: " + Time + " Delay: " + _delay);

                        if (Time >= _delay)
                        {
                            // first execution, we're past the delay time
                            ExecuteTask();
                        }
                    }
                    else
                    {
                        _output.WriteLine("ExecutionCount 1+ => Time: " + Time + " Delay: " + _delay);

                        if (Time >= _delay)
                        {
                            // subsequent executions, we're past the interval time
                            ExecuteTask();
                        }
                    }
                }
            }
        }

        private void ExecuteTask()
        {
            lock (_lock)
            {
                _output.WriteLine("Executing task ...");
                Task.Tick();
                Time = 0; // we reset time after each execution
                ExecutionCount++;
                _output.WriteLine("executionCount: " + ExecutionCount);
            }
        }
    }

    private sealed class TestTimerListener : ITimerListener
    {
        public readonly ITimerListener ActualListener;
        public readonly AtomicInteger Count = new();

        public int IntervalTimeInMilliseconds => 10;

        public TestTimerListener(ITimerListener actual)
        {
            ActualListener = actual;
        }

        public void Tick()
        {
            Count.IncrementAndGet();
            ActualListener.Tick();
        }
    }

    private sealed class TestCollapserWithVoidResponseType : HystrixCollapser<object, object, int>
    {
        private readonly ITestOutputHelper _output;

        public override int RequestArgument { get; }

        public TestCollapserWithVoidResponseType(ITestOutputHelper output, ICollapserTimer timer, int value)
            : base(CollapserKeyFromString(timer), RequestCollapserScope.Request, timer, GetCollapserOptions(CollapserKeyFromString(timer)))
        {
            RequestArgument = value;
            _output = output;
        }

        private static IHystrixCollapserOptions GetCollapserOptions(IHystrixCollapserKey key)
        {
            var opts = new HystrixCollapserOptions(key)
            {
                MaxRequestsInBatch = 1000,
                TimerDelayInMilliseconds = 50
            };

            return opts;
        }

        protected override HystrixCommand<object> CreateCommand(ICollection<ICollapsedRequest<object, int>> requests)
        {
            var args = new List<int>();

            foreach (ICollapsedRequest<object, int> request in requests)
            {
                args.Add(request.Argument);
            }

            return new FireAndForgetCommand(_output, args);
        }

        protected override void MapResponseToRequests(object batchResponse, ICollection<ICollapsedRequest<object, int>> requests)
        {
            foreach (ICollapsedRequest<object, int> r in requests)
            {
                r.Response = null;
            }
        }
    }

    private sealed class TestCollapserWithVoidResponseTypeAndMissingMapResponseToRequests : HystrixCollapser<object, object, int>
    {
        private readonly ITestOutputHelper _output;

        public override int RequestArgument { get; }

        public TestCollapserWithVoidResponseTypeAndMissingMapResponseToRequests(ITestOutputHelper output, ICollapserTimer timer, int value)
            : base(CollapserKeyFromString(timer), RequestCollapserScope.Request, timer, GetCollapserOptions(CollapserKeyFromString(timer)))
        {
            RequestArgument = value;
            _output = output;
        }

        protected override HystrixCommand<object> CreateCommand(ICollection<ICollapsedRequest<object, int>> requests)
        {
            var args = new List<int>();

            foreach (ICollapsedRequest<object, int> request in requests)
            {
                args.Add(request.Argument);
            }

            return new FireAndForgetCommand(_output, args);
        }

        protected override void MapResponseToRequests(object batchResponse, ICollection<ICollapsedRequest<object, int>> requests)
        {
        }

        private static IHystrixCollapserOptions GetCollapserOptions(IHystrixCollapserKey key)
        {
            var opts = new HystrixCollapserOptions(key)
            {
                MaxRequestsInBatch = 1000,
                TimerDelayInMilliseconds = 50
            };

            return opts;
        }
    }

    private sealed class Pair<TAa, TBb>
    {
        public readonly TAa Aa;
        public readonly TBb Bb;

        public Pair(TAa a, TBb b)
        {
            Aa = a;
            Bb = b;
        }
    }

    private sealed class MyCommand : HystrixCommand<List<Pair<string, int>>>
    {
        private readonly List<string> _args;
        private readonly ITestOutputHelper _output;

        public MyCommand(ITestOutputHelper output, List<string> args)
            : base(GetCommandOptions())
        {
            _args = args;
            _output = output;
        }

        protected override List<Pair<string, int>> Run()
        {
            _output.WriteLine("Executing batch command on : " + Thread.CurrentThread.ManagedThreadId + " with args : " + _args);
            var results = new List<Pair<string, int>>();

            foreach (string arg in _args)
            {
                results.Add(new Pair<string, int>(arg, int.Parse(arg)));
            }

            return results;
        }

        private static IHystrixCommandOptions GetCommandOptions()
        {
            var opts = new HystrixCommandOptions
            {
                GroupKey = HystrixCommandGroupKeyDefault.AsKey("BATCH")
            };

            return opts;
        }
    }

    private sealed class MyCollapser : HystrixCollapser<List<Pair<string, int>>, int, string>
    {
        private readonly ITestOutputHelper _output;

        protected override string CacheKey { get; }

        public override string RequestArgument => CacheKey;

        public MyCollapser(ITestOutputHelper output, string arg, bool reqCacheEnabled)
            : base(HystrixCollapserKeyDefault.AsKey("UNITTEST"), RequestCollapserScope.Request, new RealCollapserTimer(), GetCollapserOptions(reqCacheEnabled),
                HystrixCollapserMetrics.GetInstance(HystrixCollapserKeyDefault.AsKey("UNITTEST"), GetCollapserOptions(reqCacheEnabled)))
        {
            CacheKey = arg;
            _output = output;
        }

        public MyCollapser(ITestOutputHelper output, string arg, bool reqCacheEnabled, int timerDelayInMilliseconds)
            : base(HystrixCollapserKeyDefault.AsKey("UNITTEST"), RequestCollapserScope.Request, new RealCollapserTimer(),
                GetCollapserOptions(reqCacheEnabled, timerDelayInMilliseconds),
                HystrixCollapserMetrics.GetInstance(HystrixCollapserKeyDefault.AsKey("UNITTEST"),
                    GetCollapserOptions(reqCacheEnabled, timerDelayInMilliseconds)))
        {
            CacheKey = arg;
            _output = output;
        }

        protected override HystrixCommand<List<Pair<string, int>>> CreateCommand(ICollection<ICollapsedRequest<int, string>> requests)
        {
            var args = new List<string>(requests.Count);

            foreach (ICollapsedRequest<int, string> req in requests)
            {
                args.Add(req.Argument);
            }

            return new MyCommand(_output, args);
        }

        protected override void MapResponseToRequests(List<Pair<string, int>> batchResponse, ICollection<ICollapsedRequest<int, string>> requests)
        {
            foreach (Pair<string, int> pair in batchResponse)
            {
                foreach (ICollapsedRequest<int, string> collapsedReq in requests)
                {
                    if (collapsedReq.Argument.Equals(pair.Aa))
                    {
                        collapsedReq.Response = pair.Bb;
                    }
                }
            }
        }

        private static IHystrixCollapserOptions GetCollapserOptions(bool reqCacheEnabled)
        {
            var opts = new HystrixCollapserOptions(HystrixCollapserKeyDefault.AsKey("UNITTEST"))
            {
                RequestCacheEnabled = reqCacheEnabled
            };

            return opts;
        }

        private static IHystrixCollapserOptions GetCollapserOptions(bool reqCacheEnabled, int timerDelayInMilliseconds)
        {
            var opts = new HystrixCollapserOptions(HystrixCollapserKeyDefault.AsKey("UNITTEST"))
            {
                RequestCacheEnabled = reqCacheEnabled,
                TimerDelayInMilliseconds = timerDelayInMilliseconds
            };

            return opts;
        }
    }

    private sealed class TestSubscriber<T> : ObserverBase<T>
    {
        private readonly CountdownEvent _latch = new(1);
        private readonly ITestOutputHelper _output;
        private int _completions;

        public List<T> OnNextEvents { get; }

        public List<Notification<T>> OnCompletedEvents
        {
            get
            {
                int c = _completions;
                var results = new List<Notification<T>>();

                for (int i = 0; i < c; i++)
                {
                    results.Add(Notification.CreateOnCompleted<T>());
                }

                return results;
            }
        }

        public List<Exception> OnErrorEvents { get; }

        public bool IsUnsubscribed { get; set; }

        public IDisposable Subscription { get; set; }

        public TestSubscriber(ITestOutputHelper output)
        {
            _output = output;
            OnNextEvents = new List<T>();
            OnErrorEvents = new List<Exception>();
        }

        public void Unsubscribe()
        {
            if (Subscription != null)
            {
                Subscription.Dispose();
            }

            IsUnsubscribed = true;
        }

        public void AwaitTerminalEvent(int timeInMilliseconds)
        {
            try
            {
                _latch.Wait(timeInMilliseconds);
            }
            catch (Exception e)
            {
                Assert.False(true, e.Message);
            }
        }

        public void AssertNoErrors()
        {
            if (OnErrorEvents.Count > 0)
            {
                Assert.False(true, "Unexpected onError events");
            }
        }

        public void AssertValues(params T[] check)
        {
            foreach (T v in check)
            {
                if (!OnNextEvents.Contains(v))
                {
                    Assert.False(true, $"Value not found: {v}");
                }
            }
        }

        internal void AssertError(Type et)
        {
            if (OnErrorEvents.Count != 1)
            {
                Assert.False(true, "No errors or multiple errors");
            }

            Exception e = OnErrorEvents[0];
            TypeInfo eTypeInfo = e.GetType().GetTypeInfo();
            TypeInfo etTypeInfo = et.GetTypeInfo();

            if (eTypeInfo.Equals(etTypeInfo) || eTypeInfo.IsSubclassOf(et))
            {
                return;
            }

            Assert.False(true, $"Exceptions differ, Expected: {et} Found: {e.GetType()}");
        }

        internal void AssertNoValues()
        {
            int c = OnNextEvents.Count;

            if (c != 0)
            {
                Assert.False(true, $"No onNext events expected yet some received: {c}");
            }
        }

        internal void AssertCompleted()
        {
            int s = _completions;

            if (s == 0)
            {
                Assert.False(true, "Not completed!");
            }
            else if (s > 1)
            {
                Assert.False(true, $"Completed multiple times: {s}");
            }
        }

        protected override void OnCompletedCore()
        {
            _output.WriteLine("OnCompleted @ " + Time.CurrentTimeMillis);
            _completions++;
            _latch.SignalEx();
        }

        protected override void OnErrorCore(Exception error)
        {
            _output.WriteLine("OnError @ " + Time.CurrentTimeMillis + " : " + error.Message);
            OnErrorEvents.Add(error);
            _latch.SignalEx();
        }

        protected override void OnNextCore(T value)
        {
            _output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + value);
            OnNextEvents.Add(value);
        }
    }
}
