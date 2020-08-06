﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Host;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Listener
{
    [Trait("Category", "Integration")]
    public class BrokerDeclaredQueueNameTest : AbstractTest
    {
        [Fact]
        public async Task TestBrokerNamedQueueDMLC()
        {
            var latch3 = new CountdownEvent(1);
            var latch4 = new CountdownEvent(2);
            var message = new AtomicReference<IMessage>();
            var services = CreateContainer();
            services.AddRabbitQueue(new Config.Queue(string.Empty, false, true, true));

            services.AddHostedService<RabbitHostService>();
            services.TryAddSingleton<IApplicationContext, GenericApplicationContext>();
            services.TryAddSingleton<Connection.IConnectionFactory, CachingConnectionFactory>();
            services.TryAddSingleton<Converter.ISmartMessageConverter, RabbitMQ.Support.Converter.SimpleMessageConverter>();
            services.AddSingleton((p) =>
            {
                return CreateDMLCContainer(p, latch3, latch4, message);
            });
            services.AddRabbitAdmin();
            services.AddRabbitTemplate();
            var provider = services.BuildServiceProvider();

            await provider.GetRequiredService<IHostedService>().StartAsync(default);

            var container = provider.GetRequiredService<DirectMessageListenerContainer>();
            var cf = provider.GetRequiredService<Connection.IConnectionFactory>() as CachingConnectionFactory;

            await container.Start();
            Assert.True(container._startedLatch.Wait(TimeSpan.FromSeconds(10))); // Really wait for container to start

            var queue = provider.GetRequiredService<IQueue>();
            var template = provider.GetRabbitTemplate();
            var firstActualName = queue.ActualName;
            message.Value = null;
            template.ConvertAndSend(firstActualName, "foo");

            Assert.True(latch3.Wait(TimeSpan.FromSeconds(10)));
            var body = EncodingUtils.GetDefaultEncoding().GetString((byte[])message.Value.Payload);
            Assert.Equal("foo", body);
            var newConnectionLatch = new CountdownEvent(2);
            var conListener = new TestConnectionListener(newConnectionLatch);
            cf.AddConnectionListener(conListener);
            cf.ResetConnection();
            Assert.True(newConnectionLatch.Wait(TimeSpan.FromSeconds(10)));
            var secondActualName = queue.ActualName;
            Assert.NotEqual(firstActualName, secondActualName);
            message.Value = null;
            template.ConvertAndSend(secondActualName, "bar");
            Assert.True(latch4.Wait(TimeSpan.FromSeconds(10)));
            body = EncodingUtils.GetDefaultEncoding().GetString((byte[])message.Value.Payload);
            Assert.Equal("bar", body);
            await container.Stop();
        }

        private DirectMessageListenerContainer CreateDMLCContainer(IServiceProvider services, CountdownEvent latch3, CountdownEvent latch4, AtomicReference<IMessage> message)
        {
            var cf = services.GetRequiredService<Connection.IConnectionFactory>();
            var ctx = services.GetRequiredService<IApplicationContext>();
            var queue2 = services.GetRequiredService<IQueue>();
            var listener = new TestMessageListener(latch3, latch4, message);
            var container = new DirectMessageListenerContainer(ctx, cf);
            container.SetQueues(queue2);
            container.SetupMessageListener(listener);
            container.FailedDeclarationRetryInterval = 1000;
            container.MissingQueuesFatal = false;
            container.RecoveryInterval = 100;
            container.IsAutoStartup = false;

            return container;
        }

        private class TestConnectionListener : IConnectionListener
        {
            public TestConnectionListener(CountdownEvent latch)
            {
                Latch = latch;
            }

            public CountdownEvent Latch { get; }

            public void OnClose(Connection.IConnection connection)
            {
            }

            public void OnCreate(Connection.IConnection connection)
            {
                if (!Latch.IsSet)
                {
                    Latch.Signal();
                }
            }

            public void OnShutDown(RC.ShutdownEventArgs args)
            {
            }
        }

        private class TestMessageListener : IMessageListener
        {
            public TestMessageListener(CountdownEvent latch1, CountdownEvent latch2, AtomicReference<IMessage> message)
            {
                Latch1 = latch1;
                Latch2 = latch2;
                Message = message;
            }

            public AcknowledgeMode ContainerAckMode { get; set; }

            public CountdownEvent Latch1 { get; }

            public CountdownEvent Latch2 { get; }

            public AtomicReference<IMessage> Message { get; }

            public void OnMessage(IMessage message)
            {
                Message.Value = message;
                if (!Latch1.IsSet)
                {
                    Latch1.Signal();
                }

                if (!Latch2.IsSet)
                {
                    Latch2.Signal();
                }
            }

            public void OnMessageBatch(List<IMessage> messages)
            {
                throw new NotImplementedException();
            }
        }
    }
}
