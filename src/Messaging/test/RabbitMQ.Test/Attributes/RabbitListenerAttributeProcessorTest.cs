// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Attributes;

[Trait("Category", "Integration")]
public class RabbitListenerAttributeProcessorTest
{
    [Fact]
    public async Task SimpleMessageListener()
    {
        var queues = new List<IQueue>
        {
            QueueBuilder.Durable("testQueue").Build(),
            QueueBuilder.Durable("secondQueue").Build()
        };
        var provider = await Config.CreateAndStartServices(null, queues, typeof(SimpleMessageListenerTestBean));
        var context = provider.GetService<IApplicationContext>();
        var factory = context.GetService<IRabbitListenerContainerFactory>() as RabbitListenerContainerTestFactory;
        Assert.Single(factory.GetListenerContainers());
        var container = factory.GetListenerContainers()[0];

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
        configBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "rabbit:myQueue", "secondQueue" }
        });
        var config = configBuilder.Build();
        var queues = new List<IQueue>
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
        var queues = new List<IQueue>
        {
            QueueBuilder.Durable("metaTestQueue").Build(),
            QueueBuilder.Durable("metaTestQueue2").Build()
        };
        var provider = await Config.CreateAndStartServices(null, queues, typeof(MultipleQueueNamesTestBean));
        var context = provider.GetService<IApplicationContext>();
        var factory = context.GetService<IRabbitListenerContainerFactory>() as RabbitListenerContainerTestFactory;
        Assert.Single(factory.GetListenerContainers());
        var container = factory.GetListenerContainers()[0];

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

        var queues = new List<IQueue> { queue1, queue2 };
        var provider = await Config.CreateAndStartServices(null, queues, typeof(MultipleQueuesTestBean));
        var context = provider.GetService<IApplicationContext>();
        var factory = context.GetService<IRabbitListenerContainerFactory>() as RabbitListenerContainerTestFactory;
        Assert.Single(factory.GetListenerContainers());
        var container = factory.GetListenerContainers()[0];

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

        var queues = new List<IQueue> { queue1, queue2 };
        var provider = await Config.CreateAndStartServices(null, queues, typeof(MixedQueuesAndQueueNamesTestBean));
        var context = provider.GetService<IApplicationContext>();
        var factory = context.GetService<IRabbitListenerContainerFactory>() as RabbitListenerContainerTestFactory;
        Assert.Single(factory.GetListenerContainers());
        var container = factory.GetListenerContainers()[0];

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
        configBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "rabbit:myQueue", "#{@queue1}" }
        });
        var config = configBuilder.Build();

        var queue1 = QueueBuilder.Durable("metaTestQueue").Build();
        queue1.ServiceName = "queue1";
        var queue2 = QueueBuilder.Durable("metaTestQueue2").Build();
        queue2.ServiceName = "queue2";

        var queues = new List<IQueue> { queue1, queue2 };
        var provider = await Config.CreateAndStartServices(config, queues, typeof(PropertyPlaceholderResolvingToQueueTestBean));
        var context = provider.GetService<IApplicationContext>();
        var factory = context.GetService<IRabbitListenerContainerFactory>() as RabbitListenerContainerTestFactory;
        Assert.Single(factory.GetListenerContainers());
        var container = factory.GetListenerContainers()[0];

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

        var queues = new List<IQueue> { queue1, queue2 };
        await Assert.ThrowsAsync<ExpressionException>(() => Config.CreateAndStartServices(null, queues, typeof(InvalidValueInAnnotationTestBean)));
    }

    [Fact]
    public void CreateExchangeReturnsCorrectType()
    {
        var services = new ServiceCollection();

        RabbitListenerDeclareAtrributeProcessor.ProcessDeclareAttributes(services, null, typeof(TestTarget));
        var exchanges = services.BuildServiceProvider().GetServices<IExchange>();
        Assert.Contains(exchanges, ex => ex.Type == ExchangeType.Direct);
        Assert.Contains(exchanges, ex => ex.Type == ExchangeType.Topic);
        Assert.Contains(exchanges, ex => ex.Type == ExchangeType.Fanout);
        Assert.Contains(exchanges, ex => ex.Type == ExchangeType.Headers);
        Assert.Contains(exchanges, ex => ex.Type == ExchangeType.System);
    }

    public static class Config
    {
        public static async Task<ServiceProvider> CreateAndStartServices(IConfiguration configuration, List<IQueue> queues, params Type[] listeners)
        {
            var mockConnectionFactory = new Mock<IConnectionFactory>();
            var mockConnection = new Mock<IConnection>();
            var mockChannel = new Mock<RC.IModel>();
            mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
            mockConnection.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(mockChannel.Object);
            mockChannel.Setup(c => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
            mockConnection.Setup(c => c.IsOpen).Returns(true);
            mockChannel.Setup(c => c.IsOpen).Returns(true);
            var queueName = new AtomicReference<string>();
            mockChannel.Setup(c => c.QueueDeclarePassive(It.IsAny<string>())).Returns(() => new RC.QueueDeclareOk(queueName.Value, 0, 0))
                .Callback<string>(name => queueName.Value = name);

            var services = new ServiceCollection();
            var config = configuration ?? new ConfigurationBuilder().Build();

            services.AddSingleton(config);
            services.AddSingleton(mockConnectionFactory.Object);
            services.AddRabbitHostingServices();
            services.AddRabbitMessageHandlerMethodFactory();
            services.AddRabbitDefaultMessageConverter();
            services.AddRabbitListenerEndpointRegistry();
            services.AddRabbitListenerEndpointRegistrar();
            services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("testFactory");
            services.AddRabbitListenerAttributeProcessor((_, r) =>
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

    public class TestTarget
    {
        [DeclareExchange(Name ="test", Type = ExchangeType.Direct)]
        [DeclareExchange(Name = "test", Type = ExchangeType.Topic)]
        [DeclareExchange(Name = "test", Type = ExchangeType.Fanout)]
        [DeclareExchange(Name = "test", Type = ExchangeType.Headers)]
        [DeclareExchange(Name = "test", Type = ExchangeType.System)]
        public void Method()
        {
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
