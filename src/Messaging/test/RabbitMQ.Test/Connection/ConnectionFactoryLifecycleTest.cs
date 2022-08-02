// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Events;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Connection.CachingConnectionFactory;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

[Trait("Category", "Integration")]
public class ConnectionFactoryLifecycleTest : AbstractTest
{
    [Fact]
    public async Task TestConnectionFactoryAvailableDuringStop()
    {
        ServiceCollection services = CreateContainer();
        services.AddRabbitConnectionFactory((_, f) => f.Host = "localhost");
        services.AddRabbitAdmin();
        services.AddSingleton<MyLifecycle>();
        services.AddSingleton<ILifecycle>(p => p.GetService<MyLifecycle>());
        ServiceProvider provider = services.BuildServiceProvider();

        var hostService = provider.GetRequiredService<IHostedService>();
        await hostService.StartAsync(default);
        var myLifecycle = provider.GetService<MyLifecycle>();
        var cf = provider.GetService<IConnectionFactory>() as CachingConnectionFactory;
        await provider.DisposeAsync();

        Assert.False(myLifecycle.IsRunning);
        Assert.True(cf.Stopped);
        Assert.Throws<RabbitApplicationContextClosedException>(() => cf.CreateConnection());
    }

    [Fact]
    public async Task TestBlockedConnection()
    {
        ServiceCollection services = CreateContainer();

        services.AddRabbitConnectionFactory((_, f) =>
        {
            f.Host = "localhost";
            f.ServiceName = "TestBlockedConnection";
        });

        services.AddRabbitAdmin();
        ServiceProvider provider = services.BuildServiceProvider();

        var hostService = provider.GetRequiredService<IHostedService>();
        await hostService.StartAsync(default);
        var blockedConnectionLatch = new CountdownEvent(1);
        var unblockedConnectionLatch = new CountdownEvent(1);
        var cf = provider.GetService<IConnectionFactory>() as CachingConnectionFactory;
        var connection = cf.CreateConnection() as ChannelCachingConnectionProxy;
        var listener = new TestBlockedListener(blockedConnectionLatch, unblockedConnectionLatch);
        connection.AddBlockedListener(listener);
        global::RabbitMQ.Client.IConnection amqConnection = connection.Target.Connection;
        amqConnection.HandleConnectionBlocked("Test connection blocked");
        Assert.True(blockedConnectionLatch.Wait(TimeSpan.FromSeconds(10)));
        amqConnection.HandleConnectionUnblocked();
        Assert.True(unblockedConnectionLatch.Wait(TimeSpan.FromSeconds(10)));
        await provider.DisposeAsync();
    }

    public class TestBlockedListener : IBlockedListener
    {
        private readonly CountdownEvent _blockedConnectionLatch;
        private readonly CountdownEvent _unblockedConnectionLatch;

        public TestBlockedListener(CountdownEvent blockedConnectionLatch, CountdownEvent unblockedConnectionLatch)
        {
            _blockedConnectionLatch = blockedConnectionLatch;
            _unblockedConnectionLatch = unblockedConnectionLatch;
        }

        public void HandleBlocked(object sender, ConnectionBlockedEventArgs args)
        {
            _blockedConnectionLatch.Signal();
        }

        public void HandleUnblocked(object sender, EventArgs args)
        {
            _unblockedConnectionLatch.Signal();
        }
    }

    public class MyLifecycle : ISmartLifecycle
    {
        private readonly RabbitAdmin _admin;
        private readonly IQueue _queue = new AnonymousQueue();
        private volatile bool _running;

        public bool IsRunning => _running;

        public int Phase => 0;

        public bool IsAutoStartup => true;

        public MyLifecycle(IConnectionFactory cf)
        {
            _admin = new RabbitAdmin(cf);
        }

        public Task Start()
        {
            _running = true;
            _admin.DeclareQueue(_queue);
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            // Prior to the fix for AMQP-546, this threw an exception and
            // running was not reset.
            _admin.DeleteQueue(_queue.QueueName);
            _running = false;
            return Task.CompletedTask;
        }

        public async Task Stop(Action callback)
        {
            await Stop();
            callback();
        }
    }
}
