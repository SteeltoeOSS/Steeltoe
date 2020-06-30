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
using Microsoft.Extensions.Hosting;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Extensions;
using Steeltoe.Messaging.Rabbit.Listener;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using R = RabbitMQ.Client;

namespace Steeltoe.Messaging.Rabbit.Attributes
{
    public class RabbitListenerAttributeProcessorTest
    {
        [Fact]
        public async Task SimpleMessageListener()
        {
            var queues = new List<IQueue>()
            {
                QueueBuilder.Durable("testQueue").Build(),
                QueueBuilder.Durable("secondQueue").Build()
            };
            var provider = await Config.CreateAndStartServices(null, queues, typeof(SimpleMessageListenerTestBean));
            var context = provider.GetService<IApplicationContext>();
            var factory = context.GetService<IRabbitListenerContainerFactory>() as RabbitListenerContainerTestFactory;
            Assert.Single(factory.GetListenerContainers());
            var container = factory.GetListenerContainers()[0] as MessageListenerTestContainer;

            var endpoint = container.Endpoint;
            Assert.IsType<MethodRabbitListenerEndpoint>(endpoint);
            var methodEndpoint = endpoint as MethodRabbitListenerEndpoint;
            Assert.NotNull(methodEndpoint.Instance);
            Assert.NotNull(methodEndpoint.Method);

            var listenerContainer = new DirectMessageListenerContainer(context);
            methodEndpoint.SetupListenerContainer(listenerContainer);
            Assert.NotNull(listenerContainer.MessageListener);
            Assert.True(container.IsStarted);
            provider.Dispose();
            Assert.True(container.IsStopped);
        }

