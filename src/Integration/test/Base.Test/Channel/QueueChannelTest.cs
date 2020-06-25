// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Channels = System.Threading.Channels;

namespace Steeltoe.Integration.Channel.Test
{
    public class QueueChannelTest
    {
        [Fact]
        public void TestSimpleSendAndReceive()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var latch = new CountdownEvent(1);
            var channel = new QueueChannel(provider);
            Task.Run(() =>
            {
                var message = channel.Receive();
                if (message != null)
                {
                    latch.Signal();
                }
            });
            channel.Send(new GenericMessage("testing"));
            Assert.True(latch.Wait(10000));
        }

        [Fact]
        public void TestSimpleSendAndReceiveWithNonBlockingQueue()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var latch = new CountdownEvent(1);
            var chan = Channels.Channel.CreateBounded<IMessage>(new Channels.BoundedChannelOptions(int.MaxValue) { FullMode = Channels.BoundedChannelFullMode.DropWrite });
            var channel = new QueueChannel(provider, chan);
            Task.Run(() =>
            {
                var message = channel.Receive();
                if (message != null)
                {
                    latch.Signal();
                }
            });
            channel.Send(new GenericMessage("testing"));
            Assert.True(latch.Wait(10000));
        }

        [Fact]
        public void TestSimpleSendAndReceiveWithNonBlockingQueueWithTimeout()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var latch = new CountdownEvent(1);
            var chan = Channels.Channel.CreateBounded<IMessage>(new Channels.BoundedChannelOptions(int.MaxValue) { FullMode = Channels.BoundedChannelFullMode.DropWrite });
            var channel = new QueueChannel(provider, chan);
            Task.Run(() =>
            {
                var message = channel.Receive(1);
                if (message != null)
                {
                    latch.Signal();
                }
            });
            channel.Send(new GenericMessage("testing"));
            Assert.True(latch.Wait(10000));
        }

        [Fact]
        public void TestSimpleSendAndReceiveWithTimeout()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var latch = new CountdownEvent(1);
            var channel = new QueueChannel(provider);
            Task.Run(() =>
            {
                var message = channel.Receive(1);
                if (message != null)
                {
                    latch.Signal();
                }
            });
            channel.Send(new GenericMessage("testing"));
            Assert.True(latch.Wait(10000));
        }

        [Fact]
        public void TestImmediateReceive()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var messageNull = false;
            var channel = new QueueChannel(provider);
            var latch1 = new CountdownEvent(1);
            var latch2 = new CountdownEvent(1);
            void RecvAction1()
            {
                var message = channel.Receive(0);
                messageNull = message == null;
                latch1.Signal();
            }

            Task.Run(RecvAction1);
            Assert.True(latch1.Wait(10000));
            channel.Send(new GenericMessage("testing"));
            void RecvAction2()
            {
                var message = channel.Receive(0);
                if (message != null)
                {
                    latch2.Signal();
                }
            }

            Task.Run(RecvAction2);
            Assert.True(latch2.Wait(10000));
        }

        [Fact]
        public void TestBlockingReceiveAsyncWithNoTimeout()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var messageNull = false;
            var channel = new QueueChannel(provider);
            var latch = new CountdownEvent(1);
            var cancellationTokenSource = new CancellationTokenSource();
            var task = Task.Run(async () =>
           {
               var message = await channel.ReceiveAsync(cancellationTokenSource.Token);
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
            var provider = services.BuildServiceProvider();
            var messageNull = false;
            var channel = new QueueChannel(provider);
            var latch = new CountdownEvent(1);
            Task.Run(() =>
            {
                var message = channel.Receive(5);
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
            var provider = services.BuildServiceProvider();
            var messageNull = false;
            var channel = new QueueChannel(provider);
            var latch = new CountdownEvent(1);
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(10000);
            Task.Run(async () =>
            {
                var message = await channel.ReceiveAsync(cancellationTokenSource.Token);
                messageNull = message == null;
                latch.Signal();
            });
            cancellationTokenSource.Cancel();
            Assert.True(latch.Wait(10000));
            Assert.True(messageNull);
        }

        [Fact]
        public void TestBlockingReceiveWithTimeoutEmptyThenSend()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var messageNull = false;
            var channel = new QueueChannel(provider);
            var latch = new CountdownEvent(1);
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(10000);
            Task.Run(async () =>
            {
                var message = await channel.ReceiveAsync(cancellationTokenSource.Token);
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
            var provider = services.BuildServiceProvider();
            var channel = new QueueChannel(provider, 3);
            var result1 = channel.Send(new GenericMessage("test-1"));
            Assert.True(result1);
            var result2 = channel.Send(new GenericMessage("test-2"), 100);
            Assert.True(result2);
            var result3 = channel.Send(new GenericMessage("test-3"), 0);
            Assert.True(result3);
            var result4 = channel.Send(new GenericMessage("test-4"), 0);
            Assert.False(result4);
        }

        [Fact]
        public void TestBlockingSendAsyncWithNoTimeout()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var channel = new QueueChannel(provider, 1);
            var result1 = channel.Send(new GenericMessage("test-1"));
            Assert.True(result1);
            var latch = new CountdownEvent(1);
            var cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                await channel.SendAsync(new GenericMessage("test-2"), cancellationTokenSource.Token);
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
            var provider = services.BuildServiceProvider();
            var channel = new QueueChannel(provider, 1);
            var result1 = channel.Send(new GenericMessage("test-1"));
            Assert.True(result1);
            var latch = new CountdownEvent(1);
            Task.Run(() =>
            {
                channel.Send(new GenericMessage("test-2"), 5);
                latch.Signal();
            });

            Assert.True(latch.Wait(10000));
        }

        [Fact]
        public void TestBlockingSendAsyncWithTimeout()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var channel = new QueueChannel(provider, 1);
            var result1 = channel.Send(new GenericMessage("test-1"));
            Assert.True(result1);
            var latch = new CountdownEvent(1);
            var cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                cancellationTokenSource.CancelAfter(5);
                await channel.SendAsync(new GenericMessage("test-2"), cancellationTokenSource.Token);
                latch.Signal();
            });

            Assert.True(latch.Wait(10000));
        }

        [Fact]
        public void TestClear()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var channel = new QueueChannel(provider, 2);
            var message1 = new GenericMessage("test1");
            var message2 = new GenericMessage("test2");
            var message3 = new GenericMessage("test3");
            Assert.True(channel.Send(message1));
            Assert.True(channel.Send(message2));
            Assert.False(channel.Send(message3, 0));
            Assert.Equal(2, channel.QueueSize);
            Assert.Equal(2 - 2, channel.RemainingCapacity);
            var clearedMessages = channel.Clear();
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
            var provider = services.BuildServiceProvider();
            var channel = new QueueChannel(provider);
            var clearedMessages = channel.Clear();
            Assert.NotNull(clearedMessages);
            Assert.Empty(clearedMessages);
        }

        [Fact]
        public void TestPurge()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var channel = new QueueChannel(provider);
            Assert.Throws<NotSupportedException>(() => channel.Purge(null));
        }
    }
}
