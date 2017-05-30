//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Steeltoe.CircuitBreaker.Hystrix.Collapser;
using Steeltoe.CircuitBreaker.Hystrix.Util;
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
    public class HystrixCollapserTest : HystrixTestBase, IDisposable
    {
        ITestOutputHelper output;
        public HystrixCollapserTest(ITestOutputHelper output) : base()
        {
            this.output = output;
        }

        public override void Dispose()
        {
            base.Dispose();
            HystrixCollapserMetrics.Reset();
        }

        [Fact]
        public void TestTwoRequests()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(output, timer, 1);
            Task<string> response1 = collapser1.ExecuteAsync();
            HystrixCollapser<List<string>, string, string> collapser2 = new TestRequestCollapser(output, timer, 2);
            Task<String> response2 = collapser2.ExecuteAsync();

            timer.IncrementTime(10); // let time pass that equals the default delay/period

            Assert.Equal("1", response1.Result);
            if (response2.Wait(1000))
                Assert.Equal("2", response2.Result);
            else
                Assert.False(true, "Timed out");

            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            HystrixCollapserMetrics metrics = collapser1.Metrics;
            Assert.True(metrics == collapser2.Metrics);

            IHystrixInvokableInfo command = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.First();
            Assert.Equal(2, command.NumberCollapsed);
        }
        [Fact]
        public void TestMultipleBatches()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<string>, string, string> collapser1 = new TestRequestCollapser(output, timer, 1);
            Task<string> response1 = collapser1.ExecuteAsync();
            Task<string> response2 = new TestRequestCollapser(output, timer, 2).ExecuteAsync();
            timer.IncrementTime(10); // let time pass that equals the default delay/period

            Assert.Equal("1", GetResult(response1, 1000));
            Assert.Equal("2", GetResult(response2, 1000));

            // now request more
            Task<string> response3 = new TestRequestCollapser(output, timer, 3).ExecuteAsync();
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
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new TestRequestCollapser(output, timer, 1, 2, 10);
            HystrixCollapser<List<String>, String, String> collapser2 = new TestRequestCollapser(output, timer, 2, 2, 10);
            HystrixCollapser<List<String>, String, String> collapser3 = new TestRequestCollapser(output, timer, 3, 2, 10);
            output.WriteLine("*** " + DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " Constructed the collapsers");
            Task<String> response1 = collapser1.ExecuteAsync();
            Task<String> response2 = collapser2.ExecuteAsync();
            Task<String> response3 = collapser3.ExecuteAsync();
            output.WriteLine("*** " + DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " queued the collapsers");

            timer.IncrementTime(10); // let time pass that equals the default delay/period
            output.WriteLine("*** " + DateTime.Now.Ticks / 10000 + " : " + Thread.CurrentThread.ManagedThreadId + " incremented the virtual timer");


            Assert.Equal("1", GetResult(response1, 1000));
            Assert.Equal("2", GetResult(response2, 1000));
            Assert.Equal("3", GetResult(response3, 1000));

            // we should have had it execute twice because the batch size was 2
            output.WriteLine("ReqLog : " + HystrixRequestLog.CurrentRequestLog.GetExecutedCommandsAsString());
            Assert.Equal(2, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            ICollection<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands;
            Assert.Equal(2, cmdIterator.First().NumberCollapsed);
            Assert.Equal(1, cmdIterator.Last().NumberCollapsed);
        }

        [Fact]
        public void TestRequestsOverTime()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new TestRequestCollapser(output, timer, 1);
            Task<String> response1 = collapser1.ExecuteAsync();
            timer.IncrementTime(5);
            Task<String> response2 = new TestRequestCollapser(output, timer, 2).ExecuteAsync();
            timer.IncrementTime(8);
            // should execute here
            Task<String> response3 = new TestRequestCollapser(output, timer, 3).ExecuteAsync();
            timer.IncrementTime(6);
            Task<String> response4 = new TestRequestCollapser(output, timer, 4).ExecuteAsync();
            timer.IncrementTime(8);
            // should execute here
            Task<String> response5 = new TestRequestCollapser(output, timer, 5).ExecuteAsync();
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
            int NUM = 10;

            List<IObservable<int>> observables = new List<IObservable<int>>();
            for (int i = 0; i < NUM; i++)
            {
                MyCollapser c = new MyCollapser(output, "5", true);
                var observable = c.ToObservable();
                observables.Add(observable);
            }

            List<TestSubscriber<int>> subscribers = new List<TestSubscriber<int>>();
            foreach (IObservable<int> o in observables)
            {
                TestSubscriber<int> sub = new TestSubscriber<int>(output);
                subscribers.Add(sub);
                o.Subscribe(sub);
            }

            Time.Wait( 100);

            //all subscribers should receive the same value
            foreach (TestSubscriber<int> sub in subscribers)
            {
                sub.AwaitTerminalEvent(1000);
                output.WriteLine("Subscriber received : " + sub.OnNextEvents.Count);
                sub.AssertNoErrors();
                sub.AssertValues(5);
            }
        }
        [Fact]
        public void TestDuplicateArgumentsWithRequestCachingOff()
        {
            int NUM = 10;

            List<IObservable<int>> observables = new List<IObservable<int>>();
            for (int i = 0; i < NUM; i++)
            {
                MyCollapser c = new MyCollapser(output, "5", false);
                observables.Add(c.ToObservable());
            }

            List<TestSubscriber<int>> subscribers = new List<TestSubscriber<int>>();
            foreach (IObservable<int> o in observables)
            {
                TestSubscriber<int> sub = new TestSubscriber<int>(output);
                subscribers.Add(sub);
                o.Subscribe(sub);
            }
            // Wait to make sure batch ran
            Time.Wait(100);

            AtomicInteger numErrors = new AtomicInteger(0);
            AtomicInteger numValues = new AtomicInteger(0);

            // only the first subscriber should receive the value.
            // the others should get an error that the batch contains duplicates
            foreach (TestSubscriber<int> sub in subscribers)
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
            Assert.Equal(NUM - 1, numErrors.Value);
        }
        //public static IObservable<TSource> OnSubscribe<TSource>(IObservable<TSource> source)
        //{
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

        //}
        [Fact]
        public void TestUnsubscribeFromSomeDuplicateArgsDoesNotRemoveFromBatch()
        {
            int NUM = 10;

            List<IObservable<int>> observables = new List<IObservable<int>>();
            List<MyCollapser> collapsers = new List<MyCollapser>();
            for (int i = 0; i < NUM; i++)
            {
                MyCollapser c = new MyCollapser(output, "5", true);
                collapsers.Add(c);
                var obs = c.ToObservable();
                observables.Add(obs);
            }

            List<TestSubscriber<int>> subscribers = new List<TestSubscriber<int>>();
            List<IDisposable> subscriptions = new List<IDisposable>();

            foreach (IObservable<int> o in observables)
            {
                TestSubscriber<int> sub = new TestSubscriber<int>(output);
                subscribers.Add(sub);
                IDisposable s = o.Subscribe(sub);
                sub.Subscription = s;
                subscriptions.Add(s);
            }


            //unsubscribe from all but 1
            for (int i = 0; i < NUM - 1; i++)
            {
                subscribers[i].Unsubscribe();
            }

            Time.Wait( 100);

            //all subscribers with an active subscription should receive the same value
            foreach (TestSubscriber<int> sub in subscribers)
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
        public void TestUnsubscribeOnOneDoesntKillBatch()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new TestRequestCollapser(output, timer, 1);
            CancellationTokenSource cts1 = new CancellationTokenSource();
            CancellationTokenSource cts2 = new CancellationTokenSource();
            Task<String> response1 = collapser1.ExecuteAsync(cts1.Token);
            Task<String> response2 = new TestRequestCollapser(output, timer, 2).ExecuteAsync(cts2.Token);

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
            catch (Exception )
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
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new TestShardedRequestCollapser(output, timer, "1a");
            Task<String> response1 = collapser1.ExecuteAsync();
            Task<String> response2 = new TestShardedRequestCollapser(output, timer, "2b").ExecuteAsync();
            Task<String> response3 = new TestShardedRequestCollapser(output, timer, "3b").ExecuteAsync();
            Task<String> response4 = new TestShardedRequestCollapser(output, timer, "4a").ExecuteAsync();
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
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new TestRequestCollapser(output, timer, "1");
            Task<String> response1 = collapser1.ExecuteAsync();
            Task<String> response2 = new TestRequestCollapser(output, timer, "2").ExecuteAsync();

            // simulate a new request
            RequestCollapserFactory.ResetRequest();

            Task<String> response3 = new TestRequestCollapser(output, timer, "3").ExecuteAsync();
            Task<String> response4 = new TestRequestCollapser(output, timer, "4").ExecuteAsync();

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
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new TestGloballyScopedRequestCollapser(output, timer, "1");
            Task<String> response1 = collapser1.ExecuteAsync();
            Task<String> response2 = new TestGloballyScopedRequestCollapser(output, timer, "2").ExecuteAsync();

            // simulate a new request
            RequestCollapserFactory.ResetRequest();

            Task<String> response3 = new TestGloballyScopedRequestCollapser(output, timer, "3").ExecuteAsync();
            Task<String> response4 = new TestGloballyScopedRequestCollapser(output, timer, "4").ExecuteAsync();

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
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new TestRequestCollapserWithFaultyCreateCommand(output, timer, "1");
            Task<String> response1 = collapser1.ExecuteAsync();
            Task<String> response2 = new TestRequestCollapserWithFaultyCreateCommand(output, timer, "2").ExecuteAsync();
            timer.IncrementTime(10); // let time pass that equals the default delay/period

            try
            {
                GetResult(response1, 1000);
                Assert.True(false, "we should have received an exception");
            }
            catch (Exception )
            {
                // what we expect
                //output.WriteLine(e.ToString());
            }
            try
            {
                GetResult(response2, 1000);
                Assert.True(false, "we should have received an exception");
            }
            catch (Exception )
            {
                // what we expect
                //output.WriteLine(e.ToString());
            }

            Assert.Equal(0, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);
        }
        [Fact]
        public void TestErrorHandlingWhenMapToResponseFails()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new TestRequestCollapserWithFaultyMapToResponse(output, timer, "1");
            Task<String> response1 = collapser1.ExecuteAsync();
            Task<String> response2 = new TestRequestCollapserWithFaultyMapToResponse(output, timer, "2").ExecuteAsync();
            timer.IncrementTime(10); // let time pass that equals the default delay/period
            try
            {
                GetResult(response1, 1000);
                Assert.True(false, "we should have received an exception");
            }
            catch (Exception )
            {
                // what we expect
                //output.WriteLine(e.ToString());
            }
            try
            {
                GetResult(response2, 1000);
                Assert.True(false, "we should have received an exception");
            }
            catch (Exception )
            {
                // what we expect
                //output.WriteLine(e.ToString());
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
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new TestRequestCollapser(output, timer, 1);
            Task<String> response1 = collapser1.ExecuteAsync();
            timer.IncrementTime(5);
            Task<String> response2 = new TestRequestCollapser(output, timer, 2).ExecuteAsync();
            timer.IncrementTime(8);
            // should execute here
            Task<String> response3 = new TestRequestCollapser(output, timer, 3).ExecuteAsync();
            timer.IncrementTime(6);
            Task<String> response4 = new TestRequestCollapser(output, timer, 4).ExecuteAsync();
            timer.IncrementTime(8);
            // should execute here
            Task<String> response5 = new TestRequestCollapser(output, timer, 5).ExecuteAsync();
            timer.IncrementTime(10);
            // should execute here

            // wait for all tasks to complete
            Assert.Equal("1", GetResult(response1, 1000));
            Assert.Equal("2", GetResult(response2, 1000));
            Assert.Equal("3", GetResult(response3, 1000));
            Assert.Equal("4", GetResult(response4, 1000));
            Assert.Equal("5", GetResult(response5, 1000));



            // each task should have been executed 3 times
            foreach (ATask t in timer.tasks.Values)
            {
                Assert.Equal(3, t.task.count.Value);
            }

            output.WriteLine("timer.tasks.size() A: " + timer.tasks.Count);
            output.WriteLine("tasks in test: " + timer.tasks);


            List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(2, cmdIterator[0].NumberCollapsed);
            Assert.Equal(2, cmdIterator[1].NumberCollapsed);
            Assert.Equal(1, cmdIterator[2].NumberCollapsed);

            output.WriteLine("timer.tasks.size() B: " + timer.tasks.Count);
            var rv = RequestCollapserFactory.GetRequestVariable<List<string>, string, string>(new TestRequestCollapser(output, timer, 1).CollapserKey.Name);

            context.Dispose();

            Assert.NotNull(rv);
            // they should have all been removed as part of ThreadContext.remove()
            Assert.Equal(0, timer.tasks.Count);
        }
        [Fact]
        public void TestRequestVariableLifecycle2()
        {

            TestCollapserTimer timer = new TestCollapserTimer(output);
            ConcurrentDictionary<Task<String>, Task<String>> responses = new ConcurrentDictionary<Task<String>, Task<String>>();
            List<Task> threads = new List<Task>();

            // kick off work (simulating a single request with multiple threads)
            for (int t = 0; t < 5; t++)
            {
                int outerLoop = t;
                Task th = new Task(() =>
                {
                    for (int i = 0; i < 100; i++)
                    {
                        int uniqueInt = (outerLoop * 100) + i;
                        Task<string> tsk = new TestRequestCollapser(output, timer, uniqueInt).ExecuteAsync();
                        responses.TryAdd(tsk, tsk);
                    }

                }, CancellationToken.None, TaskCreationOptions.LongRunning);
                th.Start();
                threads.Add(th);

            }
            Task.WaitAll(threads.ToArray());



            // we expect 5 threads * 100 responses each
            Assert.Equal(500, responses.Count);

            foreach (Task<String> f in responses.Values)
            {
                // they should not be done yet because the counter hasn't incremented
                Assert.False(f.IsCompleted);
            }

            timer.IncrementTime(5);
            HystrixCollapser<List<String>, String, String> collapser1 = new TestRequestCollapser(output, timer, 2);
            Task<String> response2 = collapser1.ExecuteAsync();
            timer.IncrementTime(8);
            // should execute here
            Task<String> response3 = new TestRequestCollapser(output, timer, 3).ExecuteAsync();
            timer.IncrementTime(6);
            Task<String> response4 = new TestRequestCollapser(output, timer, 4).ExecuteAsync();
            timer.IncrementTime(8);
            // should execute here
            Task<String> response5 = new TestRequestCollapser(output, timer, 5).ExecuteAsync();
            timer.IncrementTime(10);
            // should execute here

            // wait for all tasks to complete
            foreach (Task<String> f in responses.Values)
            {
                GetResult(f, 1000);

            }
            Assert.Equal("2", GetResult(response2, 1000));
            Assert.Equal("3", GetResult(response3, 1000));
            Assert.Equal("4", GetResult(response4, 1000));
            Assert.Equal("5", GetResult(response5, 1000));

            // each task should have been executed 3 times
            foreach (ATask t in timer.tasks.Values)
            {
                Assert.Equal(3, t.task.count.Value);
            }

            List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(500, cmdIterator[0].NumberCollapsed);
            Assert.Equal(2, cmdIterator[1].NumberCollapsed);
            Assert.Equal(1, cmdIterator[2].NumberCollapsed);
            var rv = RequestCollapserFactory.GetRequestVariable<List<string>, string, string>(new TestRequestCollapser(output, timer, 1).CollapserKey.Name);

            context.Dispose();

            Assert.NotNull(rv);
            // they should have all been removed as part of ThreadContext.remove()
            Assert.Equal(0, timer.tasks.Count);

        }
        [Fact]
        public void TestRequestCache1()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            SuccessfulCacheableCollapsedCommand command1 = new SuccessfulCacheableCollapsedCommand(output, timer, "A", true);
            SuccessfulCacheableCollapsedCommand command2 = new SuccessfulCacheableCollapsedCommand(output, timer, "A", true);

            Task<String> f1 = command1.ExecuteAsync();
            Task<String> f2 = command2.ExecuteAsync();

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

            Task<String> f3 = command1.ExecuteAsync();

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

            IHystrixInvokableInfo command = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList().First();
            output.WriteLine("command.getExecutionEvents(): " + command.ExecutionEvents.Count);
            Assert.Equal(2, command.ExecutionEvents.Count);
            Assert.True(command.ExecutionEvents.Contains(HystrixEventType.SUCCESS));
            Assert.True(command.ExecutionEvents.Contains(HystrixEventType.COLLAPSED));


            Assert.Equal(1, command.NumberCollapsed);
        }

        /**
         * Test Request scoped caching doesn't prevent different ones from executing
         */
        [Fact]
        public void TestRequestCache2()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            SuccessfulCacheableCollapsedCommand command1 = new SuccessfulCacheableCollapsedCommand(output, timer, "A", true);
            SuccessfulCacheableCollapsedCommand command2 = new SuccessfulCacheableCollapsedCommand(output, timer, "B", true);

            Task<String> f1 = command1.ExecuteAsync();
            Task<String> f2 = command2.ExecuteAsync();

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

            Task<String> f3 = command1.ExecuteAsync();
            Task<String> f4 = command2.ExecuteAsync();

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

            IHystrixInvokableInfo command = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList().First();
            output.WriteLine("command.getExecutionEvents(): " + command.ExecutionEvents.Count);
            Assert.Equal(2, command.ExecutionEvents.Count);
            Assert.True(command.ExecutionEvents.Contains(HystrixEventType.SUCCESS));
            Assert.True(command.ExecutionEvents.Contains(HystrixEventType.COLLAPSED));


        }

        /**
 * Test Request scoped caching with a mixture of commands
 */
        [Fact]
        public void TestRequestCache3()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            SuccessfulCacheableCollapsedCommand command1 = new SuccessfulCacheableCollapsedCommand(output, timer, "A", true);
            SuccessfulCacheableCollapsedCommand command2 = new SuccessfulCacheableCollapsedCommand(output, timer, "B", true);
            SuccessfulCacheableCollapsedCommand command3 = new SuccessfulCacheableCollapsedCommand(output, timer, "B", true);

            Task<String> f1 = command1.ExecuteAsync();
            Task<String> f2 = command2.ExecuteAsync();
            Task<String> f3 = command3.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            try
            {
                Assert.Equal("A", GetResult(f1, 1000));
                Assert.Equal("B", GetResult(f2, 1000));
                Assert.Equal("B", GetResult(f3, 1000));
            }
            catch (Exception )
            {
                throw;
            }

            Task<String> f4 = command1.ExecuteAsync();
            Task<String> f5 = command2.ExecuteAsync();
            Task<String> f6 = command3.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            try
            {
                Assert.Equal("A", GetResult(f4, 1000));
                Assert.Equal("B", GetResult(f5, 1000));
                Assert.Equal("B", GetResult(f6, 1000));
            }
            catch (Exception )
            {
                throw;
            }

            // we should still have executed only one command
            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            IHystrixInvokableInfo command = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList()[0];
            Assert.Equal(2, command.ExecutionEvents.Count);
            Assert.True(command.ExecutionEvents.Contains(HystrixEventType.SUCCESS));
            Assert.True(command.ExecutionEvents.Contains(HystrixEventType.COLLAPSED));


            Assert.Equal(2, command.NumberCollapsed);
        }
        /**
 * Test Request scoped caching with a mixture of commands
 */
        [Fact]
        public void TestNoRequestCache3()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            SuccessfulCacheableCollapsedCommand command1 = new SuccessfulCacheableCollapsedCommand(output, timer, "A", false);
            SuccessfulCacheableCollapsedCommand command2 = new SuccessfulCacheableCollapsedCommand(output, timer, "B", false);
            SuccessfulCacheableCollapsedCommand command3 = new SuccessfulCacheableCollapsedCommand(output, timer, "B", false);

            Task<String> f1 = command1.ExecuteAsync();
            Task<String> f2 = command2.ExecuteAsync();
            Task<String> f3 = command3.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            try
            {
                Assert.Equal("A", GetResult(f1, 1000));
                Assert.Equal("B", GetResult(f2, 1000));
                Assert.Equal("B", GetResult(f3, 1000));
            }
            catch (Exception )
            {
                throw;
            }

            Task<String> f4 = command1.ExecuteAsync();
            Task<String> f5 = command2.ExecuteAsync();
            Task<String> f6 = command3.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            try
            {
                Assert.Equal("A", GetResult(f4, 1000));
                Assert.Equal("B", GetResult(f5, 1000));
                Assert.Equal("B", GetResult(f6, 1000));
            }
            catch (Exception )
            {
                throw;
            }

            // request caching is turned off on this so we expect 2 command executions
            Assert.Equal(2, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            // we expect to see it with SUCCESS and COLLAPSED and both
            IHystrixInvokableInfo commandA = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList()[0];
            Assert.Equal(2, commandA.ExecutionEvents.Count);
            Assert.True(commandA.ExecutionEvents.Contains(HystrixEventType.SUCCESS));
            Assert.True(commandA.ExecutionEvents.Contains(HystrixEventType.COLLAPSED));

            // we expect to see it with SUCCESS and COLLAPSED and both
            IHystrixInvokableInfo commandB = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList()[1];
            Assert.Equal(2, commandB.ExecutionEvents.Count);
            Assert.True(commandB.ExecutionEvents.Contains(HystrixEventType.SUCCESS));
            Assert.True(commandB.ExecutionEvents.Contains(HystrixEventType.COLLAPSED));

            List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(2, cmdIterator[0].NumberCollapsed);  //1 for A, 1 for B.  Batch contains only unique arguments (no duplicates)
            Assert.Equal(2, cmdIterator[1].NumberCollapsed);  //1 for A, 1 for B.  Batch contains only unique arguments (no duplicates)
        }
        /**
 * Test command that uses a null request argument
 */
        [Fact]
        public void TestRequestCacheWithNullRequestArgument()
        {
            ConcurrentQueue<HystrixCommand<List<String>>> commands = new ConcurrentQueue<HystrixCommand<List<String>>>();

            TestCollapserTimer timer = new TestCollapserTimer(output);
            SuccessfulCacheableCollapsedCommand command1 = new SuccessfulCacheableCollapsedCommand(output, timer, null, true, commands);
            SuccessfulCacheableCollapsedCommand command2 = new SuccessfulCacheableCollapsedCommand(output, timer, null, true, commands);

            Task<String> f1 = command1.ExecuteAsync();
            Task<String> f2 = command2.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            Assert.Equal("NULL", GetResult(f1, 1000));
            Assert.Equal("NULL", GetResult(f2, 1000));

            // it should have executed 1 command
            Assert.Equal(1, commands.Count);
            HystrixCommand<List<String>> peek = null;
            commands.TryPeek(out peek);
            Assert.True(peek.ExecutionEvents.Contains(HystrixEventType.SUCCESS));
            Assert.True(peek.ExecutionEvents.Contains(HystrixEventType.COLLAPSED));

            Task<String> f3 = command1.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            Assert.Equal("NULL", GetResult(f3, 1000));

            // it should still be 1 ... no new executions
            Assert.Equal(1, commands.Count);
            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(1, cmdIterator[0].NumberCollapsed);
        }
        [Fact]
        public void TestRequestCacheWithCommandError()
        {
            ConcurrentQueue<HystrixCommand<List<String>>> commands = new ConcurrentQueue<HystrixCommand<List<String>>>();

            TestCollapserTimer timer = new TestCollapserTimer(output);
            SuccessfulCacheableCollapsedCommand command1 = new SuccessfulCacheableCollapsedCommand(output, timer, "FAILURE", true, commands);
            SuccessfulCacheableCollapsedCommand command2 = new SuccessfulCacheableCollapsedCommand(output, timer, "FAILURE", true, commands);

            Task<String> f1 = command1.ExecuteAsync();
            Task<String> f2 = command2.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            try
            {
                Assert.Equal("A", GetResult(f1, 1000));
                Assert.Equal("A", GetResult(f2, 1000));
                Assert.True(false, "exception should have been thrown");
            }
            catch (Exception )
            {
                // expected
            }

            // it should have executed 1 command
            Assert.Equal(1, commands.Count);
            HystrixCommand<List<String>> peek = null;
            commands.TryPeek(out peek);

            Assert.True(peek.ExecutionEvents.Contains(HystrixEventType.FAILURE));
            Assert.True(peek.ExecutionEvents.Contains(HystrixEventType.COLLAPSED));

            Task<String> f3 = command1.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            try
            {
                Assert.Equal("A", GetResult(f3, 1000));
                Assert.True(false, "exception should have been thrown");
            }
            catch (Exception )
            {
                // expected
            }

            // it should still be 1 ... no new executions
            Assert.Equal(1, commands.Count);
            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(1, cmdIterator[0].NumberCollapsed);

        }

        /**
         * Test that a command that times out will still be cached and when retrieved will re-throw the exception.
         */
        [Fact]
        public void TestRequestCacheWithCommandTimeout()
        {
            ConcurrentQueue<HystrixCommand<List<String>>> commands = new ConcurrentQueue<HystrixCommand<List<String>>>();

            TestCollapserTimer timer = new TestCollapserTimer(output);
            SuccessfulCacheableCollapsedCommand command1 = new SuccessfulCacheableCollapsedCommand(output, timer, "TIMEOUT", true, commands);
            SuccessfulCacheableCollapsedCommand command2 = new SuccessfulCacheableCollapsedCommand(output, timer, "TIMEOUT", true, commands);

            Task<String> f1 = command1.ExecuteAsync();
            Task<String> f2 = command2.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            try
            {
                Assert.Equal("A", GetResult(f1, 1000));
                Assert.Equal("A", GetResult(f2, 1000));
                Assert.True(false, "exception should have been thrown");
            }
            catch (Exception )
            {
                // expected
            }

            // it should have executed 1 command
            Assert.Equal(1, commands.Count);
            HystrixCommand<List<String>> peek = null;
            commands.TryPeek(out peek);
            Assert.True(peek.ExecutionEvents.Contains(HystrixEventType.TIMEOUT));
            Assert.True(peek.ExecutionEvents.Contains(HystrixEventType.COLLAPSED));


            Task<String> f3 = command1.ExecuteAsync();

            // increment past batch time so it executes
            timer.IncrementTime(15);

            try
            {
                Assert.Equal("A", GetResult(f3, 1000));
                Assert.True(false, "exception should have been thrown");
            }
            catch (Exception )
            {
                // expected
            }

            // it should still be 1 ... no new executions
            Assert.Equal(1, commands.Count);
            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(1, cmdIterator[0].NumberCollapsed);

        }
        /**
 * Test how the collapser behaves when the circuit is short-circuited
 */
        [Fact]
        public async void TestRequestWithCommandShortCircuited()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new TestRequestCollapserWithShortCircuitedCommand(output, timer, "1");
            IObservable<String> response1 = collapser1.Observe();
            IObservable<String> response2 = new TestRequestCollapserWithShortCircuitedCommand(output, timer, "2").Observe();
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

            List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(2, cmdIterator[0].NumberCollapsed);


        }
        /**
 * Test a Void response type - null being set as response.
 *
 * @throws Exception
 */
        [Fact]
        public void TestVoidResponseTypeFireAndForgetCollapsing1()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            TestCollapserWithVoidResponseType collapser1 = new TestCollapserWithVoidResponseType(output, timer, 1);
            Task<object> response1 = collapser1.ExecuteAsync();
            Task<object> response2 = new TestCollapserWithVoidResponseType(output, timer, 2).ExecuteAsync();
            timer.IncrementTime(100); // let time pass that equals the default delay/period

            // normally someone wouldn't wait on these, but we need to make sure they do in fact return
            // and not block indefinitely in case someone does call get()
            Assert.Null( GetResult(response1, 1000));
            Assert.Null( GetResult(response2, 1000));

            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(2, cmdIterator[0].NumberCollapsed);

        }

        /**
         * Test a Void response type - response never being set in mapResponseToRequest
         *
         * @throws Exception
         */
        [Fact]
        public void TestVoidResponseTypeFireAndForgetCollapsing2()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            TestCollapserWithVoidResponseTypeAndMissingMapResponseToRequests collapser1 = new TestCollapserWithVoidResponseTypeAndMissingMapResponseToRequests(output, timer, 1);
            Task<object> response1 = collapser1.ExecuteAsync();
            new TestCollapserWithVoidResponseTypeAndMissingMapResponseToRequests(output, timer, 2).ExecuteAsync();
            timer.IncrementTime(100); // let time pass that equals the default delay/period

            // we will fetch one of these just so we wait for completion ... but expect an error
            try
            {
                Assert.Null( GetResult(response1, 1000));
                Assert.False(true, "expected an error as mapResponseToRequests did not set responses");
            }
            catch (Exception e)
            {
                Assert.True(e.InnerException is InvalidOperationException);
                Assert.True(e.InnerException.Message.StartsWith("No response set by"));
            }

            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(2, cmdIterator[0].NumberCollapsed);
        }
        /**
 * Test a Void response type with execute - response being set in mapResponseToRequest to null
 *
 * @throws Exception
 */
        [Fact]
        public void TestVoidResponseTypeFireAndForgetCollapsing3()
        {
            ICollapserTimer timer = new RealCollapserTimer();
            TestCollapserWithVoidResponseType collapser1 = new TestCollapserWithVoidResponseType(output, timer, 1);
            Assert.Null(collapser1.Execute());

            Assert.Equal(1, HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.Count);

            List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(1, cmdIterator[0].NumberCollapsed);
        }

        [Fact]
        public void TestEarlyUnsubscribeExecutedViaToObservable()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new TestRequestCollapser(output, timer, 1);
            IObservable<String> response1 = collapser1.ToObservable();
            HystrixCollapser<List<String>, String, String> collapser2 = new TestRequestCollapser(output, timer, 2);
            IObservable<String> response2 = collapser2.ToObservable();

            CountdownEvent latch1 = new CountdownEvent(1);
            CountdownEvent latch2 = new CountdownEvent(1);

            AtomicReference<String> value1 = new AtomicReference<String>(null);
            AtomicReference<String> value2 = new AtomicReference<String>(null);

            IDisposable s1 = response1
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

            IDisposable s2 = response2
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
            HystrixCollapserMetrics metrics = collapser1.Metrics;
            Assert.True(metrics == collapser2.Metrics);

            List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(1, cmdIterator[0].NumberCollapsed);

        }
        [Fact]
        public void TestEarlyUnsubscribeExecutedViaObserve()
        {

            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new TestRequestCollapser(output, timer, 1);
            IObservable<String> response1 = collapser1.Observe();
            HystrixCollapser<List<String>, String, String> collapser2 = new TestRequestCollapser(output, timer, 2);
            IObservable<String> response2 = collapser2.Observe();

            CountdownEvent latch1 = new CountdownEvent(1);
            CountdownEvent latch2 = new CountdownEvent(1);

            AtomicReference<String> value1 = new AtomicReference<String>(null);
            AtomicReference<String> value2 = new AtomicReference<String>(null);

            IDisposable s1 = response1
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

            IDisposable s2 = response2
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
            HystrixCollapserMetrics metrics = collapser1.Metrics;
            Assert.True(metrics == collapser2.Metrics);

            List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            Assert.Equal(1, cmdIterator[0].NumberCollapsed);
        }
        [Fact]
        public void TestEarlyUnsubscribeFromAllCancelsBatch()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new TestRequestCollapser(output, timer, 1);
            IObservable<String> response1 = collapser1.Observe();
            HystrixCollapser<List<String>, String, String> collapser2 = new TestRequestCollapser(output, timer, 2);
            IObservable<String> response2 = collapser2.Observe();

            CountdownEvent latch1 = new CountdownEvent(1);
            CountdownEvent latch2 = new CountdownEvent(1);

            AtomicReference<String> value1 = new AtomicReference<String>(null);
            AtomicReference<String> value2 = new AtomicReference<String>(null);

            IDisposable s1 = response1
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

            IDisposable s2 = response2
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
        public void TestRequestThenCacheHitAndCacheHitUnsubscribed()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            IObservable<String> response1 = collapser1.Observe();
            HystrixCollapser<List<String>, String, String> collapser2 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            IObservable<String> response2 = collapser2.Observe();

            CountdownEvent latch1 = new CountdownEvent(1);
            CountdownEvent latch2 = new CountdownEvent(1);

            AtomicReference<String> value1 = new AtomicReference<String>(null);
            AtomicReference<String> value2 = new AtomicReference<String>(null);

            IDisposable s1 = response1
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

            IDisposable s2 = response2
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


            List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            AssertCommandExecutionEvents(cmdIterator[0], HystrixEventType.SUCCESS, HystrixEventType.COLLAPSED);
            Assert.Equal(1, cmdIterator[0].NumberCollapsed); //should only be 1 collapsed - other came from cache, then was cancelled

        }
        [Fact]
        public void TestRequestThenCacheHitAndOriginalUnsubscribed()
        {
            // TODO:
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            IObservable<String> response1 = collapser1.Observe();
            HystrixCollapser<List<String>, String, String> collapser2 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            IObservable<String> response2 = collapser2.Observe();

            CountdownEvent latch1 = new CountdownEvent(1);
            CountdownEvent latch2 = new CountdownEvent(1);

            AtomicReference<String> value1 = new AtomicReference<String>(null);
            AtomicReference<String> value2 = new AtomicReference<String>(null);
            IDisposable s1 = response1
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

            IDisposable s2 = response2
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


            List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            AssertCommandExecutionEvents(cmdIterator[0], HystrixEventType.SUCCESS, HystrixEventType.COLLAPSED);
            Assert.Equal(1, cmdIterator[0].NumberCollapsed); //should only be 1 collapsed - other came from cache, then was cancelled

        }
        [Fact]
    public void TestRequestThenTwoCacheHitsOriginalAndOneCacheHitUnsubscribed()  
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            IObservable<String> response1 = collapser1.Observe();
            HystrixCollapser<List<String>, String, String> collapser2 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            IObservable<String> response2 = collapser2.Observe();
            HystrixCollapser<List<String>, String, String> collapser3 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            IObservable<String> response3 = collapser3.Observe();

            CountdownEvent latch1 = new CountdownEvent(1);
            CountdownEvent latch2 = new CountdownEvent(1);
            CountdownEvent latch3 = new CountdownEvent(1);

            AtomicReference<String> value1 = new AtomicReference<String>(null);
            AtomicReference<String> value2 = new AtomicReference<String>(null);
            AtomicReference<String> value3 = new AtomicReference<String>(null);
            IDisposable s1 = response1
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

            IDisposable s2 = response2
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

            IDisposable s3 = response3
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


            List<IHystrixInvokableInfo> cmdIterator = HystrixRequestLog.CurrentRequestLog.AllExecutedCommands.ToList();
            AssertCommandExecutionEvents(cmdIterator[0], HystrixEventType.SUCCESS, HystrixEventType.COLLAPSED);
            Assert.Equal(1, cmdIterator[0].NumberCollapsed); //should only be 1 collapsed - other came from cache, then was cancelled

    }
        [Fact]

        public void TestRequestThenTwoCacheHitsAllUnsubscribed()
        {
            TestCollapserTimer timer = new TestCollapserTimer(output);
            HystrixCollapser<List<String>, String, String> collapser1 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            IObservable<String> response1 = collapser1.Observe();
            HystrixCollapser<List<String>, String, String> collapser2 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            IObservable<String> response2 = collapser2.Observe();
            HystrixCollapser<List<String>, String, String> collapser3 = new SuccessfulCacheableCollapsedCommand(output, timer, "foo", true);
            IObservable<String> response3 = collapser3.Observe();

            CountdownEvent latch1 = new CountdownEvent(1);
            CountdownEvent latch2 = new CountdownEvent(1);
            CountdownEvent latch3 = new CountdownEvent(1);

            AtomicReference<String> value1 = new AtomicReference<String>(null);
            AtomicReference<String> value2 = new AtomicReference<String>(null);
            AtomicReference<String> value3 = new AtomicReference<String>(null);

            IDisposable s1 = response1
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

            IDisposable s2 = response2
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

            IDisposable s3 = response3
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

        private static T GetResult<T>(Task<T> task, int timeout)
        {
            if (task.Wait(timeout))
                return task.Result;
            else
                return default(T);
        }
        protected void AssertCommandExecutionEvents(IHystrixInvokableInfo command, params HystrixEventType[] expectedEventTypes)
        {
            bool emitExpected = false;
            int expectedEmitCount = 0;

            bool fallbackEmitExpected = false;
            int expectedFallbackEmitCount = 0;

            List<HystrixEventType> condensedEmitExpectedEventTypes = new List<HystrixEventType>();

            foreach (HystrixEventType expectedEventType in expectedEventTypes)
            {
                if (expectedEventType.Equals(HystrixEventType.EMIT))
                {
                    if (!emitExpected)
                    {
                        //first EMIT encountered, add it to condensedEmitExpectedEventTypes
                        condensedEmitExpectedEventTypes.Add(HystrixEventType.EMIT);
                    }
                    emitExpected = true;
                    expectedEmitCount++;
                }
                else if (expectedEventType.Equals(HystrixEventType.FALLBACK_EMIT))
                {
                    if (!fallbackEmitExpected)
                    {
                        //first FALLBACK_EMIT encountered, add it to condensedEmitExpectedEventTypes
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
            List<HystrixEventType> actualEventTypes = command.ExecutionEvents;
            Assert.Equal(expectedEmitCount, command.NumberEmissions);
            Assert.Equal(expectedFallbackEmitCount, command.NumberFallbackEmissions);
            Assert.Equal(condensedEmitExpectedEventTypes, actualEventTypes);
        }

        class TestRequestCollapser : HystrixCollapser<List<string>, string, string>
        {

            protected readonly string value;
            protected ConcurrentQueue<HystrixCommand<List<string>>> commandsExecuted;
            protected ITestOutputHelper output;
            public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, int value) :
                this(output, timer, value.ToString())
            {
            }

            public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value) :
                this(output, timer, value, 10000, 10)
            {
            }

            public TestRequestCollapser(ITestOutputHelper output, RequestCollapserScope scope, TestCollapserTimer timer, string value) :
                this(output, scope, timer, value, 10000, 10)
            {
            }

            public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value, ConcurrentQueue<HystrixCommand<List<string>>> executionLog) :

                this(output, timer, value, 10000, 10, executionLog)
            {
            }

            public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, int value, int defaultMaxRequestsInBatch, int defaultTimerDelayInMilliseconds) :

                this(output, timer, value.ToString(), defaultMaxRequestsInBatch, defaultTimerDelayInMilliseconds)
            {
            }

            public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value, int defaultMaxRequestsInBatch, int defaultTimerDelayInMilliseconds) :

                this(output, timer, value, defaultMaxRequestsInBatch, defaultTimerDelayInMilliseconds, null)
            {
            }

            public TestRequestCollapser(ITestOutputHelper output, RequestCollapserScope scope, TestCollapserTimer timer, string value, int defaultMaxRequestsInBatch, int defaultTimerDelayInMilliseconds) :

                this(output, scope, timer, value, defaultMaxRequestsInBatch, defaultTimerDelayInMilliseconds, null)
            {
            }

            public TestRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value, int defaultMaxRequestsInBatch, int defaultTimerDelayInMilliseconds, ConcurrentQueue<HystrixCommand<List<string>>> executionLog) :

                this(output, RequestCollapserScope.REQUEST, timer, value, defaultMaxRequestsInBatch, defaultTimerDelayInMilliseconds, executionLog)
            {
            }

            private static HystrixCollapserMetrics CreateMetrics()
            {
                IHystrixCollapserKey key = HystrixCollapserKeyDefault.AsKey("COLLAPSER_ONE");
                return HystrixCollapserMetrics.GetInstance(key, new HystrixCollapserOptions(key));
            }

            public TestRequestCollapser(ITestOutputHelper output, RequestCollapserScope scope, TestCollapserTimer timer, string value, int defaultMaxRequestsInBatch, int defaultTimerDelayInMilliseconds, ConcurrentQueue<HystrixCommand<List<string>>> executionLog) :

                // use a CollapserKey based on the CollapserTimer object reference so it's unique for each timer as we don't want caching
                // of properties to occur and we're using the default HystrixProperty which typically does caching
                base(CollapserKeyFromString(timer), scope, timer, GetOptions(CollapserKeyFromString(timer), defaultMaxRequestsInBatch, defaultTimerDelayInMilliseconds), CreateMetrics())
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
            public override string RequestArgument
            {
                get { return value; }
            }

            protected override HystrixCommand<List<String>> CreateCommand(ICollection<ICollapsedRequest<string, string>> requests)
            {
                /* return a mocked command */
                HystrixCommand<List<string>> command = new TestCollapserCommand(output, requests);
                if (commandsExecuted != null)
                {
                    commandsExecuted.Enqueue(command);
                }
                return command;
            }

            protected override void MapResponseToRequests(List<String> batchResponse, ICollection<ICollapsedRequest<string, string>> requests)
            {
                // for simplicity I'll assume it's a 1:1 mapping between lists ... in real implementations they often need to index to maps
                // to allow random access as the response size does not match the request size
                if (batchResponse.Count != requests.Count)
                {
                    throw new Exception("lists don't match in size => " + batchResponse.Count + " : " + requests.Count);
                }
                int i = 0;
                foreach (ICollapsedRequest<string, string> request in requests)
                {
                    request.Response = batchResponse[i++];
                }

            }

        }

        /**
         * Shard on the artificially provided 'type' variable.
         */
        class TestShardedRequestCollapser : TestRequestCollapser
        {

            public TestShardedRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value) :
                base(output, timer, value)
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

                List<ICollection<ICollapsedRequest<string, string>>> shards = new List<ICollection<ICollapsedRequest<string, string>>>();
                shards.Add(typeA);
                shards.Add(typeB);
                return shards;
            }

        }

        /**
         * Test the global scope
         */
        class TestGloballyScopedRequestCollapser : TestRequestCollapser
        {

            public TestGloballyScopedRequestCollapser(ITestOutputHelper output, TestCollapserTimer timer, string value) :
                base(output, RequestCollapserScope.GLOBAL, timer, value)
            {
            }

        }

        /**
         * Throw an exception when creating a command.
         */
        class TestRequestCollapserWithFaultyCreateCommand : TestRequestCollapser
        {

            public TestRequestCollapserWithFaultyCreateCommand(ITestOutputHelper output, TestCollapserTimer timer, string value) :
                base(output, timer, value)
            {
            }

            protected override HystrixCommand<List<string>> CreateCommand(ICollection<ICollapsedRequest<string, string>> requests)
            {
                throw new Exception("some failure");
            }

        }

        /**
         * Throw an exception when creating a command.
         */
        class TestRequestCollapserWithShortCircuitedCommand : TestRequestCollapser
        {

            public TestRequestCollapserWithShortCircuitedCommand(ITestOutputHelper output, TestCollapserTimer timer, string value) :
                base(output, timer, value)
            {
            }

            protected override HystrixCommand<List<string>> CreateCommand(ICollection<ICollapsedRequest<string, string>> requests)
            {
                // args don't matter as it's short-circuited
                return new ShortCircuitedCommand(output);
            }

        }

        /**
         * Throw an exception when mapToResponse is invoked
         */
        class TestRequestCollapserWithFaultyMapToResponse : TestRequestCollapser
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

        class TestCollapserCommand : TestHystrixCommand<List<string>>
        {

            private readonly ICollection<ICollapsedRequest<string, string>> requests;
            ITestOutputHelper output;

            public TestCollapserCommand(ITestOutputHelper output, ICollection<ICollapsedRequest<string, string>> requests)
                : base(TestPropsBuilder().SetCommandOptionDefaults(GetCommandOptions()))
            {
                this.requests = requests;
                this.output = output;
            }

            private static IHystrixCommandOptions GetCommandOptions()
            {
                var opts = HystrixCommandOptionsTest.GetUnitTestOptions();
                opts.ExecutionTimeoutInMilliseconds = 500;
                return opts;
            }
            protected override List<string> Run()
            {
                output.WriteLine(">>> TestCollapserCommand run() ... batch size: " + requests.Count);
                // simulate a batch request
                List<String> response = new List<String>();
                foreach (ICollapsedRequest<String, String> request in requests)
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
                                Time.Wait( 800);

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

        }

        /**
         * A Command implementation that supports caching.
         */
        class SuccessfulCacheableCollapsedCommand : TestRequestCollapser
        {

            private readonly bool cacheEnabled;

            public SuccessfulCacheableCollapsedCommand(ITestOutputHelper output, TestCollapserTimer timer, string value, bool cacheEnabled) :
                base(output, timer, value)
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
                        return "aCacheKey_" + base.value;
                    else
                        return null;
                }
            }
        }

        class ShortCircuitedCommand : HystrixCommand<List<string>>
        {
            ITestOutputHelper output;
            public ShortCircuitedCommand(ITestOutputHelper output)
                : base(GetCommandOptions())
            {
                this.output = output;
            }

            private static IHystrixCommandOptions GetCommandOptions()
            {
                IHystrixCommandOptions opts = HystrixCommandOptionsTest.GetUnitTestOptions();

                opts.CircuitBreakerForceOpen = true;
                opts.GroupKey = HystrixCommandGroupKeyDefault.AsKey("shortCircuitedCommand");

                return opts;
            }

            protected override List<string> Run()
            {
                output.WriteLine("*** execution (this shouldn't happen)");
                // this won't ever get called as we're forcing short-circuiting
                List<string> values = new List<string>();
                values.Add("hello");
                return values;
            }

        }

        class FireAndForgetCommand : HystrixCommand<object>
        {
            ITestOutputHelper output;


            public FireAndForgetCommand(ITestOutputHelper output, List<int> values)
                : base(GetCommandOptions())
            {
                this.output = output;
            }

            private static IHystrixCommandOptions GetCommandOptions()
            {
                IHystrixCommandOptions opts = HystrixCommandOptionsTest.GetUnitTestOptions();
                opts.GroupKey = HystrixCommandGroupKeyDefault.AsKey("fireAndForgetCommand");

                return opts;
            }
            protected override object Run()
            {
                output.WriteLine("*** FireAndForgetCommand execution: " + Thread.CurrentThread.ManagedThreadId);
                return null;
            }

        }

        class TestCollapserTimer : ICollapserTimer
        {
            private object _lock = new object();
            public readonly ConcurrentDictionary<ATask, ATask> tasks = new ConcurrentDictionary<ATask, ATask>();
            ITestOutputHelper output;
            public TestCollapserTimer(ITestOutputHelper output)
            {
                this.output = output;
            }

            public TimerReference AddListener(ITimerListener collapseTask)
            {
                TestTimerListener listener = new TestTimerListener(collapseTask);
                var t = new ATask(output, listener);
                tasks.TryAdd(t, t);

                TestTimerReference refr = new TestTimerReference(this, listener, TimeSpan.FromMilliseconds(0));
                return refr;

                /**
                 * This is a hack that overrides 'clear' of a WeakReference to match the required API
                 * but then removes the strong-reference we have inside 'tasks'.
                 * <p>
                 * We do this so our unit tests know if the WeakReference is cleared correctly, and if so then the ATack is removed from 'tasks'
                 */
                //    return new SoftReference<TimerListener>(collapseTask) {
                //                @Override
                //                public void clear()
                //    {
                //        // super.clear();
                //        for (ATask t : tasks) {
                //        if (t.task.actualListener.equals(collapseTask))
                //        {
                //            tasks.remove(t);
                //        }
                //    }
                //}

                //            };
            }

            /**
             * Increment time by X. Note that incrementing by multiples of delay or period time will NOT execute multiple times.
             * <p>
             * You must call incrementTime multiple times each increment being larger than 'period' on subsequent calls to cause multiple executions.
             * <p>
             * This is because executing multiple times in a tight-loop would not achieve the correct behavior, such as batching, since it will all execute "now" not after intervals of time.
             *
             * @param timeInMilliseconds amount of time to increment
             */
            public void IncrementTime(int timeInMilliseconds)
            {
                lock (_lock)
                {
                    foreach (ATask t in tasks.Values)
                    {
                        t.IncrementTime(timeInMilliseconds);
                    }
                }
            }
        }
        class TestTimerReference : TimerReference
        {
            TestCollapserTimer ctimer;
            public TestTimerReference(TestCollapserTimer ctimer, ITimerListener listener, TimeSpan period) :
                base(listener, period)
            {
                this.ctimer = ctimer;
            }

            public override void Dispose()
            {
                // Called when context is disposed
                foreach (var v in ctimer.tasks.Values)
                {
                    if (v.task == this._listener)
                    {
                        ATask removed = v;
                        ctimer.tasks.TryRemove(v, out removed);
                    }
                }
                base.Dispose();
            }

        }

        class ATask
        {
            public readonly TestTimerListener task;
            readonly int delay = 10;

            // our relative time that we'll use
            public volatile int time = 0;
            public volatile int executionCount = 0;

            private object _lock = new object();
            ITestOutputHelper output;
            public ATask(ITestOutputHelper output, TestTimerListener task)
            {
                this.task = task;
                this.output = output;
            }

            public void IncrementTime(int timeInMilliseconds)
            {
                lock (_lock)
                {
                    time += timeInMilliseconds;
                    if (task != null)
                    {
                        if (executionCount == 0)
                        {
                            output.WriteLine("ExecutionCount 0 => Time: " + time + " Delay: " + delay);
                            if (time >= delay)
                            {
                                // first execution, we're past the delay time
                                ExecuteTask();
                            }
                        }
                        else
                        {
                            output.WriteLine("ExecutionCount 1+ => Time: " + time + " Delay: " + delay);
                            if (time >= delay)
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
                    task.Tick();
                    this.time = 0; // we reset time after each execution
                    this.executionCount++;
                    output.WriteLine("executionCount: " + executionCount);
                }
            }
        }

        class TestTimerListener : ITimerListener
        {

            public readonly ITimerListener actualListener;
            public readonly AtomicInteger count = new AtomicInteger();

            public TestTimerListener(ITimerListener actual)
            {
                this.actualListener = actual;
            }


            public void Tick()
            {
                count.IncrementAndGet();
                actualListener.Tick();
            }


            public int IntervalTimeInMilliseconds
            {
                get { return 10; }
            }

        }

        private static IHystrixCollapserKey CollapserKeyFromString(object o)
        {
            return new HystrixCollapserKeyDefault(o.ToString() + o.GetHashCode());

        }

        class TestCollapserWithVoidResponseType : HystrixCollapser<object, object, int>
        {

            private readonly int value;
            ITestOutputHelper output;

            public TestCollapserWithVoidResponseType(ITestOutputHelper output, ICollapserTimer timer, int value) :
                base(CollapserKeyFromString(timer), RequestCollapserScope.REQUEST, timer, GetCollapserOptions(CollapserKeyFromString(timer)))
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
            public override int RequestArgument
            {
                get { return value; }
            }


            protected override HystrixCommand<object> CreateCommand(ICollection<ICollapsedRequest<object, int>> requests)
            {

                List<int> args = new List<int>();
                foreach (CollapsedRequest<object, int> request in requests)
                {
                    args.Add(request.Argument);
                }
                return new FireAndForgetCommand(output, args);
            }


            protected override void MapResponseToRequests(object batchResponse, ICollection<ICollapsedRequest<object, int>> requests)
            {
                foreach (ICollapsedRequest<object, int> r in requests)
                {
                    r.Response = null;
                }
            }

        }

        class TestCollapserWithVoidResponseTypeAndMissingMapResponseToRequests : HystrixCollapser<object, object, int>
        {

            private readonly int value;
            ITestOutputHelper output;

            public TestCollapserWithVoidResponseTypeAndMissingMapResponseToRequests(ITestOutputHelper output, ICollapserTimer timer, int value)
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
            public override int RequestArgument
            {
                get { return value; }
            }


            protected override HystrixCommand<object> CreateCommand(ICollection<ICollapsedRequest<object, int>> requests)
            {

                List<int> args = new List<int>();
                foreach (CollapsedRequest<object, int> request in requests)
                {
                    args.Add(request.Argument);
                }
                return new FireAndForgetCommand(output, args);
            }


            protected override void MapResponseToRequests(object batchResponse, ICollection<ICollapsedRequest<object, int>> requests)
            {
            }
        }
        class Pair<A, B>
        {
            public readonly A a;
            public readonly B b;

            public Pair(A a, B b)
            {
                this.a = a;
                this.b = b;
            }
        }

        class MyCommand : HystrixCommand<List<Pair<String, int>>>
        {

            private readonly List<String> args;
            ITestOutputHelper output;
            public MyCommand(ITestOutputHelper output, List<String> args)
            :
                base(GetCommandOptions())
            {
                this.args = args;
                this.output = output;
            }

            private static IHystrixCommandOptions GetCommandOptions()
            {
                var opts = new HystrixCommandOptions()
                {
                    GroupKey = HystrixCommandGroupKeyDefault.AsKey("BATCH")
                };
                return opts;
            }

            protected override List<Pair<String, int>> Run()
            {
                output.WriteLine("Executing batch command on : " + Thread.CurrentThread.ManagedThreadId + " with args : " + args);
                List<Pair<String, int>> results = new List<Pair<String, int>>();
                foreach (String arg in args)
                {
                    results.Add(new Pair<String, int>(arg, int.Parse(arg)));
                }
                return results;
            }
        }

        class MyCollapser : HystrixCollapser<List<Pair<String, int>>, int, String>
        {

            private readonly String arg;
            ITestOutputHelper output;
            public MyCollapser(ITestOutputHelper output, String arg, bool reqCacheEnabled)
                : base(HystrixCollapserKeyDefault.AsKey("UNITTEST"),
                    RequestCollapserScope.REQUEST,
                    new RealCollapserTimer(),
                    GetCollapserOptions(reqCacheEnabled),
                    HystrixCollapserMetrics.GetInstance(HystrixCollapserKeyDefault.AsKey("UNITTEST"), GetCollapserOptions(reqCacheEnabled)))
            {
                this.arg = arg;
                this.output = output;
            }
            static IHystrixCollapserOptions GetCollapserOptions(bool reqCacheEnabled)
            {
                var opts = new HystrixCollapserOptions(HystrixCollapserKeyDefault.AsKey("UNITTEST"))
                {
                    RequestCacheEnabled = reqCacheEnabled,
                };
                return opts;
            }
            public override String RequestArgument
            {
                get { return arg; }
            }

            protected override HystrixCommand<List<Pair<String, int>>> CreateCommand(ICollection<ICollapsedRequest<int, String>> collapsedRequests)
            {
                List<String> args = new List<String>(collapsedRequests.Count);
                foreach (CollapsedRequest<int, String> req in collapsedRequests)
                {
                    args.Add(req.Argument);
                }
                return new MyCommand(output, args);
            }

            protected override void MapResponseToRequests(List<Pair<String, int>> batchResponse, ICollection<ICollapsedRequest<int, String>> collapsedRequests)
            {
                foreach (Pair<String, int> pair in batchResponse)
                {
                    foreach (ICollapsedRequest<int, String> collapsedReq in collapsedRequests)
                    {
                        if (collapsedReq.Argument.Equals(pair.a))
                        {
                            collapsedReq.Response = pair.b;
                        }
                    }
                }
            }

            protected override String CacheKey
            {
                get { return arg; }
            }
        }
        class TestSubscriber<T> : ObserverBase<T>, IDisposable
        {
            CountdownEvent latch = new CountdownEvent(1);
            ITestOutputHelper output;
            bool isDisposed = false;

            List<T> values;
            List<Exception> errors;
            int completions = 0;
            public TestSubscriber(ITestOutputHelper output)
            {
                this.output = output;
                this.values = new List<T>();
                this.errors = new List<Exception>();
            }

            protected override void OnCompletedCore()
            {
                output.WriteLine("OnCompleted @ " + DateTime.Now.Ticks / 10000);
                completions++;
                latch.SignalEx();
            }

            protected override void OnErrorCore(Exception error)
            {
                output.WriteLine("OnError @ " + DateTime.Now.Ticks / 10000 + " : " + error.Message.ToString());
                errors.Add(error);
                latch.SignalEx();
            }

            protected override void OnNextCore(T value)
            {
                output.WriteLine("OnNext @ " + DateTime.Now.Ticks / 10000 + " : " + value.ToString());
                values.Add(value);
            }
            public void Unsubscribe()
            {
                if (Subscription != null)
                {
                    Subscription.Dispose();
                }

                this.isDisposed = true;
            }
            public List<T> OnNextEvents
            {
                get { return values; }
            }

            public List<Notification<T>> OnCompletedEvents
            {
                get
                {
                    int c = completions;
                    List<Notification<T>> results = new List<Notification<T>>();
                    for (int i = 0; i < c; i++)
                    {
                        results.Add(Notification.CreateOnCompleted<T>());
                    }
                    return results;
                }
            }
            public List<Exception> OnErrorEvents { get { return errors; } }

            public bool IsUnsubscribed { get { return isDisposed; } set { isDisposed = value; } }

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
                if (errors.Count > 0)
                {
                    Assert.False(true, "Unexpecteed onError events");
                }
            }
            public void AssertValues(params T[] check)
            {
                foreach (var v in check)
                {
                    if (!values.Contains(v))
                        Assert.False(true, "Value not found: " + v);
                }

            }

            internal void AssertError(Type et)
            {
                if (errors.Count != 1)
                {
                    Assert.False(true, "No errors or multiple errors");
                }
                Exception e = errors[0];
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
                int c = values.Count;
                if (c != 0)
                {
                    Assert.False(true, "No onNext events expected yet some received: " + c);
                }
            }

            internal void AssertCompleted()
            {
                int s = completions;
                if (s == 0)
                {
                    Assert.False(true, "Not completed!");
                }
                else
                if (s > 1)
                {
                    Assert.False(true, "Completed multiple times: " + s);
                }
            }
        }
    }
}