        [Fact]
        public async Task SimpleMessageListenerWithMixedAnnotations()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "rabbit:myQueue", "secondQueue" }
                });
            var config = configBuilder.Build();
            var queues = new List<IQueue>()
            {
                QueueBuilder.Durable("testQueue").Build(),
                QueueBuilder.Durable("secondQueue").Build()
            };
            var provider = await Config.CreateAndStartServices(config, queues, typeof(SimpleMessageListenerWithMixedAnnotationsTestBean));
            var context = provider.GetService<IApplicationContext>();
            var factory = context.GetService<IRabbitListenerContainerFactory>() as RabbitListenerContainerTestFactory;
            Assert.Single(factory.GetListenerContainers());
            var container = factory.GetListenerContainers()[0];

            var endpoint = container.Endpoint;
            Assert.IsType<MethodRabbitListenerEndpoint>(endpoint);
            var methodEndpoint = endpoint as MethodRabbitListenerEndpoint;
            Assert.NotNull(methodEndpoint.Instance);
            Assert.NotNull(methodEndpoint.Method);

            Assert.Equal(2, methodEndpoint.QueueNames.Count);
            Assert.Contains("testQueue", methodEndpoint.QueueNames);
            Assert.Contains("secondQueue", methodEndpoint.QueueNames);

            var listenerContainer = new DirectMessageListenerContainer(context);
            methodEndpoint.SetupListenerContainer(listenerContainer);
            Assert.NotNull(listenerContainer.MessageListener);
            Assert.True(container.IsStarted);
            provider.Dispose();
            Assert.True(container.IsStopped);
        }

        [Fact]
        public async Task MultipleQueueNamesTestBeanTest()
        {
            var queues = new List<IQueue>()
            {
                QueueBuilder.Durable("metaTestQueue").Build(),
                QueueBuilder.Durable("metaTestQueue2").Build()
            };
            var provider = await Config.CreateAndStartServices(null, queues, typeof(MultipleQueueNamesTestBean));
            var context = provider.GetService<IApplicationContext>();
            var factory = context.GetService<IRabbitListenerContainerFactory>() as RabbitListenerContainerTestFactory;
            Assert.Single(factory.GetListenerContainers());
            var container = factory.GetListenerContainers()[0] as MessageListenerTestContainer;

            var endpoint = container.Endpoint as AbstractRabbitListenerEndpoint;
            Assert.NotNull(endpoint);

            Assert.Equal(2, endpoint.QueueNames.Count);
            Assert.Contains("metaTestQueue", endpoint.QueueNames);
            Assert.Contains("metaTestQueue2", endpoint.QueueNames);
        }

        [Fact]
        public async Task MultipleQueuesTestBeanTest()
        {
            var queue1 = QueueBuilder.Durable("metaTestQueue").Build();
            queue1.ServiceName = "queue1";
            var queue2 = QueueBuilder.Durable("metaTestQueue2").Build();
            queue2.ServiceName = "queue2";

            var queues = new List<IQueue>() { queue1, queue2 };
            var provider = await Config.CreateAndStartServices(null, queues, typeof(MultipleQueuesTestBean));
            var context = provider.GetService<IApplicationContext>();
            var factory = context.GetService<IRabbitListenerContainerFactory>() as RabbitListenerContainerTestFactory;
            Assert.Single(factory.GetListenerContainers());
            var container = factory.GetListenerContainers()[0] as MessageListenerTestContainer;

            var endpoint = container.Endpoint as AbstractRabbitListenerEndpoint;
            Assert.NotNull(endpoint);

            Assert.Equal(2, endpoint.QueueNames.Count);
            Assert.Contains("metaTestQueue", endpoint.QueueNames);
            Assert.Contains("metaTestQueue2", endpoint.QueueNames);
        }

        [Fact]
        public async Task MixedQueuesAndQueueNamesTestBeanTest()
        {
            var queue1 = QueueBuilder.Durable("metaTestQueue").Build();
            queue1.ServiceName = "queue1";
            var queue2 = QueueBuilder.Durable("metaTestQueue2").Build();

            var queues = new List<IQueue>() { queue1, queue2 };
            var provider = await Config.CreateAndStartServices(null, queues, typeof(MixedQueuesAndQueueNamesTestBean));
            var context = provider.GetService<IApplicationContext>();
            var factory = context.GetService<IRabbitListenerContainerFactory>() as RabbitListenerContainerTestFactory;
            Assert.Single(factory.GetListenerContainers());
            var container = factory.GetListenerContainers()[0] as MessageListenerTestContainer;

            var endpoint = container.Endpoint as AbstractRabbitListenerEndpoint;
            Assert.NotNull(endpoint);

            Assert.Equal(2, endpoint.QueueNames.Count);
            Assert.Contains("metaTestQueue", endpoint.QueueNames);
            Assert.Contains("metaTestQueue2", endpoint.QueueNames);
        }

        [Fact]
        public async Task PropertyPlaceholderResolvingToQueueTestBeanTest()
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "rabbit:myQueue", "#{@queue1}" }
                });
            var config = configBuilder.Build();

            var queue1 = QueueBuilder.Durable("metaTestQueue").Build();
            queue1.ServiceName = "queue1";
            var queue2 = QueueBuilder.Durable("metaTestQueue2").Build();
            queue2.ServiceName = "queue2";

            var queues = new List<IQueue>() { queue1, queue2 };
            var provider = await Config.CreateAndStartServices(config, queues, typeof(PropertyPlaceholderResolvingToQueueTestBean));
            var context = provider.GetService<IApplicationContext>();
            var factory = context.GetService<IRabbitListenerContainerFactory>() as RabbitListenerContainerTestFactory;
            Assert.Single(factory.GetListenerContainers());
            var container = factory.GetListenerContainers()[0] as MessageListenerTestContainer;

            var endpoint = container.Endpoint as AbstractRabbitListenerEndpoint;
            Assert.NotNull(endpoint);

            Assert.Equal(2, endpoint.QueueNames.Count);
            Assert.Contains("metaTestQueue", endpoint.QueueNames);
            Assert.Contains("metaTestQueue2", endpoint.QueueNames);
        }

        [Fact]
        public async Task InvalidValueInAnnotationTestBeanTest()
        {
            var queue1 = QueueBuilder.Durable("metaTestQueue").Build();
            queue1.ServiceName = "queue1";
            var queue2 = QueueBuilder.Durable("metaTestQueue2").Build();
            queue2.ServiceName = "queue2";

            var queues = new List<IQueue>() { queue1, queue2 };
            var excep = await Assert.ThrowsAsync<InvalidOperationException>(() => Config.CreateAndStartServices(null, queues, typeof(InvalidValueInAnnotationTestBean)));
            Assert.Contains("Unable to resolve expression", excep.Message);
        }

        public class Config
        {
            public static async Task<ServiceProvider> CreateAndStartServices(IConfiguration configuration, List<IQueue> queues, params Type[] listeners)
            {
                var mockConnectionFactory = new Mock<IConnectionFactory>();
                var mockConnection = new Mock<IConnection>();
                var mockChannel = new Mock<R.IModel>();
                mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
                mockConnection.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(mockChannel.Object);
                mockChannel.Setup((c) => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
                mockConnection.Setup((c) => c.IsOpen).Returns(true);
                mockChannel.Setup((c) => c.IsOpen).Returns(true);
                AtomicReference<string> queueName = new AtomicReference<string>();
                mockChannel.Setup(c => c.QueueDeclarePassive(It.IsAny<string>())).Returns(() => new R.QueueDeclareOk(queueName.Value, 0, 0))
                    .Callback<string>((name) => queueName.Value = name);

                var services = new ServiceCollection();
                var config = configuration;
                if (config == null)
                {
                    config = new ConfigurationBuilder().Build();
                }

                services.AddSingleton<IConfiguration>(config);
                services.AddSingleton<IConnectionFactory>(mockConnectionFactory.Object);
                services.AddRabbitHostingServices();
                services.AddRabbitMessageHandlerMethodFactory();
                services.AddRabbitDefaultMessageConverter();
                services.AddRabbitListenerEndpointRegistry();
                services.AddRabbitListenerEndpointRegistrar();
                services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("testFactory");
                services.AddRabbitListenerAttributeProcessor((p, r) =>
                {
                    r.ContainerFactoryServiceName = "testFactory";
                });

                services.AddRabbitQueues(queues.ToArray());

                foreach (var listener in listeners)
                {
                    services.AddSingleton(listener);
                }

                services.AddRabbitListeners(config, listeners);

                var provider = services.BuildServiceProvider();
                await provider.GetRequiredService<IHostedService>().StartAsync(default);
                return provider;
            }
        }

        public class SimpleMessageListenerTestBean
        {
            [RabbitListener("testQueue")]
            public void HandleIt(string body)
            {
            }
        }

        public class SimpleMessageListenerWithMixedAnnotationsTestBean
        {
            [RabbitListener("testQueue", "${rabbit.myQueue}")]
            public void HandleIt(string body)
            {
            }
        }

        public class MultipleQueueNamesTestBean
        {
            [RabbitListener("metaTestQueue", "metaTestQueue2")]
            public void HandleIt(string body)
            {
            }
        }

        public class MultipleQueuesTestBean
        {
            [RabbitListener("#{@queue1}", "#{@queue2}")]
            public void HandleIt(string body)
            {
            }
        }

        public class MixedQueuesAndQueueNamesTestBean
        {
            [RabbitListener("metaTestQueue2", "#{@queue1}")]
            public void HandleIt(string body)
            {
            }
        }

        public class PropertyPlaceholderResolvingToQueueTestBean
        {
            [RabbitListener("${rabbit:myQueue}", "#{@queue2}")]
            public void HandleIt(string body)
            {
            }
        }

        public class InvalidValueInAnnotationTestBean
        {
            [RabbitListener("#{@testFactory}")]
            public void HandleIt(string body)
            {
            }
        }
    }
}
