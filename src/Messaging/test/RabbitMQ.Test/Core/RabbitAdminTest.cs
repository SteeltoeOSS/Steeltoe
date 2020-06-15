// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Steeltoe.Messaging.Rabbit.Config.Binding;

namespace Steeltoe.Messaging.Rabbit.Core
{
    public class RabbitAdminTest
    {
        [Fact]
        public void TestSettingOfNullConnectionFactory()
        {
            Connection.IConnectionFactory connectionFactory = null;
            Assert.Throws<ArgumentNullException>(() => new RabbitAdmin(connectionFactory));
        }

        [Fact]
        public void TestNoFailOnStartupWithMissingBroker()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddRabbitQueue(new Config.Queue("foo"));

            var connectionFactory = new SingleConnectionFactory("foo")
            {
                Port = 434343
            };
            var applicationContext = GetApplicationContext(serviceCollection);
            var rabbitAdmin = new RabbitAdmin(applicationContext, connectionFactory)
            {
                AutoStartup = true
            };
            connectionFactory.Destroy();
        }

        [Fact]
        public void TestFailOnFirstUseWithMissingBroker()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddRabbitQueue(new Config.Queue("foo"));
            var connectionFactory = new SingleConnectionFactory("localhost")
            {
                Port = 434343
            };
            var applicationContext = GetApplicationContext(serviceCollection);
            var rabbitAdmin = new RabbitAdmin(applicationContext, connectionFactory)
            {
                AutoStartup = true
            };
            Assert.Throws<AmqpConnectException>(() => rabbitAdmin.DeclareQueue());
            connectionFactory.Destroy();
        }

        [Fact(Skip = "Requires Broker")]
        public async Task TestGetQueueProperties()
        {
            var serviceCollection = new ServiceCollection();
            var connectionFactory = new SingleConnectionFactory("localhost");
            var applicationContext = GetApplicationContext(serviceCollection);
            var rabbitAdmin = new RabbitAdmin(applicationContext, connectionFactory);
            var queueName = "test.properties." + DateTimeOffset.Now.ToUnixTimeMilliseconds();
            try
            {
                rabbitAdmin.DeclareQueue(new Config.Queue(queueName));
                var template = new RabbitTemplate(connectionFactory);
                template.ConvertAndSend(queueName, "foo");
                var n = 0;
                while (n++ < 100 && MessageCount(rabbitAdmin, queueName) == 0)
                {
                    await Task.Delay(100);
                }

                Assert.True(n < 100);
                var channel = connectionFactory.CreateConnection().CreateChannel(false);
                var consumer = new DefaultBasicConsumer(channel);
                channel.BasicConsume(queueName, true, consumer);
                n = 0;
                while (n++ < 100 && MessageCount(rabbitAdmin, queueName) > 0)
                {
                    await Task.Delay(100);
                }

                Assert.True(n < 100);

                var props = rabbitAdmin.GetQueueProperties(queueName);
                Assert.True(props.TryGetValue(RabbitAdmin.QUEUE_CONSUMER_COUNT, out var consumerCount));
                Assert.Equal(1U, consumerCount);
                channel.Close();
            }
            finally
            {
                rabbitAdmin.DeleteQueue(queueName);
                connectionFactory.Destroy();
            }
        }

        [Fact(Skip = "Requires Broker")]
        public void TestTemporaryLogs()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddRabbitQueue(new Config.Queue("testq.nonDur", false, false, false));
            serviceCollection.AddRabbitQueue(new Config.Queue("testq.ad", true, false, true));
            serviceCollection.AddRabbitQueue(new Config.Queue("testq.excl", true, true, false));
            serviceCollection.AddRabbitQueue(new Config.Queue("testq.all", false, true, true));
            serviceCollection.AddRabbitExchange(new Config.DirectExchange("testex.nonDur", false, false));
            serviceCollection.AddRabbitExchange(new Config.DirectExchange("testex.ad", true, true));
            serviceCollection.AddRabbitExchange(new Config.DirectExchange("testex.all", false, true));
            var connectionFactory = new SingleConnectionFactory("localhost");
            var applicationContext = GetApplicationContext(serviceCollection);

            var logs = new List<string>();
            var mockLogger = new Mock<ILogger>();
            mockLogger.Setup((l) => l.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()))
                .Callback(new InvocationAction(invocation =>
                {
                    logs.Add(invocation.Arguments[2].ToString());
                }));
            var rabbitAdmin = new RabbitAdmin(applicationContext, connectionFactory, mockLogger.Object);

            try
            {
                connectionFactory.CreateConnection().Close();
                logs.Sort();
                Assert.NotEmpty(logs);
                Assert.Contains("(testex.ad) durable:True, auto-delete:True", logs[0]);
                Assert.Contains("(testex.all) durable:False, auto-delete:True", logs[1]);
                Assert.Contains("(testex.nonDur) durable:False, auto-delete:False", logs[2]);
                Assert.Contains("(testq.ad) durable:True, auto-delete:True, exclusive:False", logs[3]);
                Assert.Contains("(testq.all) durable:False, auto-delete:True, exclusive:True", logs[4]);
                Assert.Contains("(testq.excl) durable:True, auto-delete:False, exclusive:True", logs[5]);
                Assert.Contains("(testq.nonDur) durable:False, auto-delete:False, exclusive:False", logs[6]);
            }
            finally
            {
                CleanQueuesAndExchanges(rabbitAdmin);
                connectionFactory.Destroy();
            }
        }

        [Fact(Skip = "Requires Broker")]
        public void TestMultiEntities()
        {
            var serviceCollection = CreateContainer();
            var e1 = new Config.DirectExchange("e1", false, true);
            serviceCollection.AddRabbitExchange(e1);
            var q1 = new Config.Queue("q1", false, false, true);
            serviceCollection.AddRabbitQueue(q1);
            var binding = BindingBuilder.Bind(q1).To(e1).With("k1");
            serviceCollection.AddRabbitBinding(binding);
            var es = new Declarables(
                "es",
                new DirectExchange("e2", false, true),
                new DirectExchange("e3", false, true));
            serviceCollection.AddSingleton(es);
            var qs = new Declarables(
                "qs",
                new Config.Queue("q2", false, false, true),
                new Config.Queue("q3", false, false, true));
            serviceCollection.AddSingleton(qs);
            var bs = new Declarables(
                "qs",
                new Binding("b1", "q2", DestinationType.QUEUE, "e2", "k2", null),
                new Binding("b2", "q3", DestinationType.QUEUE, "e3", "k3", null));
            serviceCollection.AddSingleton(bs);
            var ds = new Declarables(
                "ds",
                new DirectExchange("e4", false, true),
                new Queue("q4", false, false, true),
                new Binding("b3", "q4", DestinationType.QUEUE, "e4", "k4", null));
            serviceCollection.AddSingleton(ds);
            var provider = serviceCollection.BuildServiceProvider();
            var admin = provider.GetRabbitAdmin();
            var template = provider.GetRabbitTempate();
            template.ConvertAndSend("e1", "k1", "foo");
            template.ConvertAndSend("e2", "k2", "bar");
            template.ConvertAndSend("e3", "k3", "baz");
            template.ConvertAndSend("e4", "k4", "qux");
            Assert.Equal("foo", template.ReceiveAndConvert("q1"));
            Assert.Equal("bar", template.ReceiveAndConvert("q2"));
            Assert.Equal("baz", template.ReceiveAndConvert("q3"));
            Assert.Equal("qux", template.ReceiveAndConvert("q4"));
            admin.DeleteQueue("q1");
            admin.DeleteQueue("q2");
            admin.DeleteQueue("q3");
            admin.DeleteQueue("q4");
            admin.DeleteExchange("e1");
            admin.DeleteExchange("e2");
            admin.DeleteExchange("e3");
            admin.DeleteExchange("e4");

            var ctx = provider.GetService<IApplicationContext>();
            var mixedDeclarables = ctx.GetService<Declarables>("ds");
            Assert.NotNull(mixedDeclarables);
            var queues = mixedDeclarables.GetDeclarablesByType<Queue>();
            Assert.Single(queues);
            Assert.Equal("q4", queues.Single().Name);
            var exchanges = mixedDeclarables.GetDeclarablesByType<IExchange>();
            Assert.Single(exchanges);
            Assert.Equal("e4", exchanges.Single().Name);
            var bindings = mixedDeclarables.GetDeclarablesByType<Binding>();
            Assert.Single(bindings);
            Assert.Equal("q4", bindings.Single().Destination);
        }

        [Fact(Skip = "Requires Broker")]
        public void TestAvoidHangAMQP_508()
        {
            var cf = new CachingConnectionFactory("localhost");
            var admin = new RabbitAdmin(cf);
            var bytes = new byte[300];
            var longName = Encoding.UTF8.GetString(bytes).Replace('\u0000', 'x');
            try
            {
                admin.DeclareQueue(new Queue(longName));
                throw new Exception("expected exception");
            }
            catch (Exception)
            {
                // Ignore
            }

            var goodName = "foobar";
            var name = admin.DeclareQueue(new Queue(goodName));
            Assert.Null(admin.GetQueueProperties(longName));
            Assert.NotNull(admin.GetQueueProperties(goodName));
            admin.DeleteQueue(goodName);
            cf.Destroy();
        }

        private void CleanQueuesAndExchanges(RabbitAdmin rabbitAdmin)
        {
            rabbitAdmin.DeleteQueue("testq.nonDur");
            rabbitAdmin.DeleteQueue("testq.ad");
            rabbitAdmin.DeleteQueue("testq.excl");
            rabbitAdmin.DeleteQueue("testq.all");
            rabbitAdmin.DeleteExchange("testex.nonDur");
            rabbitAdmin.DeleteExchange("testex.ad");
            rabbitAdmin.DeleteExchange("testex.all");
        }

        private uint MessageCount(RabbitAdmin rabbitAdmin, string queueName)
        {
            var info = rabbitAdmin.GetQueueInfo(queueName);
            Assert.NotNull(info);
            return info.MessageCount;
        }

        private ServiceCollection CreateContainer(ConfigurationBuilder configurationBuilder = null)
        {
            var services = new ServiceCollection();
            if (configurationBuilder == null)
            {
                configurationBuilder = new ConfigurationBuilder();
            }

            var configuration = configurationBuilder.Build();
            services.AddSingleton(configuration);
            services.AddSingleton<IConfiguration>((p) => p.GetRequiredService<IConfigurationRoot>());
            services.AddOptions();
            services.AddRabbitServices();
            services.AddRabbitAdmin();
            services.AddRabbitTemplate();
            return services;
        }

        private IApplicationContext GetApplicationContext(ServiceCollection serviceCollection, ConfigurationBuilder builder = null)
        {
            if (builder == null)
            {
                return new GenericApplicationContext(serviceCollection.BuildServiceProvider(), null);
            }
            else
            {
                return new GenericApplicationContext(serviceCollection.BuildServiceProvider(), builder.Build());
            }
        }
    }
}
