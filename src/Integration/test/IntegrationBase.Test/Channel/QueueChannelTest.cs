// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Channels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using Xunit;

namespace Steeltoe.Integration.Channel.Test;

public class QueueChannelTest
{
    private readonly IServiceProvider _provider;

    public QueueChannelTest()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<IApplicationContext, GenericApplicationContext>();
        _provider = services.BuildServiceProvider();
    }

    [Fact]
    public void TestSimpleSendAndReceive()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var latch = new CountdownEvent(1);
        var channel = new QueueChannel(provider.GetService<IApplicationContext>());

        Task.Run(() =>
        {
            IMessage message = channel.Receive();

            if (message != null)
            {
                latch.Signal();
            }
        });

        channel.Send(Message.Create("testing"));
        Assert.True(latch.Wait(10000));
    }

    [Fact]
    public void TestSimpleSendAndReceiveWithNonBlockingQueue()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var latch = new CountdownEvent(1);

        var chan = System.Threading.Channels.Channel.CreateBounded<IMessage>(new BoundedChannelOptions(int.MaxValue)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        });

        var channel = new QueueChannel(provider.GetService<IApplicationContext>(), chan);

        Task.Run(() =>
        {
            IMessage message = channel.Receive();

            if (message != null)
            {
                latch.Signal();
            }
        });

        channel.Send(Message.Create("testing"));
        Assert.True(latch.Wait(10000));
    }

    [Fact]
    public void TestSimpleSendAndReceiveWithNonBlockingQueueWithTimeout()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var latch = new CountdownEvent(1);

        var chan = System.Threading.Channels.Channel.CreateBounded<IMessage>(new BoundedChannelOptions(int.MaxValue)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        });

        var channel = new QueueChannel(provider.GetService<IApplicationContext>(), chan);

        Task.Run(() =>
        {
            IMessage message = channel.Receive(1);

            if (message != null)
            {
                latch.Signal();
            }
        });

        channel.Send(Message.Create("testing"));
        Assert.True(latch.Wait(10000));
    }

    [Fact]
    public void TestSimpleSendAndReceiveWithTimeout()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var latch = new CountdownEvent(1);
        var channel = new QueueChannel(provider.GetService<IApplicationContext>());

        Task.Run(() =>
        {
            IMessage message = channel.Receive(1);

            if (message != null)
            {
                latch.Signal();
            }
        });

        channel.Send(Message.Create("testing"));
        Assert.True(latch.Wait(10000));
    }

    [Fact]
    public void TestImmediateReceive()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        bool messageNull = false;
        var channel = new QueueChannel(provider.GetService<IApplicationContext>());
        var latch1 = new CountdownEvent(1);
        var latch2 = new CountdownEvent(1);

        void ReceiveAction1()
        {
            IMessage message = channel.Receive(0);
            messageNull = message == null;
            latch1.Signal();
        }

        Task.Run(ReceiveAction1);
        Assert.True(latch1.Wait(10000));
        channel.Send(Message.Create("testing"));

        void ReceiveAction2()
        {
            IMessage message = channel.Receive(0);

            if (message != null)
            {
                latch2.Signal();
            }
        }

        Task.Run(ReceiveAction2);
        Assert.True(latch2.Wait(10000));
    }

    [Fact]
    public void TestBlockingReceiveAsyncWithNoTimeout()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        bool messageNull = false;
        var channel = new QueueChannel(provider.GetService<IApplicationContext>());
        var latch = new CountdownEvent(1);
        var cancellationTokenSource = new CancellationTokenSource();

        Task.Run(async () =>
        {
            IMessage message = await channel.ReceiveAsync(cancellationTokenSource.Token);
            messageNull = message == null;
            latch.Signal();
        });

        cancellationTokenSource.Cancel();
        Assert.True(latch.Wait(10000));
        Assert.True(messageNull);
    }

    [Fact]
    public void TestBlockingReceiveWithTimeout()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        bool messageNull = false;
        var channel = new QueueChannel(provider.GetService<IApplicationContext>());
        var latch = new CountdownEvent(1);

        Task.Run(() =>
        {
            IMessage message = channel.Receive(5);
            messageNull = message == null;
            latch.Signal();
        });

        Assert.True(latch.Wait(10000));
        Assert.True(messageNull);
    }

    [Fact]
    public void TestBlockingReceiveAsyncWithTimeout()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        bool messageNull = false;
        var channel = new QueueChannel(provider.GetService<IApplicationContext>());
        var latch = new CountdownEvent(1);
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(10000);

        Task.Run(async () =>
        {
            IMessage message = await channel.ReceiveAsync(cancellationTokenSource.Token);
            messageNull = message == null;
            latch.Signal();
        });

        cancellationTokenSource.Cancel();
        Assert.True(latch.Wait(10000));
        Assert.True(messageNull);
    }

    [Fact]
    public void TestImmediateSend()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var channel = new QueueChannel(provider.GetService<IApplicationContext>(), 3);
        bool result1 = channel.Send(Message.Create("test-1"));
        Assert.True(result1);
        bool result2 = channel.Send(Message.Create("test-2"), 100);
        Assert.True(result2);
        bool result3 = channel.Send(Message.Create("test-3"), 0);
        Assert.True(result3);
        bool result4 = channel.Send(Message.Create("test-4"), 0);
        Assert.False(result4);
    }

    [Fact]
    public void TestBlockingSendAsyncWithNoTimeout()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var channel = new QueueChannel(provider.GetService<IApplicationContext>(), 1);
        bool result1 = channel.Send(Message.Create("test-1"));
        Assert.True(result1);
        var latch = new CountdownEvent(1);
        var cancellationTokenSource = new CancellationTokenSource();

        Task.Run(async () =>
        {
            await channel.SendAsync(Message.Create("test-2"), cancellationTokenSource.Token);
            latch.Signal();
        });

        cancellationTokenSource.Cancel();

        Assert.True(latch.Wait(10000));
    }

    [Fact]
    public void TestBlockingSendWithTimeout()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var channel = new QueueChannel(provider.GetService<IApplicationContext>(), 1);
        bool result1 = channel.Send(Message.Create("test-1"));
        Assert.True(result1);
        var latch = new CountdownEvent(1);

        Task.Run(() =>
        {
            channel.Send(Message.Create("test-2"), 5);
            latch.Signal();
        });

        Assert.True(latch.Wait(10000));
    }

    [Fact]
    public void TestBlockingSendAsyncWithTimeout()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var channel = new QueueChannel(provider.GetService<IApplicationContext>(), 1);
        bool result1 = channel.Send(Message.Create("test-1"));
        Assert.True(result1);
        var latch = new CountdownEvent(1);
        var cancellationTokenSource = new CancellationTokenSource();

        Task.Run(async () =>
        {
            cancellationTokenSource.CancelAfter(5);
            await channel.SendAsync(Message.Create("test-2"), cancellationTokenSource.Token);
            latch.Signal();
        });

        Assert.True(latch.Wait(10000));
    }

    [Fact]
    public void TestClear()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var channel = new QueueChannel(provider.GetService<IApplicationContext>(), 2);
        IMessage<string> message1 = Message.Create("test1");
        IMessage<string> message2 = Message.Create("test2");
        IMessage<string> message3 = Message.Create("test3");
        Assert.True(channel.Send(message1));
        Assert.True(channel.Send(message2));
        Assert.False(channel.Send(message3, 0));
        Assert.Equal(2, channel.QueueSize);
        Assert.Equal(2 - 2, channel.RemainingCapacity);
        IList<IMessage> clearedMessages = channel.Clear();
        Assert.NotNull(clearedMessages);
        Assert.Equal(2, clearedMessages.Count);
        Assert.Equal(0, channel.QueueSize);
        Assert.Equal(2, channel.RemainingCapacity);
        Assert.True(channel.Send(message3));
    }

    [Fact]
    public void TestClearEmptyChannel()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var channel = new QueueChannel(provider.GetService<IApplicationContext>());
        IList<IMessage> clearedMessages = channel.Clear();
        Assert.NotNull(clearedMessages);
        Assert.Empty(clearedMessages);
    }

    [Fact]
    public void TestPurge()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IIntegrationServices, IntegrationServices>();
        ServiceProvider provider = services.BuildServiceProvider();
        var channel = new QueueChannel(provider.GetService<IApplicationContext>());
        Assert.Throws<NotSupportedException>(() => channel.Purge(null));
    }
}
