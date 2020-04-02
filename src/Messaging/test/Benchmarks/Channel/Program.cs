using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System.Threading.Tasks;

[MemoryDiagnoser]
public class Program
{
    static void Main()
    {
        BenchmarkRunner.Run<Program>();
    }

    [Benchmark]
    public void TaskSchedulerSubscribableChannel_Send_10_000_000()
    {
        var channel = new TaskSchedulerSubscribableChannel();
        var handler = new CounterHandler();

        channel.Subscribe(handler);
        var message = new GenericMessage("test");
        for (var i = 0; i < 10_000_000; i++)
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
        var message = new GenericMessage("test");
        for (var i = 0; i < 10_000_000; i++)
        {
            await channel.Writer.WriteAsync(message);
        }
    }

    private class CounterHandler : IMessageHandler
    {
        public int Count;

        public void HandleMessage(IMessage message)
        {
            Count++;
        }
    }
}

