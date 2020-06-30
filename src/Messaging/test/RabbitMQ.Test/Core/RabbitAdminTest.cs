// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Retry;
using Steeltoe.Messaging.Rabbit.Config;
using Steeltoe.Messaging.Rabbit.Connection;
using Steeltoe.Messaging.Rabbit.Exceptions;
using Steeltoe.Messaging.Rabbit.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Steeltoe.Messaging.Rabbit.Config.Binding;
using IConnectionFactory = Steeltoe.Messaging.Rabbit.Connection.IConnectionFactory;

namespace Steeltoe.Messaging.Rabbit.Core
{
    public class RabbitAdminTest : AbstractTest
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
            var serviceCollection = CreateContainer();
            serviceCollection.AddRabbitQueue(new Config.Queue("foo"));
            serviceCollection.AddRabbitConnectionFactory<SingleConnectionFactory>((p, f) =>
            {
                f.Host = "foo";
                f.Port = 434343;
            });
            var provider = serviceCollection.BuildServiceProvider();
            var applicationContext = provider.GetService<IApplicationContext>();
            var connectionFactory = applicationContext.GetService<IConnectionFactory>();

            var rabbitAdmin = new RabbitAdmin(applicationContext, connectionFactory)
            {
                AutoStartup = true
            };
            connectionFactory.Destroy();
        }

        [Fact]
        public void TestFailOnFirstUseWithMissingBroker()
        {
            var serviceCollection = CreateContainer();
            serviceCollection.AddRabbitQueue(new Config.Queue("foo"));
            serviceCollection.AddRabbitConnectionFactory<SingleConnectionFactory>((p, f) =>
            {
                f.Host = "localhost";
                f.Port = 434343;
            });

            var provider = serviceCollection.BuildServiceProvider();
            var applicationContext = provider.GetService<IApplicationContext>();
            var connectionFactory = applicationContext.GetService<IConnectionFactory>();
            var rabbitAdmin = new RabbitAdmin(applicationContext, connectionFactory)
            {
                AutoStartup = true
            };
            Assert.Throws<RabbitConnectException>(() => rabbitAdmin.DeclareQueue());
            connectionFactory.Destroy();
        }

        [Fact]
        public async Task TestGetQueueProperties()
        {
            var serviceCollection = CreateContainer();
            serviceCollection.AddRabbitConnectionFactory<SingleConnectionFactory>((p, f) =>
            {
                f.Host = "localhost";
            });
            var provider = serviceCollection.BuildServiceProvider();
            var applicationContext = provider.GetService<IApplicationContext>();
            var connectionFactory = applicationContext.GetService<IConnectionFactory>();
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

        [Fact]
        public void TestTemporaryLogs()
        {
            var serviceCollection = CreateContainer();
            serviceCollection.AddRabbitQueue(new Config.Queue("testq.nonDur", false, false, false));
            serviceCollection.AddRabbitQueue(new Config.Queue("testq.ad", true, false, true));
            serviceCollection.AddRabbitQueue(new Config.Queue("testq.excl", true, true, false));
            serviceCollection.AddRabbitQueue(new Config.Queue("testq.all", false, true, true));
            serviceCollection.AddRabbitExchange(new Config.DirectExchange("testex.nonDur", false, false));
            serviceCollection.AddRabbitExchange(new Config.DirectExchange("testex.ad", true, true));
            serviceCollection.AddRabbitExchange(new Config.DirectExchange("testex.all", false, true));
            serviceCollection.AddRabbitConnectionFactory<SingleConnectionFactory>((p, f) =>
            {
                f.Host = "localhost";
            });
            var provider = serviceCollection.BuildServiceProvider();
            var applicationContext = provider.GetService<IApplicationContext>();
            var connectionFactory = applicationContext.GetService<IConnectionFactory>();

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
                Assert.Contains("(testex.ad), durable:True, auto-delete:True", logs[0]);
                Assert.Contains("(testex.all), durable:False, auto-delete:True", logs[1]);
                Assert.Contains("(testex.nonDur), durable:False, auto-delete:False", logs[2]);
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

        [Fact]
        public void TestMultiEntities()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            serviceCollection.AddRabbitServices();
            serviceCollection.AddRabbitAdmin();
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
            var admin = provider.GetRabbitAdmin() as RabbitAdmin;
            var template = admin.RabbitTemplate;
            template.ConvertAndSend("e1", "k1", "foo");
            template.ConvertAndSend("e2", "k2", "bar");
            template.ConvertAndSend("e3", "k3", "baz");
            template.ConvertAndSend("e4", "k4", "qux");
            Assert.Equal("foo", template.ReceiveAndConvert<string>("q1"));
            Assert.Equal("bar", template.ReceiveAndConvert<string>("q2"));
            Assert.Equal("baz", template.ReceiveAndConvert<string>("q3"));
            Assert.Equal("qux", template.ReceiveAndConvert<string>("q4"));
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
            var queues = mixedDeclarables.GetDeclarablesByType<IQueue>();
            Assert.Single(queues);
            Assert.Equal("q4", queues.Single().QueueName);
            var exchanges = mixedDeclarables.GetDeclarablesByType<IExchange>();
            Assert.Single(exchanges);
            Assert.Equal("e4", exchanges.Single().ExchangeName);
            var bindings = mixedDeclarables.GetDeclarablesByType<IBinding>();
            Assert.Single(bindings);
            Assert.Equal("q4", bindings.Single().Destination);
        }

        [Fact]
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

        [Fact]
        public void TestIgnoreDeclarationExceptionsTimeout()
        {
            var rabbitConnectionFactory = new Mock<RabbitMQ.Client.IConnectionFactory>();
            var toBeThrown = new TimeoutException("test");
            rabbitConnectionFactory.Setup((c) => c.CreateConnection(It.IsAny<string>())).Throws(toBeThrown);
            var ccf = new CachingConnectionFactory(rabbitConnectionFactory.Object);
            var admin = new RabbitAdmin(ccf);
            admin.IgnoreDeclarationExceptions = true;

            admin.DeclareQueue(new AnonymousQueue("test"));
            var lastEvent = admin.LastDeclarationExceptionEvent;
            Assert.Same(admin, lastEvent.Source);
            Assert.Same(toBeThrown, lastEvent.Exception.InnerException);
            Assert.IsType<AnonymousQueue>(lastEvent.Declarable);

            admin.DeclareQueue();
            lastEvent = admin.LastDeclarationExceptionEvent;
            Assert.Same(admin, lastEvent.Source);
            Assert.Same(toBeThrown, lastEvent.Exception.InnerException);
            Assert.Null(lastEvent.Declarable);

            admin.DeclareExchange(new DirectExchange("foo"));
            lastEvent = admin.LastDeclarationExceptionEvent;
            Assert.Same(admin, lastEvent.Source);
            Assert.Same(toBeThrown, lastEvent.Exception.InnerException);
            Assert.IsType<DirectExchange>(lastEvent.Declarable);

            admin.DeclareBinding(new Binding("foo", "foo", DestinationType.QUEUE, "bar", "baz", null));
            lastEvent = admin.LastDeclarationExceptionEvent;
            Assert.Same(admin, lastEvent.Source);
            Assert.Same(toBeThrown, lastEvent.Exception.InnerException);
            Assert.IsType<Binding>(lastEvent.Declarable);
        }

        [Fact]
        public void TestWithinInvoke()
        {
            var connectionFactory = new Mock<Connection.IConnectionFactory>();
            var connection = new Mock<Connection.IConnection>();
            connectionFactory.Setup((f) => f.CreateConnection()).Returns(connection.Object);

            var channel1 = new Mock<IModel>();
            var channel2 = new Mock<IModel>();

            connection.SetupSequence((c) => c.CreateChannel(false)).Returns(channel1.Object).Returns(channel2.Object);
            var declareOk = new QueueDeclareOk("foo", 0, 0);
            channel1.Setup((c) => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>())).Returns(declareOk);
            var template = new RabbitTemplate(connectionFactory.Object);
            var admin = new RabbitAdmin(template);

            template.Invoke<object>((o) =>
            {
                admin.DeclareQueue();
                admin.DeclareQueue();
                admin.DeclareQueue();
                admin.DeclareQueue();
                return null;
            });
            connection.Verify((c) => c.CreateChannel(false), Times.Once);
            channel1.Verify((c) => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Exactly(4));
            channel1.Verify((c) => c.Close(), Times.Once);
            channel2.VerifyNoOtherCalls();
        }

        [Fact]
        public void TestRetry()
        {
            var connectionFactory = new Mock<RabbitMQ.Client.IConnectionFactory>();
            var connection = new Mock<RabbitMQ.Client.IConnection>();
            connection.Setup(c => c.IsOpen).Returns(true);
            connectionFactory.Setup((f) => f.CreateConnection(It.IsAny<string>())).Returns(connection.Object);

            var channel1 = new Mock<IModel>();
            channel1.Setup(c => c.IsOpen).Returns(true);
            connection.Setup(c => c.CreateModel()).Returns(channel1.Object);
            channel1.Setup((c) => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>())).Throws<Exception>();
            var ccf = new CachingConnectionFactory(connectionFactory.Object);

            var rtt = new PollyRetryTemplate(new Dictionary<Type, bool>(), 3, true, 1, 1, 1);
            var serviceCollection = CreateContainer();
            serviceCollection.AddSingleton<IConnectionFactory>(ccf);
            serviceCollection.AddRabbitAdmin((p, a) =>
            {
                a.RetryTemplate = rtt;
            });
            var foo = new Config.AnonymousQueue("foo");
            serviceCollection.AddRabbitQueue(foo);
            var provider = serviceCollection.BuildServiceProvider();
            var admin = provider.GetRabbitAdmin();
            Assert.Throws<RabbitUncategorizedException>(() => ccf.CreateConnection());
            channel1.Verify((c) => c.QueueDeclare(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<IDictionary<string, object>>()), Times.Exactly(3));
        }

        [Fact]
        public async Task TestMasterLocator()
        {
            ConnectionFactory factory = new ConnectionFactory();
            factory.Uri = new Uri("amqp://guest:guest@localhost:5672/");
            var cf = new CachingConnectionFactory(factory);
            var admin = new RabbitAdmin(cf);
            var queue = new AnonymousQueue();
            admin.DeclareQueue(queue);
            var client = new HttpClient();
            var authToken = Encoding.ASCII.GetBytes("guest:guest");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

            var result = await client.GetAsync("http://localhost:15672/api/queues/%3F/" + queue.QueueName);
            int n = 0;
            while (n++ < 100 && result.StatusCode == HttpStatusCode.NotFound)
            {
                await Task.Delay(100);
                result = await client.GetAsync("http://localhost:15672/api/queues/%2F/" + queue.QueueName);
            }

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var content = await result.Content.ReadAsStringAsync();
            Assert.Contains("x-queue-master-locator", content);
            Assert.Contains("client-local", content);

            queue = new AnonymousQueue();
            queue.MasterLocator = null;
            admin.DeclareQueue(queue);

            result = await client.GetAsync("http://localhost:15672/api/queues/%3F/" + queue.QueueName);
            n = 0;
            while (n++ < 100 && result.StatusCode == HttpStatusCode.NotFound)
            {
                await Task.Delay(100);
                result = await client.GetAsync("http://localhost:15672/api/queues/%2F/" + queue.QueueName);
            }

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            content = await result.Content.ReadAsStringAsync();
            Assert.DoesNotContain("x-queue-master-locator", content);
            Assert.DoesNotContain("client-local", content);
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
    }
}
