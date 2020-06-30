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
using Moq;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Extensions;
using Steeltoe.Messaging.Rabbit.Listener.Adapters;
using System.Collections.Generic;
using Xunit;
using R = RabbitMQ.Client;

namespace Steeltoe.Messaging.Rabbit.Attributes
{
    public class MessageHandlerTest
    {
        [Fact]
        public void TestMessages()
        {
            var services = new ServiceCollection();
            services.AddRabbitServices();
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            var provider = services.BuildServiceProvider();
            var bpp = provider.GetService<IRabbitListenerAttributeProcessor>() as RabbitListenerAttributeProcessor;
            bpp.Initialize();
            var factory = bpp.MessageHandlerMethodFactory;
            var foo = new Foo();
            var invMethod = factory.CreateInvocableHandlerMethod(foo, typeof(Foo).GetMethod("Listen1"));
            var message = Message.Create("foo");
            var list = new List<IMessage>() { message };
            var mockChannel = new Mock<R.IModel>();
            var adapter = new HandlerAdapter(invMethod);
            adapter.Invoke(Message.Create(list), mockChannel.Object);
            Assert.Same(list, foo.MessagingMessages);
        }

        public class Foo
        {
            public List<IMessage> MessagingMessages;

            public void Listen1(List<IMessage> mMessages)
            {
                MessagingMessages = mMessages;
            }
        }
    }
}
