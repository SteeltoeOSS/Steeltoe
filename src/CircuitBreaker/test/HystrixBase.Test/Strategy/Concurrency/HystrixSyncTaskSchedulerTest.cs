// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency.Test
{
    public class HystrixSyncTaskSchedulerTest
    {
        private readonly ITestOutputHelper output;

        public HystrixSyncTaskSchedulerTest(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void TestContinuationTasks_DoNotCauseDeadlocks()
        {
            var dummyCommand = new DummyCommand(new HystrixCommandOptions() { GroupKey = HystrixCommandGroupKeyDefault.AsKey("foobar") });
            var options = new HystrixThreadPoolOptions()
            {
                CoreSize = 2,
            };

            // Scheduler to test
            var scheduler = new HystrixSyncTaskScheduler(options);

            var tc1 = new TaskActionClass(output, 1);
            var tc2 = new TaskActionClass(output, 2);
            var t1 = new Task<int>((o) => tc1.Run(o), dummyCommand, CancellationToken.None, TaskCreationOptions.LongRunning);
            var t2 = new Task<int>((o) => tc2.Run(o), dummyCommand, CancellationToken.None, TaskCreationOptions.LongRunning);

            // Fill up to CoreSize
            t1.Start(scheduler);
            t2.Start(scheduler);

            // Make sure they are running
            Thread.Sleep(500);

            // Allow t1 task to finish and cause continuation task to be queued
            tc1.Stop = true;

            Thread.Sleep(1000);

            Assert.True(t1.IsCompleted);
            Assert.Equal(1, t1.Result);

            tc2.Stop = true;
            Thread.Sleep(1000);

            Assert.True(t2.IsCompleted);
            Assert.Equal(2, t2.Result);
        }

        private class DummyCommand : HystrixCommand<int>
        {
            public DummyCommand(IHystrixCommandOptions commandOptions)
                : base(commandOptions, null, null, null)
            {
            }
        }

        private class TaskActionClass
        {
            public int Value;
            public bool Stop;
            public ITestOutputHelper Output;

            public TaskActionClass(ITestOutputHelper output, int val)
            {
                Output = output;
                Value = val;
            }

            public int Run(object cmd)
            {
                var result = RunAsync().GetAwaiter().GetResult();
                return result;
            }

            public async Task<int> RunAsync()
            {
                var result = await DoWorkAsync();
                return result;
            }

            public Task<int> DoWorkAsync()
            {
                var t = Task.Run(() =>
                {
                    while (!Stop)
                    {
                        Thread.Sleep(10);
                    }

                    return Value;
                });
                return t;
            }
        }
    }
}