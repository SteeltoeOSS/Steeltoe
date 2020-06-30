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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Handler;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;
using System.Threading;
using Xunit;

namespace Steeltoe.Integration.Dispatcher.Test
{
    public class FailOverDispatcherTest
    {
        private readonly IServiceProvider provider;

        public FailOverDispatcherTest()
        {
            var services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(config);
            services.AddSingleton<IApplicationContext, GenericApplicationContext>();
            services.AddSingleton<IMessageBuilderFactory, DefaultMessageBuilderFactory>();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            provider = services.BuildServiceProvider();
        }

        [Fact]
        public void SingleMessage()
        {
            var dispatcher = new UnicastingDispatcher(provider.GetService<IApplicationContext>());
            var latch = new CountdownEvent(1);
            var processor = new LatchedProcessor(latch);
            dispatcher.AddHandler(CreateConsumer(processor));
            dispatcher.Dispatch(Message.Create("test"));
            Assert.True(latch.Wait(500));
        }

        [Fact]
        public void PointToPoint()
        {
            var dispatcher = new UnicastingDispatcher(provider.GetService<IApplicationContext>());
            var latch = new CountdownEvent(1);
            var processor1 = new LatchedProcessor(latch);
            var processor2 = new LatchedProcessor(latch);
            dispatcher.AddHandler(CreateConsumer(processor1));
            dispatcher.AddHandler(CreateConsumer(processor2));
            dispatcher.Dispatch(Message.Create("test"));
            Assert.True(latch.Wait(3000));
            Assert.Equal(1, processor1.Counter + processor2.Counter);
        }

        [Fact]
        public void NoDuplicateSubscriptions()
        {
            var dispatcher = new UnicastingDispatcher(provider.GetService<IApplicationContext>());
            var target = new CountingTestEndpoint(false);
            dispatcher.AddHandler(target);
            dispatcher.AddHandler(target);
            try
            {
                dispatcher.Dispatch(Message.Create("test"));
            }
            catch (Exception)
            {
                // ignore
            }

            Assert.Equal(1, target.Counter);
        }

        [Fact]
        public void RemoveConsumerBeforeSend()
        {
            var dispatcher = new UnicastingDispatcher(provider.GetService<IApplicationContext>());
            var target1 = new CountingTestEndpoint(false);
            var target2 = new CountingTestEndpoint(false);
            var target3 = new CountingTestEndpoint(false);
            dispatcher.AddHandler(target1);
            dispatcher.AddHandler(target2);
            dispatcher.AddHandler(target3);
            dispatcher.RemoveHandler(target2);
            try
            {
                dispatcher.Dispatch(Message.Create("test"));
            }
            catch (Exception)
            {
                // ignore
            }

            Assert.Equal(2, target1.Counter + target2.Counter + target3.Counter);
        }

        [Fact]
        public void RemoveConsumerBetweenSends()
        {
            var dispatcher = new UnicastingDispatcher(provider.GetService<IApplicationContext>());
            var target1 = new CountingTestEndpoint(false);
            var target2 = new CountingTestEndpoint(false);
            var target3 = new CountingTestEndpoint(false);
            dispatcher.AddHandler(target1);
            dispatcher.AddHandler(target2);
            dispatcher.AddHandler(target3);
            try
            {
                dispatcher.Dispatch(Message.Create("test1"));
            }
            catch (Exception)
            {
                // ignore
            }

            Assert.Equal(3, target1.Counter + target2.Counter + target3.Counter);
            dispatcher.RemoveHandler(target2);
            try
            {
                dispatcher.Dispatch(Message.Create("test2"));
            }
            catch (Exception)
            {
                // ignore
            }

            Assert.Equal(5, target1.Counter + target2.Counter + target3.Counter);
            dispatcher.RemoveHandler(target1);
            try
            {
                dispatcher.Dispatch(Message.Create("test3"));
            }
            catch (Exception)
            {
                // ignore
            }

            Assert.Equal(6, target1.Counter + target2.Counter + target3.Counter);
        }

        [Fact]
        public void RemoveConsumerLastTargetCausesDeliveryException()
        {
            var dispatcher = new UnicastingDispatcher(provider.GetService<IApplicationContext>());
            var target1 = new CountingTestEndpoint(false);
            dispatcher.AddHandler(target1);
            try
            {
                dispatcher.Dispatch(Message.Create("test1"));
            }
            catch (Exception)
            {
                // ignore
            }

            Assert.Equal(1, target1.Counter);
            dispatcher.RemoveHandler(target1);
            Assert.Throws<MessageDispatchingException>(() => dispatcher.Dispatch(Message.Create("test2")));
        }

        [Fact]
        public void FirstHandlerReturnsTrue()
        {
            var dispatcher = new UnicastingDispatcher(provider.GetService<IApplicationContext>());
            var target1 = new CountingTestEndpoint(true);
            var target2 = new CountingTestEndpoint(false);
            var target3 = new CountingTestEndpoint(false);
            dispatcher.AddHandler(target1);
            dispatcher.AddHandler(target2);
            dispatcher.AddHandler(target3);
            Assert.True(dispatcher.Dispatch(Message.Create("test")));
            Assert.Equal(1, target1.Counter + target2.Counter + target3.Counter);
        }

        [Fact]
        public void MiddleHandlerReturnsTrue()
        {
            var dispatcher = new UnicastingDispatcher(provider.GetService<IApplicationContext>());
            var target1 = new CountingTestEndpoint(false);
            var target2 = new CountingTestEndpoint(true);
            var target3 = new CountingTestEndpoint(false);
            dispatcher.AddHandler(target1);
            dispatcher.AddHandler(target2);
            dispatcher.AddHandler(target3);
            Assert.True(dispatcher.Dispatch(Message.Create("test")));
            Assert.Equal(2, target1.Counter + target2.Counter + target3.Counter);
        }

        [Fact]
        public void AllHandlersReturnFalse()
        {
            var dispatcher = new UnicastingDispatcher(provider.GetService<IApplicationContext>());
            var target1 = new CountingTestEndpoint(false);
            var target2 = new CountingTestEndpoint(false);
            var target3 = new CountingTestEndpoint(false);
            dispatcher.AddHandler(target1);
            dispatcher.AddHandler(target2);
            dispatcher.AddHandler(target3);
            try
            {
                Assert.False(dispatcher.Dispatch(Message.Create("test")));
            }
            catch (Exception)
            {
            }

            Assert.Equal(3, target1.Counter + target2.Counter + target3.Counter);
        }

        private ServiceActivatingHandler CreateConsumer(IMessageProcessor processor)
        {
            var handler = new ServiceActivatingHandler(provider.GetService<IApplicationContext>(), processor);
            return handler;
        }

        public class CountingTestEndpoint : IMessageHandler
        {
            public int Counter;
            public bool ShouldAccept;

            public CountingTestEndpoint(bool shouldAccept)
            {
                ShouldAccept = shouldAccept;
            }

            public void HandleMessage(IMessage message)
            {
                Counter++;
                if (!ShouldAccept)
                {
                    throw new MessageRejectedException(message, "intentional test failure");
                }
            }
        }

        public class LatchedProcessor : IMessageProcessor
        {
            public int Counter;
            public CountdownEvent Latch;

            public LatchedProcessor(CountdownEvent latch)
            {
                Latch = latch;
            }

            public object ProcessMessage(IMessage message)
            {
                Latch.Signal();
                Counter++;
                return null;
            }
        }
    }
}
