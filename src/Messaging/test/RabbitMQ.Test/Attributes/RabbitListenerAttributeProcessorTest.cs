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
using Steeltoe.Messaging.RabbitMQ.Attributes;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Test.Configuration;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Test.Attributes;

[Trait("Category", "Integration")]
public sealed class RabbitListenerAttributeProcessorTest
{
    [Fact]
    public async Task SimpleMessageListener()
    {
        var queues = new List<IQueue>
        {
            QueueBuilder.Durable("testQueue").Build(),
            QueueBuilder.Durable("secondQueue").Build()
        };

        ServiceProvider provider = await Configuration.CreateAndStartServicesAsync(null, queues, typeof(SimpleMessageListenerTestBean));
        var context = provider.GetService<IApplicationContext>();
        var factory = (RabbitListenerContainerTestFactory)context.GetService<IRabbitListenerContainerFactory>();
        Assert.Single(factory.GetListenerContainers());
        MessageListenerTestContainer container = factory.GetListenerContainers()[0];

        IRabbitListenerEndpoint endpoint = container.Endpoint;
        Assert.IsType<MethodRabbitListenerEndpoint>(endpoint);
        var methodEndpoint = (MethodRabbitListenerEndpoint)endpoint;
        Assert.NotNull(methodEndpoint.Instance);
        Assert.NotNull(methodEndpoint.Method);

        var listenerContainer = new DirectMessageListenerContainer(context);
        methodEndpoint.SetupListenerContainer(listenerContainer);
        Assert.NotNull(listenerContainer.MessageListener);
        Assert.True(container.IsStarted);
        await provider.DisposeAsync();
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

        IConfigurationRoot configurationRoot = configBuilder.Build();

        var queues = new List<IQueue>
        {
            QueueBuilder.Durable("testQueue").Build(),
            QueueBuilder.Durable("secondQueue").Build()
        };

        ServiceProvider provider =
            await Configuration.CreateAndStartServicesAsync(configurationRoot, queues, typeof(SimpleMessageListenerWithMixedAnnotationsTestBean));

        var context = provider.GetService<IApplicationContext>();
        var factory = (RabbitListenerContainerTestFactory)context.GetService<IRabbitListenerContainerFactory>();
        Assert.Single(factory.GetListenerContainers());
        MessageListenerTestContainer container = factory.GetListenerContainers()[0];

        IRabbitListenerEndpoint endpoint = container.Endpoint;
        Assert.IsType<MethodRabbitListenerEndpoint>(endpoint);
        var methodEndpoint = (MethodRabbitListenerEndpoint)endpoint;
        Assert.NotNull(methodEndpoint.Instance);
        Assert.NotNull(methodEndpoint.Method);

        Assert.Equal(2, methodEndpoint.QueueNames.Count);
        Assert.Contains("testQueue", methodEndpoint.QueueNames);
        Assert.Contains("secondQueue", methodEndpoint.QueueNames);

        var listenerContainer = new DirectMessageListenerContainer(context);
        methodEndpoint.SetupListenerContainer(listenerContainer);
        Assert.NotNull(listenerContainer.MessageListener);
        Assert.True(container.IsStarted);
        await provider.DisposeAsync();
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

        ServiceProvider provider = await Configuration.CreateAndStartServicesAsync(null, queues, typeof(MultipleQueueNamesTestBean));
        var context = provider.GetService<IApplicationContext>();
        var factory = (RabbitListenerContainerTestFactory)context.GetService<IRabbitListenerContainerFactory>();
        Assert.Single(factory.GetListenerContainers());
        MessageListenerTestContainer container = factory.GetListenerContainers()[0];

        var endpoint = container.Endpoint as AbstractRabbitListenerEndpoint;
        Assert.NotNull(endpoint);

        Assert.Equal(2, endpoint.QueueNames.Count);
        Assert.Contains("metaTestQueue", endpoint.QueueNames);
        Assert.Contains("metaTestQueue2", endpoint.QueueNames);
    }

    [Fact]
    public async Task MultipleQueuesTestBeanTest()
    {
        IQueue queue1 = QueueBuilder.Durable("metaTestQueue").Build();
        queue1.ServiceName = "queue1";
        IQueue queue2 = QueueBuilder.Durable("metaTestQueue2").Build();
        queue2.ServiceName = "queue2";

        var queues = new List<IQueue>
        {
            queue1,
            queue2
        };

        ServiceProvider provider = await Configuration.CreateAndStartServicesAsync(null, queues, typeof(MultipleQueuesTestBean));
        var context = provider.GetService<IApplicationContext>();
        var factory = (RabbitListenerContainerTestFactory)context.GetService<IRabbitListenerContainerFactory>();
        Assert.Single(factory.GetListenerContainers());
        MessageListenerTestContainer container = factory.GetListenerContainers()[0];

        var endpoint = container.Endpoint as AbstractRabbitListenerEndpoint;
        Assert.NotNull(endpoint);

        Assert.Equal(2, endpoint.QueueNames.Count);
        Assert.Contains("metaTestQueue", endpoint.QueueNames);
        Assert.Contains("metaTestQueue2", endpoint.QueueNames);
    }

