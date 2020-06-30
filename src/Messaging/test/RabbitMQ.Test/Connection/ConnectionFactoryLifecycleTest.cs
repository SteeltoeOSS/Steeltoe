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

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client.Events;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Extensions;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Steeltoe.Messaging.Rabbit.Connection.CachingConnectionFactory;

namespace Steeltoe.Messaging.Rabbit.Connection
{
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
            private CountdownEvent blockedConnectionLatch;
            private CountdownEvent unblockedConnectionLatch;

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
            private RabbitAdmin admin;
            private IQueue queue = new AnonymousQueue();
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
