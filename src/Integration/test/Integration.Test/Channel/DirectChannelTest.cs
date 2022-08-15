// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using Xunit;

namespace Steeltoe.Integration.Channel.Test;

public class DirectChannelTest
{
    private readonly IServiceProvider _provider;

    public DirectChannelTest()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void TestSend()
    {
        var target = new ThreadNameExtractingTestTarget();
        var channel = new DirectChannel(_provider.GetService<IApplicationContext>());
        channel.Subscribe(target);
        IMessage<string> message = Message.Create("test");
        int? currentId = Task.CurrentId;
        int curThreadId = Thread.CurrentThread.ManagedThreadId;
        Assert.True(channel.Send(message));
        Assert.Equal(currentId, target.TaskId);
        Assert.Equal(curThreadId, target.ThreadId);
    }

    [Fact]
    public async Task TestSendAsync()
    {
        var target = new ThreadNameExtractingTestTarget();
        var channel = new DirectChannel(_provider.GetService<IApplicationContext>());
        channel.Subscribe(target);
        IMessage<string> message = Message.Create("test");
        Assert.True(await channel.SendAsync(message));
    }

    [Fact]
    public void TestSendOneHandler_10_000_000()
    {
        /*
         *  INT-3308 - used to run 12 million/sec
         *  1. optimize for single handler 20 million/sec
         *  2. Don't iterate over empty data types 23 million/sec
         *  3. Don't iterate over empty interceptors 31 million/sec
         *  4. Move single handler optimization to dispatcher 34 million/sec
         *
         *  29 million per second with increment counter in the handler
         */
        var channel = new DirectChannel(_provider.GetService<IApplicationContext>());

        var handler = new CounterHandler();
        channel.Subscribe(handler);
        IMessage<string> message = Message.Create("test");
        Assert.True(channel.Send(message));

        for (int i = 0; i < 10_000_000; i++)
        {
            channel.Send(message);
        }

        Assert.Equal(10_000_001, handler.Count);
    }

    [Fact]
    public async Task TestSendAsyncOneHandler_10_000_000()
    {
        var channel = new DirectChannel(_provider.GetService<IApplicationContext>());

        var handler = new CounterHandler();
        channel.Subscribe(handler);
        IMessage<string> message = Message.Create("test");
        Assert.True(await channel.SendAsync(message));

        for (int i = 0; i < 10_000_000; i++)
        {
            await channel.SendAsync(message);
        }

        Assert.Equal(10_000_001, handler.Count);
    }

    [Fact]
    public void TestSendTwoHandlers_10_000_000()
    {
        /*
         *  INT-3308 - used to run 6.4 million/sec
         *  1. Skip empty iterators as above 7.2 million/sec
         *  2. optimize for single handler 6.7 million/sec (small overhead added)
         *  3. remove LB rwlock from UnicastingDispatcher 7.2 million/sec
         *  4. Move single handler optimization to dispatcher 7.3 million/sec
         */
        var channel = new DirectChannel(_provider.GetService<IApplicationContext>());
        var count1 = new CounterHandler();
        var count2 = new CounterHandler();
        channel.Subscribe(count1);
        channel.Subscribe(count2);
        IMessage<string> message = Message.Create("test");

        for (int i = 0; i < 10_000_000; i++)
        {
            channel.Send(message);
        }

        Assert.Equal(5_000_000, count1.Count);
        Assert.Equal(5_000_000, count2.Count);
    }

    [Fact]
    public void TestSendFourHandlers_10_000_000()
    {
        var channel = new DirectChannel(_provider.GetService<IApplicationContext>());
        var count1 = new CounterHandler();
        var count2 = new CounterHandler();
        var count3 = new CounterHandler();
        var count4 = new CounterHandler();
        channel.Subscribe(count1);
        channel.Subscribe(count2);
        channel.Subscribe(count3);
        channel.Subscribe(count4);
        IMessage<string> message = Message.Create("test");

        for (int i = 0; i < 10_000_000; i++)
        {
            channel.Send(message);
        }

        Assert.Equal(10_000_000 / 4, count1.Count);
        Assert.Equal(10_000_000 / 4, count2.Count);
        Assert.Equal(10_000_000 / 4, count3.Count);
        Assert.Equal(10_000_000 / 4, count4.Count);
    }

    [Fact]
    public void TestSendInSeparateThread()
    {
        var latch = new CountdownEvent(1);
        var channel = new DirectChannel(_provider.GetService<IApplicationContext>());
        var target = new ThreadNameExtractingTestTarget(latch);
        channel.Subscribe(target);
        IMessage<string> message = Message.Create("test");

        var thread = new Thread(() => channel.Send(message))
        {
            Name = "test-thread"
        };

        thread.Start();
        latch.Wait(1000);
        Assert.Equal("test-thread", target.ThreadName);
    }

    internal sealed class CounterHandler : IMessageHandler
    {
        public int Count;

        public string ServiceName { get; set; } = nameof(CounterHandler);

        public void HandleMessage(IMessage message)
        {
            Count++;
        }
    }

    internal sealed class ThreadNameExtractingTestTarget : IMessageHandler
    {
        public readonly CountdownEvent Latch;
        public int? TaskId;
        public int ThreadId;
        public string ThreadName;

        public string ServiceName { get; set; } = nameof(ThreadNameExtractingTestTarget);

        public ThreadNameExtractingTestTarget()
            : this(null)
        {
        }

        public ThreadNameExtractingTestTarget(CountdownEvent latch)
        {
            Latch = latch;
        }

        public void HandleMessage(IMessage message)
        {
            TaskId = Task.CurrentId;
            ThreadId = Thread.CurrentThread.ManagedThreadId;
            ThreadName = Thread.CurrentThread.Name;

            if (Latch != null)
            {
                Latch.Signal();
            }
        }
    }
}
