// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Integration.Channel.Test
{
    public class DirectChannelTest
    {
        [Fact]
        public void TestSend()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var target = new ThreadNameExtractingTestTarget();
            var channel = new DirectChannel(provider);
            channel.Subscribe(target);
            var message = new GenericMessage("test");
            var currentId = Task.CurrentId;
            var curThreadId = Thread.CurrentThread.ManagedThreadId;
            Assert.True(channel.Send(message));
            Assert.Equal(currentId, target.TaskId);
            Assert.Equal(curThreadId, target.ThreadId);
        }

        [Fact]
        public async Task TestSendAsync()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var target = new ThreadNameExtractingTestTarget();
            var channel = new DirectChannel(provider);
            channel.Subscribe(target);
            var message = new GenericMessage("test");
            Assert.True(await channel.SendAsync(message));
        }

        [Fact]
        public void TestSendOneHandler_10_000_000()
        {
            /*
             *  INT-3308 - used to run 12 million/sec
             *  1. optimize for single handler 20 million/sec
             *  2. Don't iterate over empty datatypes 23 million/sec
             *  3. Don't iterate over empty interceptors 31 million/sec
             *  4. Move single handler optimization to dispatcher 34 million/sec
             *
             *  29 million per second with increment counter in the handler
             */
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var channel = new DirectChannel(provider);

            var handler = new CounterHandler();
            channel.Subscribe(handler);
            var message = new GenericMessage("test");
            Assert.True(channel.Send(message));
            for (var i = 0; i < 10000000; i++)
            {
                channel.Send(message);
            }

            Assert.Equal(10000001, handler.Count);
        }

        [Fact]
        public async Task TestSendAsyncOneHandler_10_000_000()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var channel = new DirectChannel(provider);

            var handler = new CounterHandler();
            channel.Subscribe(handler);
            var message = new GenericMessage("test");
            Assert.True(await channel.SendAsync(message));
            for (var i = 0; i < 10000000; i++)
            {
                await channel.SendAsync(message);
            }

            Assert.Equal(10000001, handler.Count);
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
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var channel = new DirectChannel(provider);
            var count1 = new CounterHandler();
            var count2 = new CounterHandler();
            channel.Subscribe(count1);
            channel.Subscribe(count2);
            var message = new GenericMessage("test");
            for (var i = 0; i < 10000000; i++)
            {
                channel.Send(message);
            }

            Assert.Equal(5000000, count1.Count);
            Assert.Equal(5000000, count2.Count);
        }

        [Fact]
        public void TestSendFourHandlers_10_000_000()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var channel = new DirectChannel(provider);
            var count1 = new CounterHandler();
            var count2 = new CounterHandler();
            var count3 = new CounterHandler();
            var count4 = new CounterHandler();
            channel.Subscribe(count1);
            channel.Subscribe(count2);
            channel.Subscribe(count3);
            channel.Subscribe(count4);
            var message = new GenericMessage("test");
            for (var i = 0; i < 10000000; i++)
            {
                channel.Send(message);
            }

            Assert.Equal(10000000 / 4, count1.Count);
            Assert.Equal(10000000 / 4, count2.Count);
            Assert.Equal(10000000 / 4, count3.Count);
            Assert.Equal(10000000 / 4, count4.Count);
        }

        [Fact]
        public void TestSendInSeparateThread()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var latch = new CountdownEvent(1);
            var channel = new DirectChannel(provider);
            var target = new ThreadNameExtractingTestTarget(latch);
            channel.Subscribe(target);
            var message = new GenericMessage("test");
            var thread = new Thread(() => channel.Send(message));
            thread.Name = "test-thread";
            thread.Start();
            latch.Wait(1000);
            Assert.Equal("test-thread", target.ThreadName);
        }

        internal class CounterHandler : IMessageHandler
        {
            public int Count;

            public void HandleMessage(IMessage message)
            {
                Count++;
            }
        }

        internal class ThreadNameExtractingTestTarget : IMessageHandler
        {
            public readonly CountdownEvent Latch;
            public int? TaskId;
            public int ThreadId;
            public string ThreadName;

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
}
