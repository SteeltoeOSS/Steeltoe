// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using System;
using System.Collections.Generic;
using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Core.RabbitAdminDeclarationTest;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Core;

public class RabbitAdminDeclarationTest : IClassFixture<RabbitAdminDeclarationTestStartupFixture>
{
    private readonly RabbitAdminDeclarationTestStartupFixture _fixture;

    public RabbitAdminDeclarationTest(RabbitAdminDeclarationTestStartupFixture fix)
    {
        _fixture = fix;
    }

    [Fact]
    public void TestUnconditional()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddLogging(b =>
        {
            b.AddDebug();
            b.AddConsole();
        });

        services.AddSingleton<IConfiguration>(config);
        services.AddRabbitHostingServices();

        var cf = new Mock<IConnectionFactory>();
        var conn = new Mock<IConnection>();
        var channel = new Mock<RC.IModel>();
        cf.Setup(f => f.CreateConnection()).Returns(conn.Object);
        cf.SetupGet(f => f.ServiceName).Returns(CachingConnectionFactory.DEFAULT_SERVICE_NAME);
        conn.Setup(c => c.CreateChannel(false)).Returns(channel.Object);
        conn.Setup(c => c.IsOpen).Returns(true);
        channel.Setup(c => c.IsOpen).Returns(true);
        channel.Setup(c => c.QueueDeclare("foo", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()))
            .Returns(() => new RC.QueueDeclareOk("foo", 0, 0));
        var listener = new AtomicReference<IConnectionListener>();
        cf.Setup(f => f.AddConnectionListener(It.IsAny<IConnectionListener>()))
            .Callback<IConnectionListener>(l => listener.Value = l);
        var queue = new Queue("foo");
        services.AddRabbitQueue(queue);
        var exchange = new DirectExchange("bar");
        services.AddRabbitExchange(exchange);
        var binding = new Binding("baz", "foo", Binding.DestinationType.QUEUE, "bar", "foo", null);
        services.AddRabbitBinding(binding);
        var provider = services.BuildServiceProvider();
        var context = provider.GetApplicationContext();
        _ = new RabbitAdmin(context, cf.Object);
        Assert.NotNull(listener.Value);
        listener.Value.OnCreate(conn.Object);
        channel.Verify(c => c.QueueDeclare("foo", true, false, false, It.IsAny<IDictionary<string, object>>()));
        channel.Verify(c => c.ExchangeDeclare("bar", "direct", true, false, It.IsAny<IDictionary<string, object>>()));
        channel.Verify(c => c.QueueBind("foo", "bar", "foo", It.IsAny<IDictionary<string, object>>()));
    }

    [Fact]
    public void TestNoDeclareWithCachedConnections()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddLogging(b =>
        {
            b.AddDebug();
            b.AddConsole();
        });

        services.AddSingleton<IConfiguration>(config);
        services.AddRabbitHostingServices();

        var mockConnectionFactory = new Mock<RC.IConnectionFactory>();

        var mockConnections = new List<RC.IConnection>();
        var mockChannels = new List<RC.IModel>();
        var connectionNumber = new AtomicInteger(-1);
        var channelNumber = new AtomicInteger(-1);

        mockConnectionFactory.Setup(f => f.CreateConnection(It.IsAny<string>()))
            .Callback(() =>
            {
                var connection = new Mock<RC.IConnection>();
                var connectionNum = connectionNumber.IncrementAndGet();
                mockConnections.Add(connection.Object);
                connection.Setup(c => c.IsOpen).Returns(true);
                connection.Setup(c => c.ToString()).Returns($"mockConnection{connectionNum}");
                connection.Setup(c => c.CreateModel())
                    .Callback(() =>
                    {
                        var channel = new Mock<RC.IModel>();
                        mockChannels.Add(channel.Object);
                        channel.Setup(c => c.IsOpen).Returns(true);
                        var channelNum = channelNumber.IncrementAndGet();
                        channel.Setup(c => c.ToString()).Returns($"mockChannel{channelNum}");
                    })
                    .Returns(() => mockChannels[channelNumber.Value]);
            })
            .Returns(() => mockConnections[connectionNumber.Value]);

        var ccf = new CachingConnectionFactory(mockConnectionFactory.Object, false, CachingConnectionFactory.CachingMode.CONNECTION);
        var queue = new Queue("foo");
        services.AddRabbitQueue(queue);
        var provider = services.BuildServiceProvider();
        var context = provider.GetApplicationContext();
        _ = new RabbitAdmin(context, ccf);
        ccf.CreateConnection().Close();
        ccf.Destroy();
        Assert.Empty(mockChannels);
    }

    [Fact]
    public void TestUnconditionalWithExplicitFactory()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddLogging(b =>
        {
            b.AddDebug();
            b.AddConsole();
        });

        services.AddSingleton<IConfiguration>(config);
        services.AddRabbitHostingServices();

        var cf = new Mock<IConnectionFactory>();
        var conn = new Mock<IConnection>();
        var channel = new Mock<RC.IModel>();
        cf.Setup(f => f.CreateConnection()).Returns(conn.Object);
        cf.SetupGet(f => f.ServiceName).Returns(CachingConnectionFactory.DEFAULT_SERVICE_NAME);
        conn.Setup(c => c.CreateChannel(false)).Returns(channel.Object);
        conn.Setup(c => c.IsOpen).Returns(true);
        channel.Setup(c => c.IsOpen).Returns(true);
        channel.Setup(c => c.QueueDeclare("foo", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()))
            .Returns(() => new RC.QueueDeclareOk("foo", 0, 0));
        var listener = new AtomicReference<IConnectionListener>();
        cf.Setup(f => f.AddConnectionListener(It.IsAny<IConnectionListener>()))
            .Callback<IConnectionListener>(l => listener.Value = l);
        var queue = new Queue("foo");
        services.AddRabbitQueue(queue);
        var exchange = new DirectExchange("bar");
        services.AddRabbitExchange(exchange);
        var binding = new Binding("baz", "foo", Binding.DestinationType.QUEUE, "bar", "foo", null);
        services.AddRabbitBinding(binding);
        var provider = services.BuildServiceProvider();
        var context = provider.GetApplicationContext();
        var admin = new RabbitAdmin(context, cf.Object);

        queue.SetAdminsThatShouldDeclare(admin);
        exchange.SetAdminsThatShouldDeclare(admin);
        binding.SetAdminsThatShouldDeclare(admin);

        Assert.NotNull(listener.Value);
        listener.Value.OnCreate(conn.Object);
        channel.Verify(c => c.QueueDeclare("foo", true, false, false, It.IsAny<IDictionary<string, object>>()));
        channel.Verify(c => c.ExchangeDeclare("bar", "direct", true, false, It.IsAny<IDictionary<string, object>>()));
        channel.Verify(c => c.QueueBind("foo", "bar", "foo", It.IsAny<IDictionary<string, object>>()));
    }

    [Fact]
    public void TestSkipBecauseDifferentFactory()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddLogging(b =>
        {
            b.AddDebug();
            b.AddConsole();
        });

        services.AddSingleton<IConfiguration>(config);
        services.AddRabbitHostingServices();

        var cf = new Mock<IConnectionFactory>();
        var conn = new Mock<IConnection>();
        var channel = new Mock<RC.IModel>();
        cf.Setup(f => f.CreateConnection()).Returns(conn.Object);
        cf.SetupGet(f => f.ServiceName).Returns(CachingConnectionFactory.DEFAULT_SERVICE_NAME);
        conn.Setup(c => c.CreateChannel(false)).Returns(channel.Object);
        conn.Setup(c => c.IsOpen).Returns(true);
        channel.Setup(c => c.IsOpen).Returns(true);
        channel.Setup(c => c.QueueDeclare("foo", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()))
            .Returns(() => new RC.QueueDeclareOk("foo", 0, 0));
        var listener = new AtomicReference<IConnectionListener>();
        cf.Setup(f => f.AddConnectionListener(It.IsAny<IConnectionListener>()))
            .Callback<IConnectionListener>(l => listener.Value = l);

        var queue = new Queue("foo");
        services.AddRabbitQueue(queue);
        var exchange = new DirectExchange("bar");
        services.AddRabbitExchange(exchange);
        var binding = new Binding("baz", "foo", Binding.DestinationType.QUEUE, "bar", "foo", null);
        services.AddRabbitBinding(binding);
        var provider = services.BuildServiceProvider();
        var context = provider.GetApplicationContext();

        _ = new RabbitAdmin(context, cf.Object);
        var other = new RabbitAdmin(cf.Object);

        queue.SetAdminsThatShouldDeclare(other);
        exchange.SetAdminsThatShouldDeclare(other);
        binding.SetAdminsThatShouldDeclare(other);

        Assert.NotNull(listener.Value);
        listener.Value.OnCreate(conn.Object);
        channel.Verify(c => c.QueueDeclare("foo", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
        channel.Verify(c => c.ExchangeDeclare("bar", "direct", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
        channel.Verify(c => c.QueueBind("foo", "bar", "foo", It.IsAny<IDictionary<string, object>>()), Times.Never);
    }

    [Fact]
    public void TestSkipBecauseShouldntDeclare()
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddLogging(b =>
        {
            b.AddDebug();
            b.AddConsole();
        });

        services.AddSingleton<IConfiguration>(config);
        services.AddRabbitHostingServices();

        var cf = new Mock<IConnectionFactory>();
        var conn = new Mock<IConnection>();
        var channel = new Mock<RC.IModel>();
        cf.Setup(f => f.CreateConnection()).Returns(conn.Object);
        cf.SetupGet(f => f.ServiceName).Returns(CachingConnectionFactory.DEFAULT_SERVICE_NAME);
        conn.Setup(c => c.CreateChannel(false)).Returns(channel.Object);
        conn.Setup(c => c.IsOpen).Returns(true);
        channel.Setup(c => c.IsOpen).Returns(true);
        channel.Setup(c => c.QueueDeclare("foo", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()))
            .Returns(() => new RC.QueueDeclareOk("foo", 0, 0));
        var listener = new AtomicReference<IConnectionListener>();
        cf.Setup(f => f.AddConnectionListener(It.IsAny<IConnectionListener>()))
            .Callback<IConnectionListener>(l => listener.Value = l);

        var queue = new Queue("foo")
        {
            ShouldDeclare = false
        };
        services.AddRabbitQueue(queue);
        var exchange = new DirectExchange("bar")
        {
            ShouldDeclare = false
        };
        services.AddRabbitExchange(exchange);
        var binding = new Binding("baz", "foo", Binding.DestinationType.QUEUE, "bar", "foo", null)
        {
            ShouldDeclare = false
        };
        services.AddRabbitBinding(binding);
        var provider = services.BuildServiceProvider();
        var context = provider.GetApplicationContext();

        _ = new RabbitAdmin(context, cf.Object);

        Assert.NotNull(listener.Value);
        listener.Value.OnCreate(conn.Object);
        channel.Verify(c => c.QueueDeclare("foo", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
        channel.Verify(c => c.ExchangeDeclare("bar", "direct", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
        channel.Verify(c => c.QueueBind("foo", "bar", "foo", It.IsAny<IDictionary<string, object>>()), Times.Never);
    }

    [Fact]
    public void TestContainerConfig()
    {
        _fixture.Listener1.Value.OnCreate(_fixture.Conn1.Object);
        _fixture.Channel1.Verify(c => c.QueueDeclare("foo", true, false, false, It.IsAny<IDictionary<string, object>>()));
        _fixture.Channel1.Verify(c => c.QueueDeclare("baz", true, false, false, It.IsAny<IDictionary<string, object>>()), Times.Never);
        _fixture.Channel1.Verify(c => c.QueueDeclare("qux", true, false, false, It.IsAny<IDictionary<string, object>>()));
        _fixture.Channel1.Verify(c => c.ExchangeDeclare("bar", "direct", true, false, It.IsAny<IDictionary<string, object>>()));
        _fixture.Channel1.Verify(c => c.QueueBind("foo", "bar", "foo", It.IsAny<IDictionary<string, object>>()));

        _fixture.Listener2.Value.OnCreate(_fixture.Conn2.Object);
        _fixture.Channel2.Verify(c => c.QueueDeclare("foo", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
        _fixture.Channel2.Verify(c => c.QueueDeclare("baz", true, false, false, It.IsAny<IDictionary<string, object>>()), Times.Never);
        _fixture.Channel2.Verify(c => c.QueueDeclare("qux", true, false, false, It.IsAny<IDictionary<string, object>>()));
        _fixture.Channel2.Verify(c => c.ExchangeDeclare("bar", "direct", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
        _fixture.Channel2.Verify(c => c.QueueBind("foo", "bar", "foo", It.IsAny<IDictionary<string, object>>()), Times.Never);

        _fixture.Listener3.Value.OnCreate(_fixture.Conn3.Object);
        _fixture.Channel3.Verify(c => c.QueueDeclare("foo", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
        var args = new Dictionary<string, object>
        {
            { "added.by.customizer.1", true },
            { "added.by.customizer.2", true }
        };
        _fixture.Channel3.Verify(c => c.QueueDeclare("baz", true, false, false, args));
        _fixture.Channel3.Verify(c => c.QueueDeclare("qux", true, false, false, It.IsAny<IDictionary<string, object>>()), Times.Never);
        _fixture.Channel3.Verify(c => c.ExchangeDeclare("bar", "direct", It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Never);
        _fixture.Channel3.Verify(c => c.QueueBind("foo", "bar", "foo", It.IsAny<IDictionary<string, object>>()), Times.Never);
    }

    [Fact]
    public void TestAddRemove()
    {
        var queue = new Queue("foo");
        var cf = new Mock<IConnectionFactory>();
        var admin1 = new RabbitAdmin(cf.Object);
        var admin2 = new RabbitAdmin(cf.Object);
        queue.SetAdminsThatShouldDeclare(admin1, admin2);
        Assert.Equal(2, queue.DeclaringAdmins.Count);
        queue.SetAdminsThatShouldDeclare(admin1);
        Assert.Single(queue.DeclaringAdmins);
        queue.SetAdminsThatShouldDeclare(new object[] { null });
        Assert.Empty(queue.DeclaringAdmins);
        queue.SetAdminsThatShouldDeclare(admin1, admin2);
        Assert.Equal(2, queue.DeclaringAdmins.Count);
        queue.SetAdminsThatShouldDeclare();
        Assert.Empty(queue.DeclaringAdmins);
        queue.SetAdminsThatShouldDeclare(admin1, admin2);
        Assert.Equal(2, queue.DeclaringAdmins.Count);
        queue.SetAdminsThatShouldDeclare(null);
        Assert.Empty(queue.DeclaringAdmins);
        queue.SetAdminsThatShouldDeclare(admin1, admin2);
        Assert.Equal(2, queue.DeclaringAdmins.Count);
        queue.SetAdminsThatShouldDeclare(null);
        Assert.Empty(queue.DeclaringAdmins);
        Assert.Throws<InvalidOperationException>(() => queue.SetAdminsThatShouldDeclare(null, admin1));
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void TestNoOpWhenNothingToDeclare()
#pragma warning restore S2699 // Tests should include assertions
    {
    }

    public sealed class RabbitAdminDeclarationTestStartupFixture : IDisposable
    {
        private readonly IServiceCollection _services;

        public ServiceProvider Provider { get; set; }

        public Mock<IConnection> Conn1 { get; set; }

        public Mock<IConnection> Conn2 { get; set; }

        public Mock<IConnection> Conn3 { get; set; }

        public Mock<RC.IModel> Channel1 { get; set; }

        public Mock<RC.IModel> Channel2 { get; set; }

        public Mock<RC.IModel> Channel3 { get; set; }

        public AtomicReference<IConnectionListener> Listener1 { get; set; } = new ();

        public AtomicReference<IConnectionListener> Listener2 { get; set; } = new ();

        public AtomicReference<IConnectionListener> Listener3 { get; set; } = new ();

        public RabbitAdminDeclarationTestStartupFixture()
        {
            _services = CreateContainer();
            Provider = _services.BuildServiceProvider();
            Provider.GetRequiredService<IHostedService>().StartAsync(default).Wait();
        }

        public ServiceCollection CreateContainer(IConfiguration config = null)
        {
            var services = new ServiceCollection();
            config ??= new ConfigurationBuilder().Build();

            services.AddLogging(b =>
            {
                b.AddDebug();
                b.AddConsole();
            });

            services.AddSingleton(config);
            services.AddRabbitHostingServices();

            // services.AddRabbitDefaultMessageConverter();
            // services.AddRabbitListenerEndpointRegistry();
            // services.AddRabbitListenerEndpointRegistrar();
            // services.AddRabbitListenerAttributeProcessor();

            // ConnectionFactory cf1
            services.AddSingleton(_ =>
            {
                var mockConnectionFactory = new Mock<IConnectionFactory>();
                Conn1 = new Mock<IConnection>();
                Channel1 = new Mock<RC.IModel>();
                mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(Conn1.Object);
                mockConnectionFactory.SetupGet(f => f.ServiceName).Returns("cf1");
                Conn1.Setup(c => c.CreateChannel(false)).Returns(Channel1.Object);
                Conn1.Setup(c => c.IsOpen).Returns(true);
                Channel1.Setup(c => c.IsOpen).Returns(true);
                var queueName = new AtomicReference<string>();
                Channel1.Setup(c => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()))
                    .Callback<string, bool, bool, bool, IDictionary<string, object>>((a1, _, _, _, _) => queueName.Value = a1)
                    .Returns(() => new RC.QueueDeclareOk(queueName.Value, 0, 0));
                mockConnectionFactory.Setup(f => f.AddConnectionListener(It.IsAny<IConnectionListener>()))
                    .Callback<IConnectionListener>(l => Listener1.Value = l);
                return mockConnectionFactory.Object;
            });

            // ConnectionFactory cf2
            services.AddSingleton(_ =>
            {
                var mockConnectionFactory = new Mock<IConnectionFactory>();
                Conn2 = new Mock<IConnection>();
                Channel2 = new Mock<RC.IModel>();
                mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(Conn2.Object);
                mockConnectionFactory.SetupGet(f => f.ServiceName).Returns("cf2");
                Conn2.Setup(c => c.CreateChannel(false)).Returns(Channel2.Object);
                Conn2.Setup(c => c.IsOpen).Returns(true);
                Channel2.Setup(c => c.IsOpen).Returns(true);
                var queueName = new AtomicReference<string>();
                Channel2.Setup(c => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()))
                    .Callback<string, bool, bool, bool, IDictionary<string, object>>((a1, _, _, _, _) => queueName.Value = a1)
                    .Returns(() => new RC.QueueDeclareOk(queueName.Value, 0, 0));
                mockConnectionFactory.Setup(f => f.AddConnectionListener(It.IsAny<IConnectionListener>()))
                    .Callback<IConnectionListener>(l => Listener2.Value = l);
                return mockConnectionFactory.Object;
            });

            // ConnectionFactory cf3
            services.AddSingleton(_ =>
            {
                var mockConnectionFactory = new Mock<IConnectionFactory>();
                Conn3 = new Mock<IConnection>();
                Channel3 = new Mock<RC.IModel>();
                mockConnectionFactory.Setup(f => f.CreateConnection()).Returns(Conn3.Object);
                mockConnectionFactory.SetupGet(f => f.ServiceName).Returns("cf3");
                Conn3.Setup(c => c.CreateChannel(false)).Returns(Channel3.Object);
                Conn3.Setup(c => c.IsOpen).Returns(true);
                Channel3.Setup(c => c.IsOpen).Returns(true);
                var queueName = new AtomicReference<string>();
                Channel3.Setup(c => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()))
                    .Callback<string, bool, bool, bool, IDictionary<string, object>>((a1, _, _, _, _) => queueName.Value = a1)
                    .Returns(() => new RC.QueueDeclareOk(queueName.Value, 0, 0));
                mockConnectionFactory.Setup(f => f.AddConnectionListener(It.IsAny<IConnectionListener>()))
                    .Callback<IConnectionListener>(l => Listener3.Value = l);
                return mockConnectionFactory.Object;
            });

            services.AddSingleton(p =>
            {
                var context = p.GetApplicationContext();
                var cf1 = context.GetService<IConnectionFactory>("cf1");
                var admin = new RabbitAdmin(context, cf1, p.GetService<ILogger<RabbitAdmin>>())
                {
                    ServiceName = "admin1"
                };
                return admin;
            });
            services.AddSingleton<IRabbitAdmin>(p =>
            {
                var context = p.GetApplicationContext();
                return context.GetService<RabbitAdmin>("admin1");
            });

            services.AddSingleton(p =>
            {
                var context = p.GetApplicationContext();
                var cf2 = context.GetService<IConnectionFactory>("cf2");
                var admin = new RabbitAdmin(context, cf2, p.GetService<ILogger<RabbitAdmin>>())
                {
                    ServiceName = "admin2"
                };
                return admin;
            });
            services.AddSingleton<IRabbitAdmin>(p =>
            {
                var context = p.GetApplicationContext();
                return context.GetService<RabbitAdmin>("admin2");
            });

            services.AddSingleton(p =>
            {
                var context = p.GetApplicationContext();
                var cf3 = context.GetService<IConnectionFactory>("cf3");
                var admin = new RabbitAdmin(context, cf3, p.GetService<ILogger<RabbitAdmin>>())
                {
                    ExplicitDeclarationsOnly = true,
                    ServiceName = "admin3"
                };
                return admin;
            });
            services.AddSingleton<IRabbitAdmin>(p =>
            {
                var context = p.GetApplicationContext();
                return context.GetService<RabbitAdmin>("admin3");
            });

            var queueFoo = new Queue("foo");
            queueFoo.SetAdminsThatShouldDeclare("admin1");
            services.AddRabbitQueue(queueFoo);

            var queueBaz = new Queue("baz");
            queueBaz.SetAdminsThatShouldDeclare("admin3");
            services.AddRabbitQueue(queueBaz);

            var queueQux = new Queue("qux");
            services.AddRabbitQueue(queueQux);

            var exchange = new DirectExchange("bar");
            exchange.SetAdminsThatShouldDeclare("admin1");
            exchange.IsInternal = true;
            services.AddRabbitExchange(exchange);

            var binding = new Binding("foo.binding", "foo", Binding.DestinationType.QUEUE, "bar", "foo", null);
            binding.SetAdminsThatShouldDeclare("admin1");
            services.AddRabbitBinding(binding);
            services.AddSingleton<IDeclarableCustomizer, Customizer1>();
            services.AddSingleton<IDeclarableCustomizer, Customizer2>();

            return services;
        }

        public void Dispose()
        {
            Provider.Dispose();
        }

        private sealed class Customizer1 : IDeclarableCustomizer
        {
            public IDeclarable Apply(IDeclarable declarable)
            {
                if (declarable is IQueue queue && queue.QueueName == "baz")
                {
                    queue.AddArgument("added.by.customizer.1", true);
                }

                return declarable;
            }
        }

        private sealed class Customizer2 : IDeclarableCustomizer
        {
            public IDeclarable Apply(IDeclarable declarable)
            {
                if (declarable is IQueue queue && queue.QueueName == "baz")
                {
                    queue.AddArgument("added.by.customizer.2", true);
                }

                return declarable;
            }
        }
    }
}
