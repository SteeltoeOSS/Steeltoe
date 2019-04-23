// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency.Test
{
    public class HystrixQueuedTaskSchedulerTest
    {
        private ITestOutputHelper output;

        public HystrixQueuedTaskSchedulerTest(ITestOutputHelper output)
            : base()
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
                MaxQueueSize = 2,
                QueueSizeRejectionThreshold = 2,
                AllowMaximumSizeToDivergeFromCoreSize = false,
            };

            // Scheduler to test
            var scheduler = new HystrixQueuedTaskScheduler(options);

            TaskActionClass tc1 = new TaskActionClass(output, 1);
            TaskActionClass tc2 = new TaskActionClass(output, 2);
            TaskActionClass tc3 = new TaskActionClass(output, 3);
            TaskActionClass tc4 = new TaskActionClass(output, 4);
            Task<int> t1 = new Task<int>((o) => tc1.Run(o), dummyCommand, CancellationToken.None, TaskCreationOptions.LongRunning);
            Task<int> t2 = new Task<int>((o) => tc2.Run(o), dummyCommand, CancellationToken.None, TaskCreationOptions.LongRunning);
            Task<int> t3 = new Task<int>((o) => tc3.Run(o), dummyCommand, CancellationToken.None, TaskCreationOptions.LongRunning);
            Task<int> t4 = new Task<int>((o) => tc4.Run(o), dummyCommand, CancellationToken.None, TaskCreationOptions.LongRunning);

            // Fill up to CoreSize
            t1.Start(scheduler);
            t2.Start(scheduler);

            // Make sure they are running
            Thread.Sleep(500);

            // Fill up queue
            t3.Start(scheduler);
            t4.Start(scheduler);

            // Allow all tasks to finish and cause continuation tasks to be queued
            tc1.Stop = true;
            tc2.Stop = true;
            tc3.Stop = true;
            tc4.Stop = true;

            Thread.Sleep(1000);

            Assert.True(t1.IsCompleted);
            Assert.Equal(1, t1.Result);

            Assert.True(t2.IsCompleted);
            Assert.Equal(2, t2.Result);

            Assert.True(t3.IsCompleted);
            Assert.Equal(3, t3.Result);

            Assert.True(t4.IsCompleted);
            Assert.Equal(4, t4.Result);
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
            public bool Stop = false;
            public ITestOutputHelper Output;

            public TaskActionClass(ITestOutputHelper output, int val)
            {
                Output = output;
                Value = val;
            }

            public int Run(object cmd)
            {
                var result = RunAsync().Result;
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