    [Fact]
    public async Task MixedQueuesAndQueueNamesTestBeanTest()
    {
        IQueue queue1 = QueueBuilder.Durable("metaTestQueue").Build();
        queue1.ServiceName = "queue1";
        IQueue queue2 = QueueBuilder.Durable("metaTestQueue2").Build();

        var queues = new List<IQueue>
        {
            queue1,
            queue2
        };

        ServiceProvider provider = await Configuration.CreateAndStartServicesAsync(null, queues, typeof(MixedQueuesAndQueueNamesTestBean));
        var context = provider.GetService<IApplicationContext>();
        var factory = (RabbitListenerContainerTestFactory)context.GetService<IRabbitListenerContainerFactory>();
        Assert.Single(factory.GetListenerContainers());
        MessageListenerTestContainer container = factory.GetListenerContainers()[0];

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

        IConfigurationRoot configurationRoot = configBuilder.Build();

        IQueue queue1 = QueueBuilder.Durable("metaTestQueue").Build();
        queue1.ServiceName = "queue1";
        IQueue queue2 = QueueBuilder.Durable("metaTestQueue2").Build();
        queue2.ServiceName = "queue2";

        var queues = new List<IQueue>
        {
            queue1,
            queue2
        };

        ServiceProvider provider =
            await Configuration.CreateAndStartServicesAsync(configurationRoot, queues, typeof(PropertyPlaceholderResolvingToQueueTestBean));

        var context = provider.GetService<IApplicationContext>();
        var factory = (RabbitListenerContainerTestFactory)context.GetService<IRabbitListenerContainerFactory>();
        Assert.Single(factory.GetListenerContainers());
        MessageListenerTestContainer container = factory.GetListenerContainers()[0];

        var endpoint = container.Endpoint as AbstractRabbitListenerEndpoint;
        Assert.NotNull(endpoint);

        Assert.Equal(2, endpoint.QueueNames.Count);
        Assert.Contains("metaTestQueue", endpoint.QueueNames);
        Assert.Contains("metaTestQueue2", endpoint.QueueNames);
    }

    [Fact]
    public async Task InvalidValueInAnnotationTestBeanTest()
    {
        IQueue queue1 = QueueBuilder.Durable("metaTestQueue").Build();
        queue1.ServiceName = "queue1";
        IQueue queue2 = QueueBuilder.Durable("metaTestQueue2").Build();
        queue2.ServiceName = "queue2";

        var queues = new List<IQueue>
        {
            queue1,
            queue2
        };

        await Assert.ThrowsAsync<ExpressionException>(async () =>
            await Configuration.CreateAndStartServicesAsync(null, queues, typeof(InvalidValueInAnnotationTestBean)));
    }

    [Fact]
    public void CreateExchangeReturnsCorrectType()
    {
        var services = new ServiceCollection();

        RabbitListenerDeclareAttributeProcessor.ProcessDeclareAttributes(services, null, typeof(TestTarget));
        IExchange[] exchanges = services.BuildServiceProvider(true).GetServices<IExchange>().ToArray();
        Assert.Contains(exchanges, ex => ex.Type == ExchangeType.Direct);
        Assert.Contains(exchanges, ex => ex.Type == ExchangeType.Topic);
        Assert.Contains(exchanges, ex => ex.Type == ExchangeType.FanOut);
        Assert.Contains(exchanges, ex => ex.Type == ExchangeType.Headers);
        Assert.Contains(exchanges, ex => ex.Type == ExchangeType.System);
    }

    public static class Configuration
    {
        public static async Task<ServiceProvider> CreateAndStartServicesAsync(IConfiguration configuration, List<IQueue> queues, params Type[] listeners)
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
            IConfiguration effectiveConfiguration = configuration ?? new ConfigurationBuilder().Build();

            services.AddSingleton(effectiveConfiguration);
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

            foreach (Type listener in listeners)
            {
                services.AddSingleton(listener);
            }

            services.AddRabbitListeners(effectiveConfiguration, listeners);

            ServiceProvider provider = services.BuildServiceProvider(true);
            await provider.GetRequiredService<IHostedService>().StartAsync(default);
            return provider;
        }
    }

    public sealed class TestTarget
    {
        [DeclareExchange(Name = "test", Type = ExchangeType.Direct)]
        [DeclareExchange(Name = "test", Type = ExchangeType.Topic)]
        [DeclareExchange(Name = "test", Type = ExchangeType.FanOut)]
        [DeclareExchange(Name = "test", Type = ExchangeType.Headers)]
        [DeclareExchange(Name = "test", Type = ExchangeType.System)]
        public void Method()
        {
        }
    }

    public sealed class SimpleMessageListenerTestBean
    {
        [RabbitListener("testQueue")]
        public void HandleIt(string body)
        {
        }
    }

    public sealed class SimpleMessageListenerWithMixedAnnotationsTestBean
    {
        [RabbitListener("testQueue", "${rabbit.myQueue}")]
        public void HandleIt(string body)
        {
        }
    }

    public sealed class MultipleQueueNamesTestBean
    {
        [RabbitListener("metaTestQueue", "metaTestQueue2")]
        public void HandleIt(string body)
        {
        }
    }

    public sealed class MultipleQueuesTestBean
    {
        [RabbitListener("#{@queue1}", "#{@queue2}")]
        public void HandleIt(string body)
        {
        }
    }

    public sealed class MixedQueuesAndQueueNamesTestBean
    {
        [RabbitListener("metaTestQueue2", "#{@queue1}")]
        public void HandleIt(string body)
        {
        }
    }

    public sealed class PropertyPlaceholderResolvingToQueueTestBean
    {
        [RabbitListener("${rabbit:myQueue}", "#{@queue2}")]
        public void HandleIt(string body)
        {
        }
    }

    public sealed class InvalidValueInAnnotationTestBean
    {
        [RabbitListener("#{@testFactory}")]
        public void HandleIt(string body)
        {
        }
    }
}
