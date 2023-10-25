// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Attributes;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.RabbitMQ.Test.Configuration;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Test.Attributes;

[Trait("Category", "Integration")]
public sealed class EnableRabbitTest
{
    [Fact]
    public async Task SampleConfiguration()
    {
        ServiceProvider services = await RabbitSampleConfig.CreateAndStartServicesAsync();
        var context = services.GetRequiredService<IApplicationContext>();
        TestSampleConfiguration(context, 2);
    }

    [Fact]
    public async Task FullConfiguration()
    {
        ServiceProvider services = await RabbitFullConfig.CreateAndStartServicesAsync();
        var context = services.GetRequiredService<IApplicationContext>();
        TestFullConfiguration(context);
    }

    [Fact]
    public async Task FullConfigurableConfiguration()
    {
        ServiceProvider services = await RabbitFullConfigurableConfig.CreateAndStartServicesAsync();
        var context = services.GetRequiredService<IApplicationContext>();
        TestFullConfiguration(context);
    }

    [Fact]
    public async Task NoRabbitAdminConfiguration()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await RabbitSampleConfig.CreateAndStartServicesAsync(typeof(FullBean)));
        Assert.Contains("rabbitAdmin", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CustomConfiguration()
    {
        ServiceProvider services = await RabbitCustomConfig.CreateAndStartServicesAsync();
        var context = services.GetRequiredService<IApplicationContext>();
        TestCustomConfiguration(context);
    }

    [Fact]
    public async Task ExplicitContainerFactory()
    {
        ServiceProvider services = await RabbitCustomContainerFactoryConfig.CreateAndStartServicesAsync();
        var context = services.GetRequiredService<IApplicationContext>();
        TestExplicitContainerFactoryConfiguration(context);
    }

    [Fact]
    public async Task DefaultContainerFactory()
    {
        ServiceProvider services = await RabbitDefaultContainerFactoryConfig.CreateAndStartServicesAsync(typeof(DefaultBean));
        var context = services.GetRequiredService<IApplicationContext>();
        TestDefaultContainerFactoryConfiguration(context);
    }

    [Fact]
    public async Task RabbitListeners()
    {
        ServiceProvider services =
            await RabbitDefaultContainerFactoryConfig.CreateAndStartServicesAsync(typeof(RabbitListenersBean), typeof(ClassLevelListenersBean));

        var context = services.GetRequiredService<IApplicationContext>();
        TestRabbitListenerRepeatable(context);
    }

    [Fact]
    public async Task UnknownFactory()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await RabbitSampleConfig.CreateAndStartServicesAsync(typeof(CustomBean)));

        Assert.Contains("customFactory", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidPriorityConfiguration()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await RabbitSampleConfig.CreateAndStartServicesAsync(typeof(InvalidPriorityBean)));

        Assert.Contains("NotANumber", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task TestProperShutdownOnException()
    {
        ServiceProvider services = await TestProperShutdownOnExceptionConfig.CreateAndStartServicesAsync();
        var context = services.GetRequiredService<IApplicationContext>();
        var listenerEndpointRegistry = context.GetService<IRabbitListenerEndpointRegistry>();
        await services.DisposeAsync();

        foreach (IMessageListenerContainer messageListenerContainer in listenerEndpointRegistry.GetListenerContainers())
        {
            Assert.False(messageListenerContainer.IsRunning);
        }
    }

    private void TestRabbitListenerRepeatable(IApplicationContext context)
    {
        var defaultFactory = (RabbitListenerContainerTestFactory)context.GetService<IRabbitListenerContainerFactory>("rabbitListenerContainerFactory");
        Assert.Equal(4, defaultFactory.GetListenerContainers().Count);

        var first = (AbstractRabbitListenerEndpoint)defaultFactory.GetListenerContainer("first").Endpoint;
        Assert.Equal("first", first.Id);
        Assert.Single(first.QueueNames);
        Assert.Equal("myQueue", first.QueueNames[0]);

        var second = (AbstractRabbitListenerEndpoint)defaultFactory.GetListenerContainer("second").Endpoint;
        Assert.Equal("second", second.Id);
        Assert.Single(second.QueueNames);
        Assert.Equal("anotherQueue", second.QueueNames[0]);

        var third = (AbstractRabbitListenerEndpoint)defaultFactory.GetListenerContainer("third").Endpoint;
        Assert.Equal("third", third.Id);
        Assert.Single(third.QueueNames);
        Assert.Equal("class1", third.QueueNames[0]);

        var fourth = (AbstractRabbitListenerEndpoint)defaultFactory.GetListenerContainer("fourth").Endpoint;
        Assert.Equal("fourth", fourth.Id);
        Assert.Single(fourth.QueueNames);
        Assert.Equal("class2", fourth.QueueNames[0]);
    }

    private void TestSampleConfiguration(IApplicationContext context, int expectedDefaultContainers)
    {
        var defaultFactory = (RabbitListenerContainerTestFactory)context.GetService<IRabbitListenerContainerFactory>("rabbitListenerContainerFactory");
        var simpleFactory = (RabbitListenerContainerTestFactory)context.GetService<IRabbitListenerContainerFactory>("simpleFactory");

        Assert.Equal(expectedDefaultContainers, defaultFactory.ListenerContainers.Count);
        Assert.Single(simpleFactory.ListenerContainers);

        IEnumerable<IQueue> queues = context.GetRabbitQueues();

        foreach (IQueue queue in queues)
        {
            Assert.True(queue.IgnoreDeclarationExceptions);
            Assert.False(queue.ShouldDeclare);
            CheckAdmins(queue.DeclaringAdmins);
        }

        IEnumerable<IExchange> exchanges = context.GetRabbitExchanges();

        foreach (IExchange exchange in exchanges)
        {
            Assert.True(exchange.IgnoreDeclarationExceptions);
            Assert.False(exchange.ShouldDeclare);
            CheckAdmins(exchange.DeclaringAdmins);
        }

        IEnumerable<IBinding> bindings = context.GetRabbitBindings();

        foreach (IBinding binding in bindings)
        {
            Assert.True(binding.IgnoreDeclarationExceptions);
            Assert.False(binding.ShouldDeclare);
            CheckAdmins(binding.DeclaringAdmins);
        }
    }

    private void TestFullConfiguration(IApplicationContext context)
    {
        var simpleFactory = (RabbitListenerContainerTestFactory)context.GetService<IRabbitListenerContainerFactory>("simpleFactory");
        Assert.Single(simpleFactory.ListenerContainers);

        MessageListenerTestContainer testContainer = simpleFactory.GetListenerContainers()[0];
        var endpoint = (AbstractRabbitListenerEndpoint)testContainer.Endpoint;
        Assert.Equal("listener1", endpoint.Id);
        AssertQueues(endpoint, "queue1", "queue2");
        Assert.Empty(endpoint.Queues);
        Assert.True(endpoint.Exclusive);
        Assert.Equal(34, endpoint.Priority);
        IRabbitAdmin admin = context.GetRabbitAdmin();
        Assert.Same(endpoint.Admin, admin);

        var container = new DirectMessageListenerContainer(context);
        endpoint.SetupListenerContainer(container);
        var listener = (MessagingMessageListenerAdapter)container.MessageListener;

        var accessor = new RabbitHeaderAccessor
        {
            ContentType = MessageHeaders.ContentTypeTextPlain
        };

        IMessage<byte[]> message = Message.Create(Encoding.UTF8.GetBytes("Hello"), accessor.MessageHeaders);
        var mockChannel = new Mock<RC.IModel>();

        listener.OnMessage(message, mockChannel.Object);
    }

    private void TestCustomConfiguration(IApplicationContext context)
    {
        var customFactory = (RabbitListenerContainerTestFactory)context.GetService<IRabbitListenerContainerFactory>("customFactory");
        var defaultFactory = (RabbitListenerContainerTestFactory)context.GetService<IRabbitListenerContainerFactory>("rabbitListenerContainerFactory");
        Assert.Single(defaultFactory.ListenerContainers);
        Assert.Single(customFactory.ListenerContainers);
        MessageListenerTestContainer testContainer = defaultFactory.GetListenerContainers()[0];
        IRabbitListenerEndpoint endpoint = testContainer.Endpoint;
        Assert.IsType<SimpleRabbitListenerEndpoint>(endpoint);
        var simpleEndpoint = (SimpleRabbitListenerEndpoint)endpoint;
        Assert.IsType<MessageListenerAdapter>(simpleEndpoint.MessageListener);
        var customRegistry = context.GetService<IRabbitListenerEndpointRegistry>();
        Assert.IsType<CustomRabbitListenerEndpointRegistry>(customRegistry);
        Assert.Equal(2, customRegistry.GetListenerContainerIds().Count);
        Assert.Equal(2, customRegistry.GetListenerContainers().Count);
        Assert.NotNull(customRegistry.GetListenerContainer("listenerId"));
        Assert.NotNull(customRegistry.GetListenerContainer("myCustomEndpointId"));
    }

    private void TestDefaultContainerFactoryConfiguration(IApplicationContext context)
    {
        var defaultFactory = (RabbitListenerContainerTestFactory)context.GetService<IRabbitListenerContainerFactory>("rabbitListenerContainerFactory");
        Assert.Single(defaultFactory.GetListenerContainers());
    }

    private void TestExplicitContainerFactoryConfiguration(IApplicationContext context)
    {
        var defaultFactory = (RabbitListenerContainerTestFactory)context.GetService<IRabbitListenerContainerFactory>("simpleFactory");
        Assert.Single(defaultFactory.GetListenerContainers());
    }

    private void AssertQueues(AbstractRabbitListenerEndpoint endpoint, params string[] queues)
    {
        List<string> actualQueues = endpoint.QueueNames;

        foreach (string expectedQueue in queues)
        {
            Assert.Contains(expectedQueue, actualQueues);
        }

        Assert.Equal(queues.Length, actualQueues.Count);
    }

    private void CheckAdmins(List<object> admins)
    {
        Assert.Single(admins);

        if (admins[0] is RabbitAdmin admin)
        {
            Assert.Equal("myAdmin", admin.ServiceName);
        }
        else
        {
            Assert.Equal("myAdmin", (string)admins[0]);
        }
    }

    public static class TestProperShutdownOnExceptionConfig
    {
        public static async Task<ServiceProvider> CreateAndStartServicesAsync()
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
            IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(configurationRoot);
            services.AddRabbitHostingServices();
            services.AddRabbitMessageHandlerMethodFactory();
            services.AddRabbitListenerAttributeProcessor();
            services.AddRabbitDefaultMessageConverter();
            services.AddRabbitListenerEndpointRegistry();
            services.AddRabbitListenerEndpointRegistrar();

            services.AddSingleton(mockConnectionFactory.Object);
            services.AddRabbitListenerContainerFactory<TestDirectRabbitListenerContainerFactory>("rabbitListenerContainerFactory");
            services.AddRabbitAdmin();

            IQueue myQueue = QueueBuilder.Durable("myQueue").Build();
            IQueue anotherQueue = QueueBuilder.Durable("anotherQueue").Build();
            IQueue class1 = QueueBuilder.Durable("class1").Build();
            IQueue class2 = QueueBuilder.Durable("class2").Build();

            services.AddRabbitQueues(myQueue, anotherQueue, class1, class2);
            services.AddSingleton<RabbitListenersBean>();
            services.AddSingleton<ClassLevelListenersBean>();
            services.AddRabbitListeners(configurationRoot, typeof(RabbitListenersBean), typeof(ClassLevelListenersBean));

            ServiceProvider container = services.BuildServiceProvider(true);
            await container.GetRequiredService<IHostedService>().StartAsync(default);
            return container;
        }
    }

    public static class RabbitSampleConfig
    {
        internal static async Task<ServiceProvider> CreateAndStartServicesAsync(Type listenerBeanType = null)
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
            IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(configurationRoot);
            services.AddRabbitHostingServices();
            services.AddRabbitMessageHandlerMethodFactory();
            services.AddRabbitListenerAttributeProcessor();
            services.AddRabbitDefaultMessageConverter();
            services.AddRabbitListenerEndpointRegistry();
            services.AddRabbitListenerEndpointRegistrar();

            services.AddSingleton(mockConnectionFactory.Object);
            services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("rabbitListenerContainerFactory");
            services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("simpleFactory");
            services.AddRabbitAdmin("myAdmin");

            IQueue foo = QueueBuilder.Durable("foo").Build();
            foo.ShouldDeclare = false;
            foo.IgnoreDeclarationExceptions = true;
            foo.DeclaringAdmins.Add("myAdmin");

            var bar = (DirectExchange)ExchangeBuilder.DirectExchange("bar").IgnoreDeclarationExceptions().SuppressDeclaration().Admins("myAdmin").Build();

            IBinding binding = BindingBuilder.Bind(foo).To(bar).With("baz");
            binding.DeclaringAdmins.Add("myAdmin");
            binding.IgnoreDeclarationExceptions = true;
            binding.ShouldDeclare = false;

            services.AddRabbitQueue(foo);
            services.AddRabbitExchange(bar);
            services.AddRabbitBinding(binding);

            listenerBeanType ??= typeof(SampleBean);

            services.AddSingleton(listenerBeanType);
            services.AddSingleton<Listener>();

            services.AddRabbitListeners(configurationRoot, listenerBeanType, typeof(Listener));
            ServiceProvider container = services.BuildServiceProvider(true);
            await container.GetRequiredService<IHostedService>().StartAsync(default);
            return container;
        }

        public sealed class Listener
        {
            [RabbitListener("foo")]
            public void Handle(string foo)
            {
            }
        }
    }

    public static class RabbitFullConfig
    {
        public static async Task<ServiceProvider> CreateAndStartServicesAsync()
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
            IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();
            services.AddSingleton<IConfiguration>(configurationRoot);
            services.AddRabbitHostingServices();
            services.AddRabbitMessageHandlerMethodFactory();
            services.AddRabbitListenerAttributeProcessor();
            services.AddRabbitDefaultMessageConverter();
            services.AddRabbitListenerEndpointRegistry();
            services.AddRabbitListenerEndpointRegistrar();

            services.AddSingleton(mockConnectionFactory.Object);
            services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("simpleFactory");
            services.AddRabbitAdmin();

            IQueue queue1 = QueueBuilder.Durable("queue1").Build();
            IQueue queue2 = QueueBuilder.Durable("queue2").Build();
            services.AddRabbitQueues(queue1, queue2);

            services.AddSingleton<FullBean>();
            services.AddRabbitListeners<FullBean>();
            ServiceProvider container = services.BuildServiceProvider(true);
            await container.GetRequiredService<IHostedService>().StartAsync(default);
            return container;
        }
    }

    public static class RabbitFullConfigurableConfig
    {
        public static async Task<ServiceProvider> CreateAndStartServicesAsync()
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
            var configBuilder = new ConfigurationBuilder();

            configBuilder.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "rabbit:listener:id", "listener1" },
                { "rabbit:listener:containerFactory", "simpleFactory" },
                { "rabbit.listener.queue", "queue1" },
                { "rabbit.listener.priority", "34" },
                { "rabbit.listener.responseRoutingKey", "routing-123" },
                { "rabbit.listener.admin", "rabbitAdmin" }
            });

            IConfigurationRoot configurationRoot = configBuilder.Build();

            services.AddSingleton<IConfiguration>(configurationRoot);
            services.AddRabbitHostingServices();
            services.AddRabbitMessageHandlerMethodFactory();
            services.AddRabbitListenerAttributeProcessor();
            services.AddRabbitDefaultMessageConverter();
            services.AddRabbitListenerEndpointRegistry();
            services.AddRabbitListenerEndpointRegistrar();

            services.AddSingleton(mockConnectionFactory.Object);
            services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("simpleFactory");
            services.AddRabbitAdmin();

            IQueue queue1 = QueueBuilder.Durable("queue1").Build();
            IQueue queue2 = QueueBuilder.Durable("queue2").Build();
            services.AddRabbitQueues(queue1, queue2);

            services.AddSingleton<FullBean>();
            services.AddRabbitListeners<FullBean>();
            ServiceProvider container = services.BuildServiceProvider(true);
            await container.GetRequiredService<IHostedService>().StartAsync(default);
            return container;
        }
    }

    public static class RabbitDefaultContainerFactoryConfig
    {
        public static async Task<ServiceProvider> CreateAndStartServicesAsync(params Type[] listeners)
        {
            var mockConnection = new Mock<IConnectionFactory>();
            var services = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            IConfigurationRoot configurationRoot = configBuilder.Build();

            services.AddSingleton<IConfiguration>(configurationRoot);
            services.AddRabbitHostingServices();
            services.AddRabbitMessageHandlerMethodFactory();
            services.AddRabbitListenerAttributeProcessor();
            services.AddRabbitDefaultMessageConverter();
            services.AddRabbitListenerEndpointRegistrar();
            services.AddRabbitListenerEndpointRegistry();
            services.AddSingleton(mockConnection.Object);

            services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("rabbitListenerContainerFactory");
            services.AddRabbitAdmin();

            IQueue queue1 = QueueBuilder.Durable("myQueue").Build();
            services.AddRabbitQueues(queue1);

            foreach (Type listener in listeners)
            {
                services.AddSingleton(listener);
            }

            services.AddRabbitListeners(configurationRoot, listeners);
            ServiceProvider container = services.BuildServiceProvider(true);
            await container.GetRequiredService<IHostedService>().StartAsync(default);
            return container;
        }
    }

    public sealed class RabbitCustomConfig : IRabbitListenerConfigurer
    {
        private readonly IRabbitListenerEndpointRegistry _registry;
        private readonly IApplicationContext _context;

        public RabbitCustomConfig(IApplicationContext context, IRabbitListenerEndpointRegistry registry)
        {
            _registry = registry;
            _context = context;
        }

        public static async Task<ServiceProvider> CreateAndStartServicesAsync()
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
            var configBuilder = new ConfigurationBuilder();
            IConfigurationRoot configurationRoot = configBuilder.Build();

            services.AddSingleton<IConfiguration>(configurationRoot);
            services.AddRabbitHostingServices();
            services.AddRabbitMessageHandlerMethodFactory();
            services.AddRabbitListenerAttributeProcessor();
            services.AddRabbitDefaultMessageConverter();
            services.AddRabbitListenerEndpointRegistrar();

            services.AddSingleton(mockConnectionFactory.Object);
            services.AddSingleton<IRabbitListenerConfigurer, RabbitCustomConfig>();
            services.AddRabbitListenerEndpointRegistry<CustomRabbitListenerEndpointRegistry>();
            services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("rabbitListenerContainerFactory");
            services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("customFactory");
            services.AddRabbitAdmin();

            IQueue queue1 = QueueBuilder.Durable("myQueue").Build();
            services.AddRabbitQueues(queue1);

            services.AddRabbitListeners<CustomBean>();
            ServiceProvider container = services.BuildServiceProvider(true);
            await container.GetRequiredService<IHostedService>().StartAsync(default);
            return container;
        }

        public void ConfigureRabbitListeners(IRabbitListenerEndpointRegistrar registrar)
        {
            registrar.EndpointRegistry = _registry;

            var endpoint = new SimpleRabbitListenerEndpoint(_context)
            {
                Id = "myCustomEndpointId"
            };

            endpoint.SetQueueNames("myQueue");
            endpoint.MessageListener = new MessageListenerAdapter(_context);
            registrar.RegisterEndpoint(endpoint);
        }
    }

    public sealed class RabbitCustomContainerFactoryConfig : IRabbitListenerConfigurer
    {
        private readonly IApplicationContext _context;

        public RabbitCustomContainerFactoryConfig(IApplicationContext context)
        {
            _context = context;
        }

        public static async Task<ServiceProvider> CreateAndStartServicesAsync()
        {
            var mockConnection = new Mock<IConnectionFactory>();
            var services = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder();
            IConfigurationRoot configurationRoot = configBuilder.Build();

            services.AddSingleton<IConfiguration>(configurationRoot);
            services.AddRabbitHostingServices();
            services.AddRabbitMessageHandlerMethodFactory();
            services.AddRabbitListenerAttributeProcessor();
            services.AddRabbitDefaultMessageConverter();
            services.AddRabbitListenerEndpointRegistrar();

            services.AddSingleton(mockConnection.Object);
            services.AddSingleton<IRabbitListenerConfigurer, RabbitCustomContainerFactoryConfig>();
            services.AddRabbitListenerEndpointRegistry();
            services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("simpleFactory");
            services.AddRabbitAdmin();

            IQueue queue1 = QueueBuilder.Durable("myQueue").Build();
            services.AddRabbitQueues(queue1);

            services.AddRabbitListeners<DefaultBean>();
            ServiceProvider container = services.BuildServiceProvider(true);
            await container.GetRequiredService<IHostedService>().StartAsync(default);
            return container;
        }

        public void ConfigureRabbitListeners(IRabbitListenerEndpointRegistrar registrar)
        {
            registrar.ContainerFactory = _context.GetService<IRabbitListenerContainerFactory>("simpleFactory");
        }
    }

    private sealed class TestDirectRabbitListenerContainerFactory : DirectRabbitListenerContainerFactory
    {
        public TestDirectRabbitListenerContainerFactory(IApplicationContext applicationContext, IConnectionFactory connectionFactory,
            ILoggerFactory loggerFactory = null)
            : base(applicationContext, connectionFactory, loggerFactory)
        {
        }

        protected override DirectMessageListenerContainer CreateContainerInstance()
        {
            return new TestDirectMessageListenerContainer();
        }
    }

    public sealed class TestDirectMessageListenerContainer : DirectMessageListenerContainer
    {
        public override void Shutdown()
        {
            throw new Exception("Exception in Shutdown()");
        }
    }

    public sealed class SampleBean
    {
        [RabbitListener("nyQueue")]
        public void DefaultHandle(string foo)
        {
        }

        [RabbitListener("muQueue", ContainerFactory = "simpleFactory")]
        public void SimpleHandle(string msg)
        {
        }
    }

    public sealed class FullBean
    {
        [RabbitListener("queue1", "queue2", Id = "listener1", ContainerFactory = "simpleFactory", Exclusive = true, Priority = "34", Admin = "rabbitAdmin")]
        public void FullHandle(string msg)
        {
        }
    }

    public sealed class FullConfigurableBean
    {
        [RabbitListener("${rabbit:listener:queue}", "queue2", Id = "${rabbit:listener:id}", ContainerFactory = "${rabbit:listener:containerFactory}",
            Exclusive = true, Priority = "${rabbit: listener:priority}", Admin = "${rabbit:listener:admin}")]
        public void FullHandle(string msg)
        {
        }
    }

    public sealed class CustomBean
    {
        [RabbitListener("myQueue", Id = "listenerId", ContainerFactory = "customFactory")]
        public void CustomHandle(string msg)
        {
        }
    }

    public sealed class DefaultBean
    {
        [RabbitListener("myQueue")]
        public void HandleIt(string msg)
        {
        }
    }

    public sealed class InvalidPriorityBean
    {
        [RabbitListener("myQueue", Priority = "NotANumber")]
        public void CustomHandle(string msg)
        {
        }
    }

    public sealed class RabbitListenersBean
    {
        [RabbitListener("myQueue", Id = "first")]
        [RabbitListener("anotherQueue", Id = "second")]
        public void RepeatableHandle(string msg)
        {
        }
    }

    [RabbitListener("class1", Id = "third")]
    [RabbitListener("class2", Id = "fourth")]
    public sealed class ClassLevelListenersBean
    {
        [RabbitHandler]
        public void RepeatableHandle(string msg)
        {
        }
    }

    private class CustomRabbitListenerEndpointRegistry : RabbitListenerEndpointRegistry
    {
        public CustomRabbitListenerEndpointRegistry(IApplicationContext applicationContext, ILogger logger = null)
            : base(applicationContext, logger)
        {
        }
    }
}
