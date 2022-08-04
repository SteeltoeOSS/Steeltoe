// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;

namespace ChannelBenchmark;

[MemoryDiagnoser]
public sealed class Program
{
    private static void Main()
    {
        BenchmarkRunner.Run<Program>();
    }

    [Benchmark]
    public void TaskSchedulerSubscribableChannel_Send_10_000_000()
    {
        var channel = new TaskSchedulerSubscribableChannel();
        var handler = new CounterHandler();

        channel.Subscribe(handler);
        IMessage<string> message = Message.Create("test");

        for (int i = 0; i < 10_000_000; i++)
        {
            channel.Send(message);
        }
    }

    [Benchmark]
    public async ValueTask TaskSchedulerSubscribableChannel_WriteAsync_10_000_000()
    {
        var channel = new TaskSchedulerSubscribableChannel();
        var handler = new CounterHandler();

        channel.Subscribe(handler);
        IMessage<string> message = Message.Create("test");

        for (int i = 0; i < 10_000_000; i++)
        {
            await channel.Writer.WriteAsync(message);
        }
    }

    private sealed class CounterHandler : IMessageHandler
    {
        public int Count;

        public string ServiceName { get; set; } = nameof(CounterHandler);

        public void HandleMessage(IMessage message)
        {
            Count++;
        }
    }
}
