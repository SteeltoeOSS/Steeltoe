// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Steeltoe.CircuitBreaker.Hystrix.Strategy.Concurrency.Test;

public class HystrixQueuedTaskSchedulerTest
{
    private readonly ITestOutputHelper output;

    public HystrixQueuedTaskSchedulerTest(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void TestContinuationTasks_DoNotCauseDeadlocks()
    {
        var dummyCommand = new DummyCommand(new HystrixCommandOptions { GroupKey = HystrixCommandGroupKeyDefault.AsKey("foobar") });
        var options = new HystrixThreadPoolOptions
        {
            CoreSize = 2,
            MaxQueueSize = 2,
            QueueSizeRejectionThreshold = 2,
            AllowMaximumSizeToDivergeFromCoreSize = false,
        };

        // Scheduler to test
        var scheduler = new HystrixQueuedTaskScheduler(options);

        var tc1 = new TaskActionClass(output, 1);
        var tc2 = new TaskActionClass(output, 2);
        var tc3 = new TaskActionClass(output, 3);
        var tc4 = new TaskActionClass(output, 4);
        var t1 = new Task<int>(o => tc1.Run(o), dummyCommand, CancellationToken.None, TaskCreationOptions.LongRunning);
        var t2 = new Task<int>(o => tc2.Run(o), dummyCommand, CancellationToken.None, TaskCreationOptions.LongRunning);
        var t3 = new Task<int>(o => tc3.Run(o), dummyCommand, CancellationToken.None, TaskCreationOptions.LongRunning);
        var t4 = new Task<int>(o => tc4.Run(o), dummyCommand, CancellationToken.None, TaskCreationOptions.LongRunning);

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

    private sealed class DummyCommand : HystrixCommand<int>
    {
        public DummyCommand(IHystrixCommandOptions commandOptions)
            : base(commandOptions)
        {
        }
    }

    private sealed class TaskActionClass
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
