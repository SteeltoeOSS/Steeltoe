// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
