﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.CircuitBreaker.Hystrix.Collapser;
using Steeltoe.CircuitBreaker.Hystrix.Util;
using Steeltoe.Common.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Test
{
    public class HystrixCollapserTest : HystrixTestBase
    {
        private readonly ITestOutputHelper output;

        public HystrixCollapserTest(ITestOutputHelper output)
            : base()
        {
            this.output = output;
        }

        public override void Dispose()
        {
            base.Dispose();
            HystrixCollapserMetrics.Reset();
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestTwoRequests()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(output, timer, 1);
            var response1 = collapser1.ExecuteAsync();
            HystrixCollapser<List<string>, string, string> collapser2 = new TestRequestCollapser(output, timer, 2);
            var response2 = collapser2.ExecuteAsync();

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

            var metrics = collapser1.Metrics;
            Assert.True(metrics == collapser2.Metrics);

            var command = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.First();
            Assert.Equal(2, command.NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestMultipleBatches()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(output, timer, 1);
            var response1 = collapser1.ExecuteAsync();
            var response2 = new TestRequestCollapser(output, timer, 2).ExecuteAsync();
            timer.IncrementTime(10); // let time pass that equals the default delay/period

            Assert.Equal("1", GetResult(response1, 1000));
            Assert.Equal("2", GetResult(response2, 1000));

            // now request more
            var response3 = new TestRequestCollapser(output, timer, 3).ExecuteAsync();
            timer.IncrementTime(10); // let time pass that equals the default delay/period

            Assert.Equal("3", GetResult(response3, 1000));

            // we should have had it execute twice now
            Assert.Equal(2, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands;

            Assert.Equal(2, cmdIterator.First().NumberCollapsed);
            Assert.Equal(1, cmdIterator.Last().NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestMaxRequestsInBatch()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(output, timer, 1, 2, 10);
            HystrixCollapser<List<string>, string, string> collapser2 = new TestRequestCollapser(output, timer, 2, 2, 10);
            HystrixCollapser<List<string>, string, string> collapser3 = new TestRequestCollapser(output, timer, 3, 2, 10);
            output.WriteLine("*** " + Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " Constructed the collapsers");
            var response1 = collapser1.ExecuteAsync();
            var response2 = collapser2.ExecuteAsync();
            var response3 = collapser3.ExecuteAsync();
            output.WriteLine("*** " + Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " queued the collapsers");

            timer.IncrementTime(10); // let time pass that equals the default delay/period
            output.WriteLine("*** " + Time.CurrentTimeMillis + " : " + Thread.CurrentThread.ManagedThreadId + " incremented the virtual timer");

            Assert.Equal("1", GetResult(response1, 1000));
            Assert.Equal("2", GetResult(response2, 1000));
            Assert.Equal("3", GetResult(response3, 1000));

            // we should have had it execute twice because the batch size was 2
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(2, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands;
            Assert.Equal(2, cmdIterator.First().NumberCollapsed);
            Assert.Equal(1, cmdIterator.Last().NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestRequestsOverTime()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(output, timer, 1);
            var response1 = collapser1.ExecuteAsync();
            timer.IncrementTime(5);
            var response2 = new TestRequestCollapser(output, timer, 2).ExecuteAsync();
            timer.IncrementTime(8);

            // should execute here
            var response3 = new TestRequestCollapser(output, timer, 3).ExecuteAsync();
            timer.IncrementTime(6);
            var response4 = new TestRequestCollapser(output, timer, 4).ExecuteAsync();
            timer.IncrementTime(8);

            // should execute here
            var response5 = new TestRequestCollapser(output, timer, 5).ExecuteAsync();
            timer.IncrementTime(10);

            // should execute here

            // wait for all tasks to complete
            Assert.Equal("1", GetResult(response1, 1000));
            Assert.Equal("2", GetResult(response2, 1000));
            Assert.Equal("3", GetResult(response3, 1000));
            Assert.Equal("4", GetResult(response4, 1000));
            Assert.Equal("5", GetResult(response5, 1000));

            Assert.Equal(3, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToArray();
            Assert.Equal(2, cmdIterator[0].NumberCollapsed);
            Assert.Equal(2, cmdIterator[1].NumberCollapsed);
            Assert.Equal(1, cmdIterator[2].NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestDuplicateArgumentsWithRequestCachingOn()
        {
            var num = 10;

            var observables = new List<IObservable<int>>();
            for (var i = 0; i < num; i++)
            {
                var c = new MyCollapser(output, "5", true);
                var observable = c.ToObservable();
                observables.Add(observable);
            }

            var subscribers = new List<TestSubscriber<int>>();
            foreach (var o in observables)
            {
                var sub = new TestSubscriber<int>(output);
                subscribers.Add(sub);
                o.Subscribe(sub);
            }

            Time.Wait(100);

            // all subscribers should receive the same value
            foreach (var sub in subscribers)
            {
                sub.AwaitTerminalEvent(1000);
                output.WriteLine("Subscriber received : " + sub.OnNextEvents.Count);
                sub.AssertNoErrors();
                sub.AssertValues(5);
            }
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestDuplicateArgumentsWithRequestCachingOff()
        {
            var num = 10;

            var observables = new List<IObservable<int>>();
            for (var i = 0; i < num; i++)
            {
                var c = new MyCollapser(output, "5", false, 500);
                observables.Add(c.ToObservable());
            }

            var subscribers = new List<TestSubscriber<int>>();
            foreach (var o in observables)
            {
                var sub = new TestSubscriber<int>(output);
                subscribers.Add(sub);
                o.Subscribe(sub);
            }

            // Wait to make sure batch ran
            Time.Wait(100);

            var numErrors = new AtomicInteger(0);
            var numValues = new AtomicInteger(0);

            // only the first subscriber should receive the value.
            // the others should get an error that the batch contains duplicates
            foreach (var sub in subscribers)
            {
                sub.AwaitTerminalEvent(1000);
                if (sub.OnCompletedEvents.Count == 0)
                {
                    output.WriteLine(Thread.CurrentThread.ManagedThreadId + " Error : " + sub.OnErrorEvents.Count);
                    sub.AssertError(typeof(ArgumentException));
                    sub.AssertNoValues();
                    numErrors.GetAndIncrement();
                }
                else
                {
                    output.WriteLine(Thread.CurrentThread.ManagedThreadId + " OnNext : " + sub.OnNextEvents.Count);
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
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestUnsubscribeFromSomeDuplicateArgsDoesNotRemoveFromBatch()
        {
            var num = 10;

            var observables = new List<IObservable<int>>();
            var collapsers = new List<MyCollapser>();
            for (var i = 0; i < num; i++)
            {
                var c = new MyCollapser(output, "5", true);
                collapsers.Add(c);
                var obs = c.ToObservable();
                observables.Add(obs);
            }

            var subscribers = new List<TestSubscriber<int>>();
            var subscriptions = new List<IDisposable>();

            foreach (var o in observables)
            {
                var sub = new TestSubscriber<int>(output);
                subscribers.Add(sub);
                var s = o.Subscribe(sub);
                sub.Subscription = s;
                subscriptions.Add(s);
            }

            // unsubscribe from all but 1
            for (var i = 0; i < num - 1; i++)
            {
                subscribers[i].Unsubscribe();
            }

            Time.Wait(100);

            // all subscribers with an active subscription should receive the same value
            foreach (var sub in subscribers)
            {
                if (!sub.IsUnsubscribed)
                {
                    sub.AwaitTerminalEvent(1000);
                    output.WriteLine("Subscriber received : " + sub.OnNextEvents.Count);
                    sub.AssertNoErrors();
                    sub.AssertValues(5);
                }
                else
                {
                    output.WriteLine("Subscriber is unsubscribed");
                }
            }
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestUnsubscribeOnOneDoesntKillBatch()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(output, timer, 1);
            var cts1 = new CancellationTokenSource();
            var cts2 = new CancellationTokenSource();
            var response1 = collapser1.ExecuteAsync(cts1.Token);
            var response2 = new TestRequestCollapser(output, timer, 2).ExecuteAsync(cts2.Token);

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

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands;
            Assert.Equal(1, cmdIterator.First().NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestShardedRequests()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestShardedRequestCollapser(output, timer, "1a");
            var response1 = collapser1.ExecuteAsync();
            var response2 = new TestShardedRequestCollapser(output, timer, "2b").ExecuteAsync();
            var response3 = new TestShardedRequestCollapser(output, timer, "3b").ExecuteAsync();
            var response4 = new TestShardedRequestCollapser(output, timer, "4a").ExecuteAsync();
            timer.IncrementTime(10); // let time pass that equals the default delay/period

            Assert.Equal("1a", GetResult(response1, 1000));
            Assert.Equal("2b", GetResult(response2, 1000));
            Assert.Equal("3b", GetResult(response3, 1000));
            Assert.Equal("4a", GetResult(response4, 1000));

            /* we should get 2 batches since it gets sharded */
            Assert.Equal(2, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands;
            Assert.Equal(2, cmdIterator.First().NumberCollapsed);
            Assert.Equal(2, cmdIterator.Last().NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestRequestScope()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(output, timer, "1");
            var response1 = collapser1.ExecuteAsync();
            var response2 = new TestRequestCollapser(output, timer, "2").ExecuteAsync();

            // simulate a new request
            RequestCollapserFactory.ResetRequest();

            var response3 = new TestRequestCollapser(output, timer, "3").ExecuteAsync();
            var response4 = new TestRequestCollapser(output, timer, "4").ExecuteAsync();

            timer.IncrementTime(10); // let time pass that equals the default delay/period

            Assert.Equal("1", GetResult(response1, 1000));
            Assert.Equal("2", GetResult(response2, 1000));
            Assert.Equal("3", GetResult(response3, 1000));
            Assert.Equal("4", GetResult(response4, 1000));

            // 2 different batches should execute, 1 per request
            Assert.Equal(2, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands;
            Assert.Equal(2, cmdIterator.First().NumberCollapsed);
            Assert.Equal(2, cmdIterator.Last().NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestGlobalScope()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestGloballyScopedRequestCollapser(output, timer, "1");
            var response1 = collapser1.ExecuteAsync();
            var response2 = new TestGloballyScopedRequestCollapser(output, timer, "2").ExecuteAsync();

            // simulate a new request
            RequestCollapserFactory.ResetRequest();

            var response3 = new TestGloballyScopedRequestCollapser(output, timer, "3").ExecuteAsync();
            var response4 = new TestGloballyScopedRequestCollapser(output, timer, "4").ExecuteAsync();

            timer.IncrementTime(10); // let time pass that equals the default delay/period
            Assert.Equal("1", GetResult(response1, 1000));
            Assert.Equal("2", GetResult(response2, 1000));
            Assert.Equal("3", GetResult(response3, 1000));
            Assert.Equal("4", GetResult(response4, 1000));

            // despite having cleared the cache in between we should have a single execution because this is on the global not request cache
            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands;
            Assert.Equal(4, cmdIterator.First().NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestErrorHandlingViaFutureException()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapserWithFaultyCreateCommand(output, timer, "1");
            var response1 = collapser1.ExecuteAsync();
            var response2 = new TestRequestCollapserWithFaultyCreateCommand(output, timer, "2").ExecuteAsync();
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
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestErrorHandlingWhenMapToResponseFails()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapserWithFaultyMapToResponse(output, timer, "1");
            var response1 = collapser1.ExecuteAsync();
            var response2 = new TestRequestCollapserWithFaultyMapToResponse(output, timer, "2").ExecuteAsync();
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

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands;
            Assert.Equal(2, cmdIterator.First().NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestRequestVariableLifecycle1()
        {
            // do actual work
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(output, timer, 1);
            var response1 = collapser1.ExecuteAsync();
            timer.IncrementTime(5);
            var response2 = new TestRequestCollapser(output, timer, 2).ExecuteAsync();
            timer.IncrementTime(8);

            // should execute here
            var response3 = new TestRequestCollapser(output, timer, 3).ExecuteAsync();
            timer.IncrementTime(6);
            var response4 = new TestRequestCollapser(output, timer, 4).ExecuteAsync();
            timer.IncrementTime(8);

            // should execute here
            var response5 = new TestRequestCollapser(output, timer, 5).ExecuteAsync();
            timer.IncrementTime(10);

            // should execute here

            // wait for all tasks to complete
            Assert.Equal("1", GetResult(response1, 1000));
            Assert.Equal("2", GetResult(response2, 1000));
            Assert.Equal("3", GetResult(response3, 1000));
            Assert.Equal("4", GetResult(response4, 1000));
            Assert.Equal("5", GetResult(response5, 1000));

            // each task should have been executed 3 times
            foreach (var t in timer.Tasks.Values)
            {
                Assert.Equal(3, t.Task.Count.Value);
            }

            output.WriteLine("timer.tasks.size() A: " + timer.Tasks.Count);
            output.WriteLine("tasks in test: " + timer.Tasks);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(2, cmdIterator[0].NumberCollapsed);
            Assert.Equal(2, cmdIterator[1].NumberCollapsed);
            Assert.Equal(1, cmdIterator[2].NumberCollapsed);

            output.WriteLine("timer.tasks.size() B: " + timer.Tasks.Count);
            var rv = RequestCollapserFactory.GetRequestVariable<List<string>, string, string>(new TestRequestCollapser(output, timer, 1).CollapserKey.Name);

            context.Dispose();

            Assert.NotNull(rv);

            // they should have all been removed as part of ThreadContext.remove()
            Assert.Empty(timer.Tasks);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestRequestVariableLifecycle2()
        {
            var timer = new TestCollapserTimer(output);
            var responses = new ConcurrentDictionary<Task<string>, Task<string>>();
            var threads = new List<Task>();

            // kick off work (simulating a single request with multiple threads)
            for (var t = 0; t < 5; t++)
            {
                var outerLoop = t;
                var th = new Task(
                    () =>
                    {
                        for (var i = 0; i < 100; i++)
                        {
                            var uniqueInt = (outerLoop * 100) + i;
                            var tsk = new TestRequestCollapser(output, timer, uniqueInt).ExecuteAsync();
                            responses.TryAdd(tsk, tsk);
                        }
                    },
                    CancellationToken.None,
                    TaskCreationOptions.LongRunning);
                th.Start();
                threads.Add(th);
            }

            Task.WaitAll(threads.ToArray());

            // we expect 5 threads * 100 responses each
            Assert.Equal(500, responses.Count);

            foreach (var f in responses.Values)
            {
                // they should not be done yet because the counter hasn't incremented
                Assert.False(f.IsCompleted);
            }

            timer.IncrementTime(5);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(output, timer, 2);
            var response2 = collapser1.ExecuteAsync();
            timer.IncrementTime(8);

            // should execute here
            var response3 = new TestRequestCollapser(output, timer, 3).ExecuteAsync();
            timer.IncrementTime(6);
            var response4 = new TestRequestCollapser(output, timer, 4).ExecuteAsync();
            timer.IncrementTime(8);

            // should execute here
            var response5 = new TestRequestCollapser(output, timer, 5).ExecuteAsync();
            timer.IncrementTime(10);

            // should execute here

            // wait for all tasks to complete
            foreach (var f in responses.Values)
            {
                GetResult(f, 1000);
            }

            Assert.Equal("2", GetResult(response2, 1000));
            Assert.Equal("3", GetResult(response3, 1000));
            Assert.Equal("4", GetResult(response4, 1000));
            Assert.Equal("5", GetResult(response5, 1000));

            // each task should have been executed 3 times
            foreach (var t in timer.Tasks.Values)
            {
                Assert.Equal(3, t.Task.Count.Value);
            }

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(500, cmdIterator[0].NumberCollapsed);
            Assert.Equal(2, cmdIterator[1].NumberCollapsed);
            Assert.Equal(1, cmdIterator[2].NumberCollapsed);
            var rv = RequestCollapserFactory.GetRequestVariable<List<string>, string, string>(new TestRequestCollapser(output, timer, 1).CollapserKey.Name);

            context.Dispose();

            Assert.NotNull(rv);

            // they should have all been removed as part of ThreadContext.remove()
            Assert.Empty(timer.Tasks);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestRequestCache1()
        {
            var timer = new TestCollapserTimer(output);
            var command1 = new SuccessfulCacheableCollapsedCommand(output, timer, "A", true);
            var command2 = new SuccessfulCacheableCollapsedCommand(output, timer, "A", true);

            var f1 = command1.ExecuteAsync();
            var f2 = command2.ExecuteAsync();

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

            var f3 = command1.ExecuteAsync();

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

            var command = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.AsEnumerable().First();
            output.WriteLine("command.getExecutionEvents(): " + command.ExecutionEvents.Count);
            Assert.Equal(2, command.ExecutionEvents.Count);
            Assert.Contains(HystrixEventType.SUCCESS, command.ExecutionEvents);
            Assert.Contains(HystrixEventType.COLLAPSED, command.ExecutionEvents);

            Assert.Equal(1, command.NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestRequestCache2()
        {
            var timer = new TestCollapserTimer(output);
            var command1 = new SuccessfulCacheableCollapsedCommand(output, timer, "A", true);
            var command2 = new SuccessfulCacheableCollapsedCommand(output, timer, "B", true);

            var f1 = command1.ExecuteAsync();
            var f2 = command2.ExecuteAsync();

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

            var f3 = command1.ExecuteAsync();
            var f4 = command2.ExecuteAsync();

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

            var command = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.AsEnumerable().First();
            output.WriteLine("command.getExecutionEvents(): " + command.ExecutionEvents.Count);
            Assert.Equal(2, command.ExecutionEvents.Count);
            Assert.Contains(HystrixEventType.SUCCESS, command.ExecutionEvents);
            Assert.Contains(HystrixEventType.COLLAPSED, command.ExecutionEvents);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestRequestCache3()
        {
            var timer = new TestCollapserTimer(output);
            var command1 = new SuccessfulCacheableCollapsedCommand(output, timer, "A", true);
            var command2 = new SuccessfulCacheableCollapsedCommand(output, timer, "B", true);
            var command3 = new SuccessfulCacheableCollapsedCommand(output, timer, "B", true);

            var f1 = command1.ExecuteAsync();
            var f2 = command2.ExecuteAsync();
            var f3 = command3.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            try
            {
                Assert.Equal("A", GetResult(f1, 1000));
                Assert.Equal("B", GetResult(f2, 1000));
                Assert.Equal("B", GetResult(f3, 1000));
            }
            catch (Exception)
            {
                throw;
            }

            var f4 = command1.ExecuteAsync();
            var f5 = command2.ExecuteAsync();
            var f6 = command3.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            try
            {
                Assert.Equal("A", GetResult(f4, 1000));
                Assert.Equal("B", GetResult(f5, 1000));
                Assert.Equal("B", GetResult(f6, 1000));
            }
            catch (Exception)
            {
                throw;
            }

            // we should still have executed only one command
            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            var command = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList()[0];
            Assert.Equal(2, command.ExecutionEvents.Count);
            Assert.Contains(HystrixEventType.SUCCESS, command.ExecutionEvents);
            Assert.Contains(HystrixEventType.COLLAPSED, command.ExecutionEvents);

            Assert.Equal(2, command.NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestNoRequestCache3()
        {
            var timer = new TestCollapserTimer(output);
            var command1 = new SuccessfulCacheableCollapsedCommand(output, timer, "A", false);
            var command2 = new SuccessfulCacheableCollapsedCommand(output, timer, "B", false);
            var command3 = new SuccessfulCacheableCollapsedCommand(output, timer, "B", false);

            var f1 = command1.ExecuteAsync();
            var f2 = command2.ExecuteAsync();
            var f3 = command3.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            try
            {
                Assert.Equal("A", GetResult(f1, 1000));
                Assert.Equal("B", GetResult(f2, 1000));
                Assert.Equal("B", GetResult(f3, 1000));
            }
            catch (Exception)
            {
                throw;
            }

            var f4 = command1.ExecuteAsync();
            var f5 = command2.ExecuteAsync();
            var f6 = command3.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            try
            {
                Assert.Equal("A", GetResult(f4, 1000));
                Assert.Equal("B", GetResult(f5, 1000));
                Assert.Equal("B", GetResult(f6, 1000));
            }
            catch (Exception)
            {
                throw;
            }

            // request caching is turned off on this so we expect 2 command executions
            Assert.Equal(2, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            // we expect to see it with SUCCESS and COLLAPSED and both
            var commandA = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList()[0];
            Assert.Equal(2, commandA.ExecutionEvents.Count);
            Assert.Contains(HystrixEventType.SUCCESS, commandA.ExecutionEvents);
            Assert.Contains(HystrixEventType.COLLAPSED, commandA.ExecutionEvents);

            // we expect to see it with SUCCESS and COLLAPSED and both
            var commandB = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList()[1];
            Assert.Equal(2, commandB.ExecutionEvents.Count);
            Assert.Contains(HystrixEventType.SUCCESS, commandB.ExecutionEvents);
            Assert.Contains(HystrixEventType.COLLAPSED, commandB.ExecutionEvents);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(2, cmdIterator[0].NumberCollapsed);  // 1 for A, 1 for B.  Batch contains only unique arguments (no duplicates)
            Assert.Equal(2, cmdIterator[1].NumberCollapsed);  // 1 for A, 1 for B.  Batch contains only unique arguments (no duplicates)
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestRequestCacheWithNullRequestArgument()
        {
            var commands = new ConcurrentQueue<HystrixCommand<List<string>>>();

            var timer = new TestCollapserTimer(output);
            var command1 = new SuccessfulCacheableCollapsedCommand(output, timer, null, true, commands);
            var command2 = new SuccessfulCacheableCollapsedCommand(output, timer, null, true, commands);

            var f1 = command1.ExecuteAsync();
            var f2 = command2.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            Assert.Equal("NULL", GetResult(f1, 1000));
            Assert.Equal("NULL", GetResult(f2, 1000));

            // it should have executed 1 command
            Assert.Single(commands);
            HystrixCommand<List<string>> peek = null;
            commands.TryPeek(out peek);
            Assert.Contains(HystrixEventType.SUCCESS, peek.ExecutionEvents);
            Assert.Contains(HystrixEventType.COLLAPSED, peek.ExecutionEvents);

            var f3 = command1.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            Assert.Equal("NULL", GetResult(f3, 1000));

            // it should still be 1 ... no new executions
            Assert.Single(commands);
            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(1, cmdIterator[0].NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestRequestCacheWithCommandError()
        {
            var commands = new ConcurrentQueue<HystrixCommand<List<string>>>();

            var timer = new TestCollapserTimer(output);
            var command1 = new SuccessfulCacheableCollapsedCommand(output, timer, "FAILURE", true, commands);
            var command2 = new SuccessfulCacheableCollapsedCommand(output, timer, "FAILURE", true, commands);

            var f1 = command1.ExecuteAsync();
            var f2 = command2.ExecuteAsync();

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
            HystrixCommand<List<string>> peek = null;
            commands.TryPeek(out peek);

            Assert.Contains(HystrixEventType.FAILURE, peek.ExecutionEvents);
            Assert.Contains(HystrixEventType.COLLAPSED, peek.ExecutionEvents);

            var f3 = command1.ExecuteAsync();

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

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(1, cmdIterator[0].NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestRequestCacheWithCommandTimeout()
        {
            var commands = new ConcurrentQueue<HystrixCommand<List<string>>>();

            var timer = new TestCollapserTimer(output);
            var command1 = new SuccessfulCacheableCollapsedCommand(output, timer, "TIMEOUT", true, commands);
            var command2 = new SuccessfulCacheableCollapsedCommand(output, timer, "TIMEOUT", true, commands);

            var f1 = command1.ExecuteAsync();
            var f2 = command2.ExecuteAsync();

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
            HystrixCommand<List<string>> peek = null;
            commands.TryPeek(out peek);
            Assert.Contains(HystrixEventType.TIMEOUT, peek.ExecutionEvents);
            Assert.Contains(HystrixEventType.COLLAPSED, peek.ExecutionEvents);

            var f3 = command1.ExecuteAsync();

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

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(1, cmdIterator[0].NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public async Task TestRequestWithCommandShortCircuited()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapserWithShortCircuitedCommand(output, timer, "1");
            var response1 = collapser1.Observe();
            var response2 = new TestRequestCollapserWithShortCircuitedCommand(output, timer, "2").Observe();
            timer.IncrementTime(10); // let time pass that equals the default delay/period

            try
            {
                await response1.FirstAsync();
                Assert.True(false, "we should have received an exception");
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());

                // what we expect
            }

            try
            {
                await response2.FirstAsync();
                Assert.True(false, "we should have received an exception");
            }
            catch (Exception e)
            {
                output.WriteLine(e.ToString());

                // what we expect
            }

            // it will execute once (short-circuited)
            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(2, cmdIterator[0].NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestVoidResponseTypeFireAndForgetCollapsing1()
        {
            var timer = new TestCollapserTimer(output);
            var collapser1 = new TestCollapserWithVoidResponseType(output, timer, 1);
            var response1 = collapser1.ExecuteAsync();
            var response2 = new TestCollapserWithVoidResponseType(output, timer, 2).ExecuteAsync();
            timer.IncrementTime(100); // let time pass that equals the default delay/period

            // normally someone wouldn't wait on these, but we need to make sure they do in fact return
            // and not block indefinitely in case someone does call get()
            Assert.Null(GetResult(response1, 1000));
            Assert.Null(GetResult(response2, 1000));

            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(2, cmdIterator[0].NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestVoidResponseTypeFireAndForgetCollapsing2()
        {
            var timer = new TestCollapserTimer(output);
            var collapser1 = new TestCollapserWithVoidResponseTypeAndMissingMapResponseToRequests(output, timer, 1);
            var response1 = collapser1.ExecuteAsync();
            new TestCollapserWithVoidResponseTypeAndMissingMapResponseToRequests(output, timer, 2).ExecuteAsync();
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

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(2, cmdIterator[0].NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestVoidResponseTypeFireAndForgetCollapsing3()
        {
            ICollapserTimer timer = new RealCollapserTimer();
            var collapser1 = new TestCollapserWithVoidResponseType(output, timer, 1);
            Assert.Null(collapser1.Execute());

            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(1, cmdIterator[0].NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestEarlyUnsubscribeExecutedViaToObservable()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(output, timer, 1);
            var response1 = collapser1.ToObservable();
            HystrixCollapser<List<string>, string, string> collapser2 = new TestRequestCollapser(output, timer, 2);
            var response2 = collapser2.ToObservable();

            var latch1 = new CountdownEvent(1);
            var latch2 = new CountdownEvent(1);

            var value1 = new AtomicReference<string>(null);
            var value2 = new AtomicReference<string>(null);

            var s1 = response1
                    .OnDispose(() =>
            {
                output.WriteLine(Time.CurrentTimeMillis + " : s1 Unsubscribed!");
                latch1.SignalEx();
            })
                    .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnNext : " + s);
                    value1.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnError : " + e);
                    latch1.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnCompleted!");
                    latch1.SignalEx();
                });

            var s2 = response2
                    .OnDispose(() =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : s2 Unsubscribed!");
                        latch2.SignalEx();
                    })
                    .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnNext : " + s);
                    value2.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnError : " + e);
                    latch2.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnCompleted!");
                    latch2.SignalEx();
                });

            s1.Dispose();

            timer.IncrementTime(10); // let time pass that equals the default delay/period

            Assert.True(latch1.Wait(1000));
            Assert.True(latch2.Wait(1000));

            Assert.Null(value1.Value);
            Assert.Equal("2", value2.Value);

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
            var metrics = collapser1.Metrics;
            Assert.True(metrics == collapser2.Metrics);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(1, cmdIterator[0].NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestEarlyUnsubscribeExecutedViaObserve()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(output, timer, 1);
            var response1 = collapser1.Observe();
            HystrixCollapser<List<string>, string, string> collapser2 = new TestRequestCollapser(output, timer, 2);
            var response2 = collapser2.Observe();

            var latch1 = new CountdownEvent(1);
            var latch2 = new CountdownEvent(1);

            var value1 = new AtomicReference<string>(null);
            var value2 = new AtomicReference<string>(null);

            var s1 = response1
                    .OnDispose(() =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : s1 Unsubscribed!");
                        latch1.SignalEx();
                    })
                    .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnNext : " + s);
                    value1.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnError : " + e);
                    latch1.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnCompleted!");
                    latch1.SignalEx();
                });

            var s2 = response2
                    .OnDispose(() =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : s2 Unsubscribed!");
                        latch2.SignalEx();
                    })
                    .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnNext : " + s);
                    value2.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnError : " + e);
                    latch2.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnCompleted!");
                    latch2.SignalEx();
                });
            s1.Dispose();

            timer.IncrementTime(10); // let time pass that equals the default delay/period

            Assert.True(latch1.Wait(1000));
            Assert.True(latch2.Wait(1000));

            Assert.Null(value1.Value);
            Assert.Equal("2", value2.Value);

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
            var metrics = collapser1.Metrics;
            Assert.True(metrics == collapser2.Metrics);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(1, cmdIterator[0].NumberCollapsed);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestEarlyUnsubscribeFromAllCancelsBatch()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(output, timer, 1);
            var response1 = collapser1.Observe();
            HystrixCollapser<List<string>, string, string> collapser2 = new TestRequestCollapser(output, timer, 2);
            var response2 = collapser2.Observe();

            var latch1 = new CountdownEvent(1);
            var latch2 = new CountdownEvent(1);

            var value1 = new AtomicReference<string>(null);
            var value2 = new AtomicReference<string>(null);

            var s1 = response1
                    .OnDispose(() =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : s1 Unsubscribed!");
                        latch1.SignalEx();
                    })
                    .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnNext : " + s);
                    value1.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnError : " + e);
                    latch1.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnCompleted!");
                    latch1.SignalEx();
                });

            var s2 = response2
                    .OnDispose(() =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : s2 Unsubscribed!");
                        latch2.SignalEx();
                    })
                    .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnNext : " + s);
                    value2.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnError : " + e);
                    latch2.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnCompleted!");
                    latch2.SignalEx();
                });

            s1.Dispose();
            s2.Dispose();

            timer.IncrementTime(10); // let time pass that equals the default delay/period

            Assert.True(latch1.Wait(1000));
            Assert.True(latch2.Wait(1000));

            Assert.Null(value1.Value);
            Assert.Null(value2.Value);

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(0, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestRequestThenCacheHitAndCacheHitUnsubscribed()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            var response1 = collapser1.Observe();
            HystrixCollapser<List<string>, string, string> collapser2 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            var response2 = collapser2.Observe();

            var latch1 = new CountdownEvent(1);
            var latch2 = new CountdownEvent(1);

            var value1 = new AtomicReference<string>(null);
            var value2 = new AtomicReference<string>(null);

            var s1 = response1
                    .OnDispose(() =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : s1 Unsubscribed!");
                        latch1.SignalEx();
                    })
                    .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnNext : " + s);
                    value1.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnError : " + e);
                    latch1.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnCompleted!");
                    latch1.SignalEx();
                });

            var s2 = response2
                    .OnDispose(() =>
                    {
                        output.WriteLine(Time.CurrentTimeMillis + " : s2 Unsubscribed!");
                        latch2.SignalEx();
                    })
                    .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnNext : " + s);
                    value2.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnError : " + e);
                    latch2.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnCompleted!");
                    latch2.SignalEx();
                });

            s2.Dispose();

            timer.IncrementTime(10); // let time pass that equals the default delay/period

            Assert.True(latch1.Wait(1000));
            Assert.True(latch2.Wait(1000));

            Assert.Equal("foo", value1.Value);
            Assert.Null(value2.Value);

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            AssertCommandExecutionEvents(cmdIterator[0], HystrixEventType.SUCCESS, HystrixEventType.COLLAPSED);
            Assert.Equal(1, cmdIterator[0].NumberCollapsed); // should only be 1 collapsed - other came from cache, then was cancelled
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestRequestThenCacheHitAndOriginalUnsubscribed()
        {
            // TODO:
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            var response1 = collapser1.Observe();
            HystrixCollapser<List<string>, string, string> collapser2 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            var response2 = collapser2.Observe();

            var latch1 = new CountdownEvent(1);
            var latch2 = new CountdownEvent(1);

            var value1 = new AtomicReference<string>(null);
            var value2 = new AtomicReference<string>(null);
            var s1 = response1
                .OnDispose(() =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 Unsubscribed!");
                    latch1.SignalEx();
                })
                .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnNext : " + s);
                    value1.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnError : " + e);
                    latch1.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnCompleted!");
                    latch1.SignalEx();
                });

            var s2 = response2
                .OnDispose(() =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 Unsubscribed!");
                    latch2.SignalEx();
                })
                .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnNext : " + s);
                    value2.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnError : " + e);
                    latch2.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnCompleted!");
                    latch2.SignalEx();
                });

            s1.Dispose();

            timer.IncrementTime(10); // let time pass that equals the default delay/period

            Assert.True(latch1.Wait(1000));
            Assert.True(latch2.Wait(1000));

            Assert.Null(value1.Value);
            Assert.Equal("foo", value2.Value);

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            AssertCommandExecutionEvents(cmdIterator[0], HystrixEventType.SUCCESS, HystrixEventType.COLLAPSED);
            Assert.Equal(1, cmdIterator[0].NumberCollapsed); // should only be 1 collapsed - other came from cache, then was cancelled
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]
        public void TestRequestThenTwoCacheHitsOriginalAndOneCacheHitUnsubscribed()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            var response1 = collapser1.Observe();
            HystrixCollapser<List<string>, string, string> collapser2 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            var response2 = collapser2.Observe();
            HystrixCollapser<List<string>, string, string> collapser3 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            var response3 = collapser3.Observe();

            var latch1 = new CountdownEvent(1);
            var latch2 = new CountdownEvent(1);
            var latch3 = new CountdownEvent(1);

            var value1 = new AtomicReference<string>(null);
            var value2 = new AtomicReference<string>(null);
            var value3 = new AtomicReference<string>(null);
            var s1 = response1
                .OnDispose(() =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 Unsubscribed!");
                    latch1.SignalEx();
                })
                .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnNext : " + s);
                    value1.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnError : " + e);
                    latch1.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnCompleted!");
                    latch1.SignalEx();
                });

            var s2 = response2
                .OnDispose(() =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 Unsubscribed!");
                    latch2.SignalEx();
                })
                .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnNext : " + s);
                    value2.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnError : " + e);
                    latch2.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnCompleted!");
                    latch2.SignalEx();
                });

            var s3 = response3
                .OnDispose(() =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s3 Unsubscribed!");
                    latch3.SignalEx();
                })
                .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s3 OnNext : " + s);
                    value3.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s3 OnError : " + e);
                    latch3.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s3 OnCompleted!");
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

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            var cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            AssertCommandExecutionEvents(cmdIterator[0], HystrixEventType.SUCCESS, HystrixEventType.COLLAPSED);
            Assert.Equal(1, cmdIterator[0].NumberCollapsed); // should only be 1 collapsed - other came from cache, then was cancelled
        }

        [Fact]
        [Trait("Category", "FlakyOnHostedAgents")]

        public void TestRequestThenTwoCacheHitsAllUnsubscribed()
        {
            var timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            var response1 = collapser1.Observe();
            HystrixCollapser<List<string>, string, string> collapser2 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            var response2 = collapser2.Observe();
            HystrixCollapser<List<string>, string, string> collapser3 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            var response3 = collapser3.Observe();

            var latch1 = new CountdownEvent(1);
            var latch2 = new CountdownEvent(1);
            var latch3 = new CountdownEvent(1);

            var value1 = new AtomicReference<string>(null);
            var value2 = new AtomicReference<string>(null);
            var value3 = new AtomicReference<string>(null);

            var s1 = response1
                .OnDispose(() =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 Unsubscribed!");
                    latch1.SignalEx();
                })
                .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnNext : " + s);
                    value1.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnError : " + e);
                    latch1.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s1 OnCompleted!");
                    latch1.SignalEx();
                });

            var s2 = response2
                .OnDispose(() =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 Unsubscribed!");
                    latch2.SignalEx();
                })
                .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnNext : " + s);
                    value2.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnError : " + e);
                    latch2.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s2 OnCompleted!");
                    latch2.SignalEx();
                });

            var s3 = response3
                .OnDispose(() =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s3 Unsubscribed!");
                    latch3.SignalEx();
                })
                .Subscribe(
                (s) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s3 OnNext : " + s);
                    value3.Value = s;
                },
                (e) =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s3 OnError : " + e);
                    latch3.SignalEx();
                },
                () =>
                {
                    output.WriteLine(Time.CurrentTimeMillis + " : s3 OnCompleted!");
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

            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(0, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
        }

        protected void AssertCommandExecutionEvents(IHystrixInvokableInfo command, params HystrixEventType[] expectedEventTypes)
        {
            var emitExpected = false;
            var expectedEmitCount = 0;

            var fallbackEmitExpected = false;
            var expectedFallbackEmitCount = 0;

            var condensedEmitExpectedEventTypes = new List<HystrixEventType>();

            foreach (var expectedEventType in expectedEventTypes)
            {
                if (expectedEventType.Equals(HystrixEventType.EMIT))
                {
                    if (!emitExpected)
                    {
                        // first EMIT encountered, add it to condensedEmitExpectedEventTypes
                        condensedEmitExpectedEventTypes.Add(HystrixEventType.EMIT);
                    }

                    emitExpected = true;
                    expectedEmitCount++;
                }
                else if (expectedEventType.Equals(HystrixEventType.FALLBACK_EMIT))
                {
                    if (!fallbackEmitExpected)
                    {
                        // first FALLBACK_EMIT encountered, add it to condensedEmitExpectedEventTypes
                        condensedEmitExpectedEventTypes.Add(HystrixEventType.FALLBACK_EMIT);
                    }

                    fallbackEmitExpected = true;
                    expectedFallbackEmitCount++;
                }
                else
                {
                    condensedEmitExpectedEventTypes.Add(expectedEventType);
                }
            }

            var actualEventTypes = command.ExecutionEvents;
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
            else
            {
                return default(T);
            }
        }

        private class TestRequestCollapser : HystrixCollapser<List<string>, string, string>
        {
            protected readonly string value;
            protected ConcurrentQueue<HystrixCommand<List<string>>> commandsExecuted;
            protected ITestOutputHelper output;

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

            public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value, ConcurrentQueue<HystrixCommand<List<string>>> executionLog)
                : this(output, timer, value, 10000, 10, executionLog)
            {
            }

            public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, int value, int defaultMaxRequestsInBatch, int defaultTimerDelayInMilliseconds)
                : this(output, timer, value.ToString(), defaultMaxRequestsInBatch, defaultTimerDelayInMilliseconds)
            {
            }

            public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value, int defaultMaxRequestsInBatch, int defaultTimerDelayInMilliseconds)
                : this(output, timer, value, defaultMaxRequestsInBatch, defaultTimerDelayInMilliseconds, null)
            {
            }

            public TestRequestCollapser(ITestOutputHelper output, RequestCollapserScope scope, TestCollapserTimer timer, string value, int defaultMaxRequestsInBatch, int defaultTimerDelayInMilliseconds)
                : this(output, scope, timer, value, defaultMaxRequestsInBatch, defaultTimerDelayInMilliseconds, null)
            {
            }

            public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value, int defaultMaxRequestsInBatch, int defaultTimerDelayInMilliseconds, ConcurrentQueue<HystrixCommand<List<string>>> executionLog)
                : this(output, RequestCollapserScope.REQUEST, timer, value, defaultMaxRequestsInBatch, defaultTimerDelayInMilliseconds, executionLog)
            {
            }

            private static HystrixCollapserMetrics CreateMetrics()
            {
                var key = HystrixCollapserKeyDefault.AsKey("COLLAPSER_ONE");
                return HystrixCollapserMetrics.GetInstance(key, new HystrixCollapserOptions(key));
            }

            public TestRequestCollapser(ITestOutputHelper output, RequestCollapserScope scope, TestCollapserTimer timer, string value, int defaultMaxRequestsInBatch, int defaultTimerDelayInMilliseconds, ConcurrentQueue<HystrixCommand<List<string>>> executionLog)
                : base(CollapserKeyFromString(timer), scope, timer, GetOptions(CollapserKeyFromString(timer), defaultMaxRequestsInBatch, defaultTimerDelayInMilliseconds), CreateMetrics())
            {
                this.value = value;
                this.commandsExecuted = executionLog;
                this.output = output;
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

            public override string RequestArgument => value;

            protected override HystrixCommand<List<string>> CreateCommand(ICollection<ICollapsedRequest<string, string>> requests)
            {
                /* return a mocked command */
                HystrixCommand<List<string>> command = new TestCollapserCommand(output, requests);
                if (commandsExecuted != null)
                {
                    commandsExecuted.Enqueue(command);
                }

                return command;
            }

            protected override void MapResponseToRequests(List<string> batchResponse, ICollection<ICollapsedRequest<string, string>> requests)
            {
                // for simplicity I'll assume it's a 1:1 mapping between lists ... in real implementations they often need to index to maps
                // to allow random access as the response size does not match the request size
                if (batchResponse.Count != requests.Count)
                {
                    throw new Exception("lists don't match in size => " + batchResponse.Count + " : " + requests.Count);
                }

                var i = 0;
                foreach (var request in requests)
                {
                    request.Response = batchResponse[i++];
                }
            }
        }

        private class TestShardedRequestCollapser : TestRequestCollapser
        {
            public TestShardedRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value)
                : base(output, timer, value)
            {
            }

            protected override ICollection<ICollection<ICollapsedRequest<string, string>>> ShardRequests(ICollection<ICollapsedRequest<string, string>> requests)
            {
                ICollection<ICollapsedRequest<string, string>> typeA = new List<ICollapsedRequest<string, string>>();
                ICollection<ICollapsedRequest<string, string>> typeB = new List<ICollapsedRequest<string, string>>();

                foreach (var request in requests)
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

                var shards = new List<ICollection<ICollapsedRequest<string, string>>>();
                shards.Add(typeA);
                shards.Add(typeB);
                return shards;
            }
        }

        private class TestGloballyScopedRequestCollapser : TestRequestCollapser
        {
            public TestGloballyScopedRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value)
                : base(output, RequestCollapserScope.GLOBAL, timer, value)
            {
            }
        }

        private class TestRequestCollapserWithFaultyCreateCommand : TestRequestCollapser
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

        private class TestRequestCollapserWithShortCircuitedCommand : TestRequestCollapser
        {
            public TestRequestCollapserWithShortCircuitedCommand(ITestOutputHelper output, TestCollapserTimer timer, string value)
                : base(output, timer, value)
            {
            }

            protected override HystrixCommand<List<string>> CreateCommand(ICollection<ICollapsedRequest<string, string>> requests)
            {
                // args don't matter as it's short-circuited
                return new ShortCircuitedCommand(output);
            }
        }

        private class TestRequestCollapserWithFaultyMapToResponse : TestRequestCollapser
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

        private class TestCollapserCommand : TestHystrixCommand<List<string>>
        {
            private readonly ICollection<ICollapsedRequest<string, string>> requests;
            private ITestOutputHelper output;

            public TestCollapserCommand(ITestOutputHelper output, ICollection<ICollapsedRequest<string, string>> requests)
                : base(TestPropsBuilder().SetCommandOptionDefaults(GetCommandOptions()))
            {
                this.requests = requests;
                this.output = output;
            }

            protected override List<string> Run()
            {
                output.WriteLine(">>> TestCollapserCommand run() ... batch size: " + requests.Count);

                // simulate a batch request
                var response = new List<string>();
                foreach (var request in requests)
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
                                output.WriteLine(e.ToString());
                            }
                        }

                        response.Add(request.Argument);
                    }
                }

                return response;
            }

            private static IHystrixCommandOptions GetCommandOptions()
            {
                var opts = HystrixCommandOptionsTest.GetUnitTestOptions();
                opts.ExecutionTimeoutInMilliseconds = 500;
                return opts;
            }
        }

        private class SuccessfulCacheableCollapsedCommand : TestRequestCollapser
        {
            private readonly bool cacheEnabled;

            public SuccessfulCacheableCollapsedCommand(ITestOutputHelper output, TestCollapserTimer timer, string value, bool cacheEnabled)
                : base(output, timer, value)
            {
                this.cacheEnabled = cacheEnabled;
            }

            public SuccessfulCacheableCollapsedCommand(ITestOutputHelper output, TestCollapserTimer timer, string value, bool cacheEnabled, ConcurrentQueue<HystrixCommand<List<string>>> executionLog)
                : base(output, timer, value, executionLog)
            {
                this.cacheEnabled = cacheEnabled;
            }

            protected override string CacheKey
            {
                get
                {
                    if (cacheEnabled)
                    {
                        return "aCacheKey_" + value;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
        }

        private class ShortCircuitedCommand : HystrixCommand<List<string>>
        {
            private ITestOutputHelper output;

            public ShortCircuitedCommand(ITestOutputHelper output)
                : base(GetCommandOptions())
            {
                this.output = output;
            }

            protected override List<string> Run()
            {
                output.WriteLine("*** execution (this shouldn't happen)");

                // this won't ever get called as we're forcing short-circuiting
                var values = new List<string>();
                values.Add("hello");
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

        private class FireAndForgetCommand : HystrixCommand<object>
        {
            private ITestOutputHelper output;

            public FireAndForgetCommand(ITestOutputHelper output, List<int> values)
                : base(GetCommandOptions())
            {
                this.output = output;
            }

            protected override object Run()
            {
                output.WriteLine("*** FireAndForgetCommand execution: " + Thread.CurrentThread.ManagedThreadId);
                return null;
            }

            private static IHystrixCommandOptions GetCommandOptions()
            {
                IHystrixCommandOptions opts = HystrixCommandOptionsTest.GetUnitTestOptions();
                opts.GroupKey = HystrixCommandGroupKeyDefault.AsKey("fireAndForgetCommand");

                return opts;
            }
        }

        private class TestCollapserTimer : ICollapserTimer
        {
            public readonly ConcurrentDictionary<ATask, ATask> Tasks = new ConcurrentDictionary<ATask, ATask>();
            private object _lock = new object();
            private ITestOutputHelper output;

            public TestCollapserTimer(ITestOutputHelper output)
            {
                this.output = output;
            }

            public TimerReference AddListener(ITimerListener collapseTask)
            {
                var listener = new TestTimerListener(collapseTask);
                var t = new ATask(output, listener);
                Tasks.TryAdd(t, t);

                var refr = new TestTimerReference(this, listener, TimeSpan.FromMilliseconds(0));
                return refr;
            }

            public void IncrementTime(int timeInMilliseconds)
            {
                lock (_lock)
                {
                    foreach (var t in Tasks.Values)
                    {
                        t.IncrementTime(timeInMilliseconds);
                    }
                }
            }
        }

        private class TestTimerReference : TimerReference
        {
            private TestCollapserTimer ctimer;

            public TestTimerReference(TestCollapserTimer ctimer, ITimerListener listener, TimeSpan period)
                : base(listener, period)
            {
                this.ctimer = ctimer;
            }

            protected override void Dispose(bool disposing)
            {
                // Called when context is disposed
                foreach (var v in ctimer.Tasks.Values)
                {
                    if (v.Task == this._listener)
                    {
                        _ = ctimer.Tasks.TryRemove(v, out var removed);
                    }
                }

                base.Dispose(disposing);
            }
        }

        private class ATask
        {
            public readonly TestTimerListener Task;

            // our relative time that we'll use
            public volatile int Time = 0;
            public volatile int ExecutionCount = 0;
            private readonly int delay = 10;
            private object _lock = new object();
            private ITestOutputHelper output;

            public ATask(ITestOutputHelper output, TestTimerListener task)
            {
                Task = task;
                this.output = output;
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
                            output.WriteLine("ExecutionCount 0 => Time: " + Time + " Delay: " + delay);
                            if (Time >= delay)
                            {
                                // first execution, we're past the delay time
                                ExecuteTask();
                            }
                        }
                        else
                        {
                            output.WriteLine("ExecutionCount 1+ => Time: " + Time + " Delay: " + delay);
                            if (Time >= delay)
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
                    output.WriteLine("Executing task ...");
                    Task.Tick();
                    this.Time = 0; // we reset time after each execution
                    this.ExecutionCount++;
                    output.WriteLine("executionCount: " + ExecutionCount);
                }
            }
        }

        private class TestTimerListener : ITimerListener
        {
            public readonly ITimerListener ActualListener;
            public readonly AtomicInteger Count = new AtomicInteger();

            public TestTimerListener(ITimerListener actual)
            {
                this.ActualListener = actual;
            }

            public void Tick()
            {
                Count.IncrementAndGet();
                ActualListener.Tick();
            }

            public int IntervalTimeInMilliseconds => 10;
        }

        private static IHystrixCollapserKey CollapserKeyFromString(object o)
        {
            return new HystrixCollapserKeyDefault(o.ToString() + o.GetHashCode());
        }

        private class TestCollapserWithVoidResponseType : HystrixCollapser<object, object, int>
        {
            private readonly int value;
            private ITestOutputHelper output;

            public TestCollapserWithVoidResponseType(ITestOutputHelper output, ICollapserTimer timer, int value)
                : base(CollapserKeyFromString(timer), RequestCollapserScope.REQUEST, timer, GetCollapserOptions(CollapserKeyFromString(timer)))
            {
                this.value = value;
                this.output = output;
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

            public override int RequestArgument => value;

            protected override HystrixCommand<object> CreateCommand(ICollection<ICollapsedRequest<object, int>> requests)
            {
                var args = new List<int>();
                foreach (var request in requests)
                {
                    args.Add(request.Argument);
                }

                return new FireAndForgetCommand(output, args);
            }

            protected override void MapResponseToRequests(object batchResponse, ICollection<ICollapsedRequest<object, int>> requests)
            {
                foreach (var r in requests)
                {
                    r.Response = null;
                }
            }
        }

        private class TestCollapserWithVoidResponseTypeAndMissingMapResponseToRequests : HystrixCollapser<object, object, int>
        {
            private readonly int value;
            private ITestOutputHelper output;

            public TestCollapserWithVoidResponseTypeAndMissingMapResponseToRequests(ITestOutputHelper output, ICollapserTimer timer, int value)
                : base(CollapserKeyFromString(timer), RequestCollapserScope.REQUEST, timer, GetCollapserOptions(CollapserKeyFromString(timer)))
            {
                this.value = value;
                this.output = output;
            }

            public override int RequestArgument => value;

            protected override HystrixCommand<object> CreateCommand(ICollection<ICollapsedRequest<object, int>> requests)
            {
                var args = new List<int>();
                foreach (var request in requests)
                {
                    args.Add(request.Argument);
                }

                return new FireAndForgetCommand(output, args);
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

        private class Pair<A, B>
        {
            public readonly A AA;
            public readonly B BB;

            public Pair(A a, B b)
            {
                AA = a;
                BB = b;
            }
        }

        private class MyCommand : HystrixCommand<List<Pair<string, int>>>
        {
            private readonly List<string> args;
            private ITestOutputHelper output;

            public MyCommand(ITestOutputHelper output, List<string> args)
                : base(GetCommandOptions())
            {
                this.args = args;
                this.output = output;
            }

            protected override List<Pair<string, int>> Run()
            {
                output.WriteLine("Executing batch command on : " + Thread.CurrentThread.ManagedThreadId + " with args : " + args);
                var results = new List<Pair<string, int>>();
                foreach (var arg in args)
                {
                    results.Add(new Pair<string, int>(arg, int.Parse(arg)));
                }

                return results;
            }

            private static IHystrixCommandOptions GetCommandOptions()
            {
                var opts = new HystrixCommandOptions()
                {
                    GroupKey = HystrixCommandGroupKeyDefault.AsKey("BATCH")
                };
                return opts;
            }
        }

        private class MyCollapser : HystrixCollapser<List<Pair<string, int>>, int, string>
        {
            private readonly string arg;
            private ITestOutputHelper output;

            public MyCollapser(ITestOutputHelper output, string arg, bool reqCacheEnabled)
                : base(
                    HystrixCollapserKeyDefault.AsKey("UNITTEST"),
                    RequestCollapserScope.REQUEST,
                    new RealCollapserTimer(),
                    GetCollapserOptions(reqCacheEnabled),
                    HystrixCollapserMetrics.GetInstance(HystrixCollapserKeyDefault.AsKey("UNITTEST"), GetCollapserOptions(reqCacheEnabled)))
            {
                this.arg = arg;
                this.output = output;
            }

            public MyCollapser(ITestOutputHelper output, string arg, bool reqCacheEnabled, int timerDelayInMilliseconds)
                : base(
                    HystrixCollapserKeyDefault.AsKey("UNITTEST"),
                    RequestCollapserScope.REQUEST,
                    new RealCollapserTimer(),
                    GetCollapserOptions(reqCacheEnabled, timerDelayInMilliseconds),
                    HystrixCollapserMetrics.GetInstance(HystrixCollapserKeyDefault.AsKey("UNITTEST"), GetCollapserOptions(reqCacheEnabled, timerDelayInMilliseconds)))
            {
                this.arg = arg;
                this.output = output;
            }

            public override string RequestArgument => arg;

            protected override HystrixCommand<List<Pair<string, int>>> CreateCommand(ICollection<ICollapsedRequest<int, string>> collapsedRequests)
            {
                var args = new List<string>(collapsedRequests.Count);
                foreach (var req in collapsedRequests)
                {
                    args.Add(req.Argument);
                }

                return new MyCommand(output, args);
            }

            protected override void MapResponseToRequests(List<Pair<string, int>> batchResponse, ICollection<ICollapsedRequest<int, string>> collapsedRequests)
            {
                foreach (var pair in batchResponse)
                {
                    foreach (var collapsedReq in collapsedRequests)
                    {
                        if (collapsedReq.Argument.Equals(pair.AA))
                        {
                            collapsedReq.Response = pair.BB;
                        }
                    }
                }
            }

            protected override string CacheKey => arg;

            private static IHystrixCollapserOptions GetCollapserOptions(bool reqCacheEnabled)
            {
                var opts = new HystrixCollapserOptions(HystrixCollapserKeyDefault.AsKey("UNITTEST"))
                {
                    RequestCacheEnabled = reqCacheEnabled,
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

        private class TestSubscriber<T> : ObserverBase<T>, IDisposable
        {
            private CountdownEvent latch = new CountdownEvent(1);
            private ITestOutputHelper output;
            private int completions = 0;

            public TestSubscriber(ITestOutputHelper output)
            {
                this.output = output;
                this.OnNextEvents = new List<T>();
                this.OnErrorEvents = new List<Exception>();
            }

            public void Unsubscribe()
            {
                if (Subscription != null)
                {
                    Subscription.Dispose();
                }

                IsUnsubscribed = true;
            }

            public List<T> OnNextEvents { get; private set; }

            public List<Notification<T>> OnCompletedEvents
            {
                get
                {
                    var c = completions;
                    var results = new List<Notification<T>>();
                    for (var i = 0; i < c; i++)
                    {
                        results.Add(Notification.CreateOnCompleted<T>());
                    }

                    return results;
                }
            }

            public List<Exception> OnErrorEvents { get; private set; }

            public bool IsUnsubscribed { get; set; } = false;

            public IDisposable Subscription { get; set; }

            public void AwaitTerminalEvent(int timeInMilli)
            {
                try
                {
                    latch.Wait(timeInMilli);
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
                    Assert.False(true, "Unexpecteed onError events");
                }
            }

            public void AssertValues(params T[] check)
            {
                foreach (var v in check)
                {
                    if (!OnNextEvents.Contains(v))
                    {
                        Assert.False(true, "Value not found: " + v);
                    }
                }
            }

            internal void AssertError(Type et)
            {
                if (OnErrorEvents.Count != 1)
                {
                    Assert.False(true, "No errors or multiple errors");
                }

                var e = OnErrorEvents[0];
                var eTypeInfo = e.GetType().GetTypeInfo();
                var etTypeInfo = et.GetTypeInfo();
                if (eTypeInfo.Equals(etTypeInfo) || eTypeInfo.IsSubclassOf(et))
                {
                    return;
                }

                Assert.False(true, "Exceptions differ, Expected: " + et.ToString() + " Found: " + e.GetType());
            }

            internal void AssertNoValues()
            {
                var c = OnNextEvents.Count;
                if (c != 0)
                {
                    Assert.False(true, "No onNext events expected yet some received: " + c);
                }
            }

            internal void AssertCompleted()
            {
                var s = completions;
                if (s == 0)
                {
                    Assert.False(true, "Not completed!");
                }
                else if (s > 1)
                {
                    Assert.False(true, "Completed multiple times: " + s);
                }
            }

            protected override void OnCompletedCore()
            {
                output.WriteLine("OnCompleted @ " + Time.CurrentTimeMillis);
                completions++;
                latch.SignalEx();
            }

            protected override void OnErrorCore(Exception error)
            {
                output.WriteLine("OnError @ " + Time.CurrentTimeMillis + " : " + error.Message.ToString());
                OnErrorEvents.Add(error);
                latch.SignalEx();
            }

            protected override void OnNextCore(T value)
            {
                output.WriteLine("OnNext @ " + Time.CurrentTimeMillis + " : " + value.ToString());
                OnNextEvents.Add(value);
            }
        }
    }
}