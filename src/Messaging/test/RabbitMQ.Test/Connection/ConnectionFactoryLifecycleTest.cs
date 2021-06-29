﻿// Licensed to the .NET Foundation under one or more agreements.
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
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Connection.CachingConnectionFactory;

namespace Steeltoe.Messaging.RabbitMQ.Connection
{
    [Trait("Category", "Integration")]
    public class ConnectionFactoryLifecycleTest : AbstractTest
    {
        [Fact]
        public async Task TestConnectionFactoryAvailableDuringStop()
        {
            var services = CreateContainer();
            services.AddRabbitConnectionFactory((p, f) => f.Host = "localhost");
            services.AddRabbitAdmin();
            services.AddSingleton<MyLifecycle>();
            services.AddSingleton<ILifecycle>((p) => p.GetService<MyLifecycle>());
            var provider = services.BuildServiceProvider();

            var hostService = provider.GetRequiredService<IHostedService>();
            await hostService.StartAsync(default);
            var myLifecycle = provider.GetService<MyLifecycle>();
            var cf = provider.GetService<IConnectionFactory>() as CachingConnectionFactory;
            provider.Dispose();

            Assert.False(myLifecycle.IsRunning);
            Assert.True(cf._stopped);
            Assert.Throws<RabbitApplicationContextClosedException>(() => cf.CreateConnection());
        }

        [Fact]
        public async Task TestBlockedConnection()
        {
            var services = CreateContainer();
            services.AddRabbitConnectionFactory((p, f) =>
            {
                f.Host = "localhost";
            });
            services.AddRabbitAdmin();
            var provider = services.BuildServiceProvider();

            var hostService = provider.GetRequiredService<IHostedService>();
            await hostService.StartAsync(default);
            var blockedConnectionLatch = new CountdownEvent(1);
            var unblockedConnectionLatch = new CountdownEvent(1);
            var cf = provider.GetService<IConnectionFactory>() as CachingConnectionFactory;
            var connection = cf.CreateConnection() as ChannelCachingConnectionProxy;
            var listener = new TestBlockedListener(blockedConnectionLatch, unblockedConnectionLatch);
            connection.AddBlockedListener(listener);
            var amqConnection = connection.Target.Connection;
            amqConnection.HandleConnectionBlocked("Test connection blocked");
            Assert.True(blockedConnectionLatch.Wait(TimeSpan.FromSeconds(10)));
            amqConnection.HandleConnectionUnblocked();
            Assert.True(unblockedConnectionLatch.Wait(TimeSpan.FromSeconds(10)));
            provider.Dispose();
        }

        public class TestBlockedListener : IBlockedListener
        {
            private readonly CountdownEvent blockedConnectionLatch;
            private readonly CountdownEvent unblockedConnectionLatch;

            public TestBlockedListener(CountdownEvent blockedConnectionLatch, CountdownEvent unblockedConnectionLatch)
            {
                this.blockedConnectionLatch = blockedConnectionLatch;
                this.unblockedConnectionLatch = unblockedConnectionLatch;
            }

            public void HandleBlocked(object sender, ConnectionBlockedEventArgs args)
            {
                blockedConnectionLatch.Signal();
            }

            public void HandleUnblocked(object sender, EventArgs args)
            {
                unblockedConnectionLatch.Signal();
            }
        }

        public class MyLifecycle : ISmartLifecycle
        {
            private readonly RabbitAdmin admin;
            private readonly IQueue queue = new AnonymousQueue();
            private volatile bool running;

            public MyLifecycle(IConnectionFactory cf)
            {
                admin = new RabbitAdmin(cf);
            }

            public Task Start()
            {
                running = true;
                admin.DeclareQueue(queue);
                return Task.CompletedTask;
            }

            public Task Stop()
            {
                // Prior to the fix for AMQP-546, this threw an exception and
                // running was not reset.
                admin.DeleteQueue(queue.QueueName);
                running = false;
                return Task.CompletedTask;
            }

            public bool IsRunning => running;

            public int Phase => 0;

            public bool IsAutoStartup => true;

            public async Task Stop(Action callback)
            {
                await Stop();
                callback();
            }
        }
    }
}
