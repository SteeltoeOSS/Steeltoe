// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;
using Steeltoe.Messaging.RabbitMQ.Support;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Attributes
{
    [Trait("Category", "Integration")]
    public class EnableRabbitTest
    {
        [Fact]
        public async Task SampleConfiguration()
        {
            var services = await RabbitSampleConfig.CreateAndStartServices();
            var context = services.GetRequiredService<IApplicationContext>();
            TestSampleConfiguration(context, 2);
        }

        [Fact]
        public async Task FullConfiguration()
        {
            var services = await RabbitFullConfig.CreateAndStartServices();
            var context = services.GetRequiredService<IApplicationContext>();
            TestFullConfiguration(context);
        }

        [Fact]
        public async Task FullConfigurableConfiguration()
        {
            var services = await RabbitFullConfigurableConfig.CreateAndStartServices();
            var context = services.GetRequiredService<IApplicationContext>();
            TestFullConfiguration(context);
        }

        [Fact]
        public async Task NoRabbitAdminConfiguration()
        {
            var excep = await Assert.ThrowsAsync<InvalidOperationException>(() => RabbitSampleConfig.CreateAndStartServices(typeof(FullBean)));
            Assert.Contains("rabbitAdmin", excep.Message);
        }

        [Fact]
        public async Task CustomConfiguration()
        {
            var services = await RabbitCustomConfig.CreateAndStartServices();
            var context = services.GetRequiredService<IApplicationContext>();
            TestCustomConfiguration(context);
        }

        [Fact]
        public async Task ExplicitContainerFactory()
        {
            var services = await RabbitCustomContainerFactoryConfig.CreateAndStartServices();
            var context = services.GetRequiredService<IApplicationContext>();
            TestExplicitContainerFactoryConfiguration(context);
        }

        [Fact]
        public async Task DefaultContainerFactory()
        {
            var services = await RabbitDefaultContainerFactoryConfig.CreateAndStartServices(typeof(DefaultBean));
            var context = services.GetRequiredService<IApplicationContext>();
            TestDefaultContainerFactoryConfiguration(context);
        }

        [Fact]
        public Task RabbitHandlerMethodFactoryConfiguration()
        {
            // TODO:
            // var services = await RabbitHandlerMethodFactoryConfig.CreateAndStartServices();
            // var context = services.GetRequiredService<IApplicationContext>();
            // TestDefaultContainerFactoryConfiguration(context);
            return Task.CompletedTask;
        }

        [Fact]
        public async Task RabbitListeners()
        {
            var services = await RabbitDefaultContainerFactoryConfig.CreateAndStartServices(typeof(RabbitListenersBean), typeof(ClassLevelListenersBean));
            var context = services.GetRequiredService<IApplicationContext>();
            TestRabbitListenerRepeatable(context);
        }

        [Fact]
        public async Task UnknownFactory()
        {
            var excep = await Assert.ThrowsAsync<InvalidOperationException>(() => RabbitSampleConfig.CreateAndStartServices(typeof(CustomBean)));
            Assert.Contains("customFactory", excep.Message);
        }

        [Fact]
        public async Task InvalidPriorityConfiguration()
        {
            var excep = await Assert.ThrowsAsync<InvalidOperationException>(() => RabbitSampleConfig.CreateAndStartServices(typeof(InvalidPriorityBean)));
            Assert.Contains("NotANumber", excep.Message);
        }

        [Fact]
        public async Task TestProperShutdownOnException()
        {
            var services = await TestProperShutdownOnExceptionConfig.CreateAndStartServices();
            var context = services.GetRequiredService<IApplicationContext>();
            var listenerEndpointRegistry = context.GetService<IRabbitListenerEndpointRegistry>();
            services.Dispose();
            foreach (var messageListenerContainer in listenerEndpointRegistry.GetListenerContainers())
            {
                Assert.False(messageListenerContainer.IsRunning);
            }
        }

        private void TestRabbitListenerRepeatable(IApplicationContext context)
        {
            var defaultFactory = context.GetService<IRabbitListenerContainerFactory>("rabbitListenerContainerFactory") as RabbitListenerContainerTestFactory;
            Assert.Equal(4, defaultFactory.GetListenerContainers().Count);

            var first = defaultFactory.GetListenerContainer("first").Endpoint as AbstractRabbitListenerEndpoint;
            Assert.Equal("first", first.Id);
            Assert.Single(first.QueueNames);
            Assert.Equal("myQueue", first.QueueNames[0]);

            var second = defaultFactory.GetListenerContainer("second").Endpoint as AbstractRabbitListenerEndpoint;
            Assert.Equal("second", second.Id);
            Assert.Single(second.QueueNames);
            Assert.Equal("anotherQueue", second.QueueNames[0]);

            var third = defaultFactory.GetListenerContainer("third").Endpoint as AbstractRabbitListenerEndpoint;
            Assert.Equal("third", third.Id);
            Assert.Single(third.QueueNames);
            Assert.Equal("class1", third.QueueNames[0]);

            var fourth = defaultFactory.GetListenerContainer("fourth").Endpoint as AbstractRabbitListenerEndpoint;
            Assert.Equal("fourth", fourth.Id);
            Assert.Single(fourth.QueueNames);
            Assert.Equal("class2", fourth.QueueNames[0]);
        }

        private void TestSampleConfiguration(IApplicationContext context, int expectedDefaultContainers)
        {
            var defaultFactory = context.GetService<IRabbitListenerContainerFactory>("rabbitListenerContainerFactory") as RabbitListenerContainerTestFactory;
            var simpleFactory = context.GetService<IRabbitListenerContainerFactory>("simpleFactory") as RabbitListenerContainerTestFactory;

            Assert.Equal(expectedDefaultContainers, defaultFactory.ListenerContainers.Count);
            Assert.Single(simpleFactory.ListenerContainers);

            var queues = context.GetRabbitQueues();
            foreach (var queue in queues)
            {
                Assert.True(queue.IgnoreDeclarationExceptions);
                Assert.False(queue.ShouldDeclare);
                CheckAdmins(queue.DeclaringAdmins);
            }

            var exchanges = context.GetRabbitExchanges();
            foreach (var exchange in exchanges)
            {
                Assert.True(exchange.IgnoreDeclarationExceptions);
                Assert.False(exchange.ShouldDeclare);
                CheckAdmins(exchange.DeclaringAdmins);
            }

            var bindings = context.GetRabbitBindings();
            foreach (var binding in bindings)
            {
                Assert.True(binding.IgnoreDeclarationExceptions);
                Assert.False(binding.ShouldDeclare);
                CheckAdmins(binding.DeclaringAdmins);
            }
        }

        private void TestFullConfiguration(IApplicationContext context)
        {
            var simpleFactory = context.GetService<IRabbitListenerContainerFactory>("simpleFactory") as RabbitListenerContainerTestFactory;
            Assert.Single(simpleFactory.ListenerContainers);

            var testContainer = simpleFactory.GetListenerContainers()[0];
            var endpoint = testContainer.Endpoint as AbstractRabbitListenerEndpoint;
            Assert.Equal("listener1", endpoint.Id);
            AssertQueues(endpoint, "queue1", "queue2");
            Assert.Empty(endpoint.Queues);
            Assert.True(endpoint.Exclusive);
            Assert.Equal(34, endpoint.Priority);
            var admin = context.GetRabbitAdmin();
            Assert.Same(endpoint.Admin, admin);

            var container = new DirectMessageListenerContainer(context);
            endpoint.SetupListenerContainer(container);
            var listener = container.MessageListener as MessagingMessageListenerAdapter;
            var accessor = new RabbitHeaderAccessor
            {
                ContentType = MessageHeaders.CONTENT_TYPE_TEXT_PLAIN
            };
            var message = Message.Create(Encoding.UTF8.GetBytes("Hello"), accessor.MessageHeaders);
            var mockChannel = new Mock<RC.IModel>();

            listener.OnMessage(message, mockChannel.Object);
        }

        private void TestCustomConfiguration(IApplicationContext context)
        {
            var customFactory = context.GetService<IRabbitListenerContainerFactory>("customFactory") as RabbitListenerContainerTestFactory;
            var defaultFactory = context.GetService<IRabbitListenerContainerFactory>("rabbitListenerContainerFactory") as RabbitListenerContainerTestFactory;
            Assert.Single(defaultFactory.ListenerContainers);
            Assert.Single(customFactory.ListenerContainers);
            var testContainer = defaultFactory.GetListenerContainers()[0];
            var endpoint = testContainer.Endpoint as IRabbitListenerEndpoint;
            Assert.IsType<SimpleRabbitListenerEndpoint>(endpoint);
            var simpEndpoint = endpoint as SimpleRabbitListenerEndpoint;
            Assert.IsType<MessageListenerAdapter>(simpEndpoint.MessageListener);
            var customRegistry = context.GetService<IRabbitListenerEndpointRegistry>();
            Assert.IsType<CustomRabbitListenerEndpointRegistry>(customRegistry);
            Assert.Equal(2, customRegistry.GetListenerContainerIds().Count);
            Assert.Equal(2, customRegistry.GetListenerContainers().Count);
            Assert.NotNull(customRegistry.GetListenerContainer("listenerId"));
            Assert.NotNull(customRegistry.GetListenerContainer("myCustomEndpointId"));
        }

        private void TestDefaultContainerFactoryConfiguration(IApplicationContext context)
        {
            var defaultFactory = context.GetService<IRabbitListenerContainerFactory>("rabbitListenerContainerFactory") as RabbitListenerContainerTestFactory;
            Assert.Single(defaultFactory.GetListenerContainers());
        }

        private void TestExplicitContainerFactoryConfiguration(IApplicationContext context)
        {
            var defaultFactory = context.GetService<IRabbitListenerContainerFactory>("simpleFactory") as RabbitListenerContainerTestFactory;
            Assert.Single(defaultFactory.GetListenerContainers());
        }

        private void AssertQueues(AbstractRabbitListenerEndpoint endpoint, params string[] queues)
        {
            var actualQueues = endpoint.QueueNames;
            foreach (var expectedQueue in queues)
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
            public static async Task<ServiceProvider> CreateAndStartServices()
            {
                var mockConnectionFactory = new Mock<IConnectionFactory>();
                var mockConnection = new Mock<IConnection>();
                var mockChannel = new Mock<RC.IModel>();
                mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
                mockConnection.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(mockChannel.Object);
                mockChannel.Setup((c) => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
                mockConnection.Setup((c) => c.IsOpen).Returns(true);
                mockChannel.Setup((c) => c.IsOpen).Returns(true);
                var queueName = new AtomicReference<string>();
                mockChannel.Setup(c => c.QueueDeclarePassive(It.IsAny<string>())).Returns(() => new RC.QueueDeclareOk(queueName.Value, 0, 0))
                    .Callback<string>((name) => queueName.Value = name);

                var services = new ServiceCollection();
                var config = new ConfigurationBuilder().Build();
                services.AddSingleton<IConfiguration>(config);
                services.AddRabbitHostingServices();
                services.AddRabbitMessageHandlerMethodFactory();
                services.AddRabbitListenerAttributeProcessor();
                services.AddRabbitDefaultMessageConverter();
                services.AddRabbitListenerEndpointRegistry();
                services.AddRabbitListenerEndpointRegistrar();

                services.AddSingleton<IConnectionFactory>(mockConnectionFactory.Object);
                services.AddRabbitListenerContainerFactory<TestDirectRabbitListenerContainerFactory>("rabbitListenerContainerFactory");
                services.AddRabbitAdmin();

                var myQueue = QueueBuilder.Durable("myQueue").Build();
                var anotherQueue = QueueBuilder.Durable("anotherQueue").Build();
                var class1 = QueueBuilder.Durable("class1").Build();
                var class2 = QueueBuilder.Durable("class2").Build();

                services.AddRabbitQueues(myQueue, anotherQueue, class1, class2);
                services.AddSingleton<RabbitListenersBean>();
                services.AddSingleton<ClassLevelListenersBean>();
                services.AddRabbitListeners(config, typeof(RabbitListenersBean), typeof(ClassLevelListenersBean));

                var container = services.BuildServiceProvider();
                await container.GetRequiredService<IHostedService>().StartAsync(default);
                return container;
            }
        }

        public static class RabbitSampleConfig
        {
            public static async Task<ServiceProvider> CreateAndStartServices(Type listenerBeanType = null)
            {
                var mockConnectionFactory = new Mock<IConnectionFactory>();
                var mockConnection = new Mock<IConnection>();
                var mockChannel = new Mock<RC.IModel>();
                mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
                mockConnection.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(mockChannel.Object);
                mockChannel.Setup((c) => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
                mockConnection.Setup((c) => c.IsOpen).Returns(true);
                mockChannel.Setup((c) => c.IsOpen).Returns(true);
                var queueName = new AtomicReference<string>();
                mockChannel.Setup(c => c.QueueDeclarePassive(It.IsAny<string>())).Returns(() => new RC.QueueDeclareOk(queueName.Value, 0, 0))
                    .Callback<string>((name) => queueName.Value = name);

                var services = new ServiceCollection();
                var config = new ConfigurationBuilder().Build();
                services.AddSingleton<IConfiguration>(config);
                services.AddRabbitHostingServices();
                services.AddRabbitMessageHandlerMethodFactory();
                services.AddRabbitListenerAttributeProcessor();
                services.AddRabbitDefaultMessageConverter();
                services.AddRabbitListenerEndpointRegistry();
                services.AddRabbitListenerEndpointRegistrar();

                services.AddSingleton<IConnectionFactory>(mockConnectionFactory.Object);
                services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("rabbitListenerContainerFactory");
                services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("simpleFactory");
                services.AddRabbitAdmin("myAdmin");

                var foo = QueueBuilder.Durable("foo").Build();
                foo.ShouldDeclare = false;
                foo.IgnoreDeclarationExceptions = true;
                foo.DeclaringAdmins.Add("myAdmin");

                var bar = (DirectExchange)ExchangeBuilder
                    .DirectExchange("bar")
                    .IgnoreDeclarationExceptions()
                    .SuppressDeclaration()
                    .Admins("myAdmin")
                    .Build();

                var binding = BindingBuilder.Bind(foo).To(bar).With("baz");
                binding.DeclaringAdmins.Add("myAdmin");
                binding.IgnoreDeclarationExceptions = true;
                binding.ShouldDeclare = false;

                services.AddRabbitQueue(foo);
                services.AddRabbitExchange(bar);
                services.AddRabbitBinding(binding);

                listenerBeanType ??= typeof(SampleBean);

                services.AddSingleton(listenerBeanType);
                services.AddSingleton<Listener>();

                services.AddRabbitListeners(config, listenerBeanType, typeof(Listener));
                var container = services.BuildServiceProvider();
                await container.GetRequiredService<IHostedService>().StartAsync(default);
                return container;
            }

            public class Listener
            {
                [RabbitListener("foo")]
                public void Handle(string foo)
                {
                }
            }
        }

        public static class RabbitFullConfig
        {
            public static async Task<ServiceProvider> CreateAndStartServices()
            {
                var mockConnectionFactory = new Mock<IConnectionFactory>();
                var mockConnection = new Mock<IConnection>();
                var mockChannel = new Mock<RC.IModel>();
                mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
                mockConnection.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(mockChannel.Object);
                mockChannel.Setup((c) => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
                mockConnection.Setup((c) => c.IsOpen).Returns(true);
                mockChannel.Setup((c) => c.IsOpen).Returns(true);
                var queueName = new AtomicReference<string>();
                mockChannel.Setup(c => c.QueueDeclarePassive(It.IsAny<string>())).Returns(() => new RC.QueueDeclareOk(queueName.Value, 0, 0))
                    .Callback<string>((name) => queueName.Value = name);

                var services = new ServiceCollection();
                var config = new ConfigurationBuilder().Build();
                services.AddSingleton<IConfiguration>(config);
                services.AddRabbitHostingServices();
                services.AddRabbitMessageHandlerMethodFactory();
                services.AddRabbitListenerAttributeProcessor();
                services.AddRabbitDefaultMessageConverter();
                services.AddRabbitListenerEndpointRegistry();
                services.AddRabbitListenerEndpointRegistrar();

                services.AddSingleton<IConnectionFactory>(mockConnectionFactory.Object);
                services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("simpleFactory");
                services.AddRabbitAdmin();

                var queue1 = QueueBuilder.Durable("queue1").Build();
                var queue2 = QueueBuilder.Durable("queue2").Build();
                services.AddRabbitQueues(queue1, queue2);

                services.AddSingleton<FullBean>();
                services.AddRabbitListeners<FullBean>();
                var container = services.BuildServiceProvider();
                await container.GetRequiredService<IHostedService>().StartAsync(default);
                return container;
            }
        }

        public static class RabbitFullConfigurableConfig
        {
            public static async Task<ServiceProvider> CreateAndStartServices()
            {
                var mockConnectionFactory = new Mock<IConnectionFactory>();
                var mockConnection = new Mock<IConnection>();
                var mockChannel = new Mock<RC.IModel>();
                mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
                mockConnection.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(mockChannel.Object);
                mockChannel.Setup((c) => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
                mockConnection.Setup((c) => c.IsOpen).Returns(true);
                mockChannel.Setup((c) => c.IsOpen).Returns(true);
                var queueName = new AtomicReference<string>();
                mockChannel.Setup(c => c.QueueDeclarePassive(It.IsAny<string>())).Returns(() => new RC.QueueDeclareOk(queueName.Value, 0, 0))
                    .Callback<string>((name) => queueName.Value = name);

                var services = new ServiceCollection();
                var configBuilder = new ConfigurationBuilder();
                configBuilder.AddInMemoryCollection(new Dictionary<string, string>()
                {
                    { "rabbit:listener:id", "listener1" },
                    { "rabbit:listener:containerFactory", "simpleFactory" },
                    { "rabbit.listener.queue", "queue1" },
                    { "rabbit.listener.priority", "34" },
                    { "rabbit.listener.responseRoutingKey", "routing-123" },
                    { "rabbit.listener.admin", "rabbitAdmin" },
                });
                var config = configBuilder.Build();

                services.AddSingleton<IConfiguration>(config);
                services.AddRabbitHostingServices();
                services.AddRabbitMessageHandlerMethodFactory();
                services.AddRabbitListenerAttributeProcessor();
                services.AddRabbitDefaultMessageConverter();
                services.AddRabbitListenerEndpointRegistry();
                services.AddRabbitListenerEndpointRegistrar();

                services.AddSingleton<IConnectionFactory>(mockConnectionFactory.Object);
                services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("simpleFactory");
                services.AddRabbitAdmin();

                var queue1 = QueueBuilder.Durable("queue1").Build();
                var queue2 = QueueBuilder.Durable("queue2").Build();
                services.AddRabbitQueues(queue1, queue2);

                services.AddSingleton<FullBean>();
                services.AddRabbitListeners<FullBean>();
                var container = services.BuildServiceProvider();
                await container.GetRequiredService<IHostedService>().StartAsync(default);
                return container;
            }
        }

        public static class RabbitDefaultContainerFactoryConfig
        {
            public static async Task<ServiceProvider> CreateAndStartServices(params Type[] listeners)
            {
                var mockConnection = new Mock<IConnectionFactory>();
                var services = new ServiceCollection();
                var configBuilder = new ConfigurationBuilder();
                var config = configBuilder.Build();

                services.AddSingleton<IConfiguration>(config);
                services.AddRabbitHostingServices();
                services.AddRabbitMessageHandlerMethodFactory();
                services.AddRabbitListenerAttributeProcessor();
                services.AddRabbitDefaultMessageConverter();
                services.AddRabbitListenerEndpointRegistrar();
                services.AddRabbitListenerEndpointRegistry();
                services.AddSingleton<IConnectionFactory>(mockConnection.Object);

                services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("rabbitListenerContainerFactory");
                services.AddRabbitAdmin();

                var queue1 = QueueBuilder.Durable("myQueue").Build();
                services.AddRabbitQueues(queue1);

                foreach (var listener in listeners)
                {
                    services.AddSingleton(listener);
                }

                services.AddRabbitListeners(config, listeners);
                var container = services.BuildServiceProvider();
                await container.GetRequiredService<IHostedService>().StartAsync(default);
                return container;
            }
        }

        public class RabbitCustomConfig : IRabbitListenerConfigurer
        {
            private readonly IRabbitListenerEndpointRegistry _registry;
            private readonly IApplicationContext _context;

            public RabbitCustomConfig(IApplicationContext context, IRabbitListenerEndpointRegistry registry)
            {
                _registry = registry;
                _context = context;
            }

            public static async Task<ServiceProvider> CreateAndStartServices()
            {
                var mockConnectionFactory = new Mock<IConnectionFactory>();
                var mockConnection = new Mock<IConnection>();
                var mockChannel = new Mock<RC.IModel>();
                mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(mockConnection.Object);
                mockConnection.Setup(c => c.CreateChannel(It.IsAny<bool>())).Returns(mockChannel.Object);
                mockChannel.Setup((c) => c.CreateBasicProperties()).Returns(new MockRabbitBasicProperties());
                mockConnection.Setup((c) => c.IsOpen).Returns(true);
                mockChannel.Setup((c) => c.IsOpen).Returns(true);
                var queueName = new AtomicReference<string>();
                mockChannel.Setup(c => c.QueueDeclarePassive(It.IsAny<string>())).Returns(() => new RC.QueueDeclareOk(queueName.Value, 0, 0))
                    .Callback<string>((name) => queueName.Value = name);

                var services = new ServiceCollection();
                var configBuilder = new ConfigurationBuilder();
                var config = configBuilder.Build();

                services.AddSingleton<IConfiguration>(config);
                services.AddRabbitHostingServices();
                services.AddRabbitMessageHandlerMethodFactory();
                services.AddRabbitListenerAttributeProcessor();
                services.AddRabbitDefaultMessageConverter();
                services.AddRabbitListenerEndpointRegistrar();

                services.AddSingleton<IConnectionFactory>(mockConnectionFactory.Object);
                services.AddSingleton<IRabbitListenerConfigurer, RabbitCustomConfig>();
                services.AddRabbitListenerEndpointRegistry<CustomRabbitListenerEndpointRegistry>();
                services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("rabbitListenerContainerFactory");
                services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("customFactory");
                services.AddRabbitAdmin();

                var queue1 = QueueBuilder.Durable("myQueue").Build();
                services.AddRabbitQueues(queue1);

                services.AddRabbitListeners<CustomBean>();
                var container = services.BuildServiceProvider();
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

        public class RabbitCustomContainerFactoryConfig : IRabbitListenerConfigurer
        {
            private readonly IApplicationContext _context;

            public RabbitCustomContainerFactoryConfig(IApplicationContext context)
            {
                _context = context;
            }

            public static async Task<ServiceProvider> CreateAndStartServices()
            {
                var mockConnection = new Mock<IConnectionFactory>();
                var services = new ServiceCollection();
                var configBuilder = new ConfigurationBuilder();
                var config = configBuilder.Build();

                services.AddSingleton<IConfiguration>(config);
                services.AddRabbitHostingServices();
                services.AddRabbitMessageHandlerMethodFactory();
                services.AddRabbitListenerAttributeProcessor();
                services.AddRabbitDefaultMessageConverter();
                services.AddRabbitListenerEndpointRegistrar();

                services.AddSingleton<IConnectionFactory>(mockConnection.Object);
                services.AddSingleton<IRabbitListenerConfigurer, RabbitCustomContainerFactoryConfig>();
                services.AddRabbitListenerEndpointRegistry();
                services.AddRabbitListenerContainerFactory<RabbitListenerContainerTestFactory>("simpleFactory");
                services.AddRabbitAdmin();

                var queue1 = QueueBuilder.Durable("myQueue").Build();
                services.AddRabbitQueues(queue1);

                services.AddRabbitListeners<DefaultBean>();
                var container = services.BuildServiceProvider();
                await container.GetRequiredService<IHostedService>().StartAsync(default);
                return container;
            }

            public void ConfigureRabbitListeners(IRabbitListenerEndpointRegistrar registrar)
            {
                registrar.ContainerFactory = _context.GetService<IRabbitListenerContainerFactory>("simpleFactory");
            }
        }

        public class TestDirectRabbitListenerContainerFactory : DirectRabbitListenerContainerFactory
        {
            public TestDirectRabbitListenerContainerFactory(IApplicationContext applicationContext, IConnectionFactory connectionFactory, ILoggerFactory loggerFactory = null)
                : base(applicationContext, connectionFactory, loggerFactory)
            {
            }

            protected override DirectMessageListenerContainer CreateContainerInstance()
            {
                return new TestDirectMessageListenerContainer();
            }
        }

        public class TestDirectMessageListenerContainer : DirectMessageListenerContainer
        {
            public override void Shutdown()
            {
                throw new Exception("Exception in Shutdown()");
            }
        }

        public class SampleBean
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

        public class FullBean
        {
            [RabbitListener(
                "queue1",
                "queue2",
                Id = "listener1",
                ContainerFactory = "simpleFactory",
                Exclusive = true,
                Priority = "34",
                Admin = "rabbitAdmin")]
            public void FullHandle(string msg)
            {
            }
        }

        public class FullConfigurableBean
        {
            [RabbitListener(
                "${rabbit:listener:queue}",
                "queue2",
                Id = "${rabbit:listener:id}",
                ContainerFactory = "${rabbit:listener:containerFactory}",
                Exclusive = true,
                Priority = "${rabbit: listener:priority}",
                Admin = "${rabbit:listener:admin}")]
            public void FullHandle(string msg)
            {
            }
        }

        public class CustomBean
        {
            [RabbitListener("myQueue", Id = "listenerId", ContainerFactory = "customFactory")]
            public void CustomHandle(string msg)
            {
            }
        }

        public class DefaultBean
        {
            [RabbitListener("myQueue")]
            public void HandleIt(string msg)
            {
            }
        }

        public class InvalidPriorityBean
        {
            [RabbitListener("myQueue", Priority = "NotANumber")]
            public void CustomHandle(string msg)
            {
            }
        }

        public class RabbitListenersBean
        {
            [RabbitListener("myQueue", Id = "first")]
            [RabbitListener("anotherQueue", Id = "second")]
            public void RepeatableHandle(string msg)
            {
            }
        }

        [RabbitListener("class1", Id = "third")]
        [RabbitListener("class2", Id = "fourth")]
        public class ClassLevelListenersBean
        {
            [RabbitHandler]
            public void RepeatableHandle(string msg)
            {
            }
        }

        protected class CustomRabbitListenerEndpointRegistry : RabbitListenerEndpointRegistry
        {
            public CustomRabbitListenerEndpointRegistry(IApplicationContext applicationContext, ILogger logger = null)
                : base(applicationContext, logger)
            {
            }
        }
    }
}
