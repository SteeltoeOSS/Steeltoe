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
    public class HystrixSyncTaskSchedulerTest
    {
        private ITestOutputHelper output;

        public HystrixSyncTaskSchedulerTest(ITestOutputHelper output)
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
            };

            // Scheduler to test
            var scheduler = new HystrixSyncTaskScheduler(options);

            TaskActionClass tc1 = new TaskActionClass(output, 1);
            TaskActionClass tc2 = new TaskActionClass(output, 2);
            Task<int> t1 = new Task<int>((o) => tc1.Run(o), dummyCommand, CancellationToken.None, TaskCreationOptions.LongRunning);
            Task<int> t2 = new Task<int>((o) => tc2.Run(o), dummyCommand, CancellationToken.None, TaskCreationOptions.LongRunning);

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
            public bool Stop = false;
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