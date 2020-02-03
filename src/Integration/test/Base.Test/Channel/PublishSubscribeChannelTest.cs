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
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Integration.Channel.Test
{
    public class PublishSubscribeChannelTest
    {
        [Fact]
        public void TestSend()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var handler = new CounterHandler();
            var channel = new PublishSubscribeChannel(provider);
            channel.Subscribe(handler);
            var message = new GenericMessage("test");
            Assert.True(channel.Send(message));
            Assert.Equal(1, handler.Count);
        }

        [Fact]
        public async ValueTask TestSendAsync()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var handler = new CounterHandler();
            var channel = new PublishSubscribeChannel(provider);
            channel.Subscribe(handler);
            var message = new GenericMessage("test");
            Assert.True(await channel.SendAsync(message));
            Assert.Equal(1, handler.Count);
        }

        [Fact]
        public void TestSendOneHandler_10_000_000()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var handler = new CounterHandler();
            var channel = new PublishSubscribeChannel(provider);
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
        public async ValueTask TestSendAsyncOneHandler_10_000_000()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var handler = new CounterHandler();
            var channel = new PublishSubscribeChannel(provider);
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
        public async ValueTask TestSendAsyncTwoHandler_10_000_000()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IIntegrationServices, IntegrationServices>();
            var provider = services.BuildServiceProvider();
            var handler1 = new CounterHandler();
            var handler2 = new CounterHandler();
            var channel = new PublishSubscribeChannel(provider);
            channel.Subscribe(handler1);
            channel.Subscribe(handler2);
            var message = new GenericMessage("test");
            for (var i = 0; i < 10000000; i++)
            {
                await channel.SendAsync(message);
            }

            Assert.Equal(10000000, handler1.Count);
            Assert.Equal(10000000, handler2.Count);
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
}
