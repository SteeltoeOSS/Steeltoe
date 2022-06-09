// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.Support;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Config.Binding;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Core;

[Trait("Category", "Integration")]
public class RabbitAdminIntegrationTest : IDisposable
{
    private readonly ServiceCollection _services;
    private ServiceProvider _provider;

    public RabbitAdminIntegrationTest()
    {
        _services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        _services.AddLogging(b =>
        {
            b.AddDebug();
            b.AddConsole();
        });

        _services.AddSingleton<IConfiguration>(config);
        _services.AddRabbitHostingServices();
        _services.AddRabbitConnectionFactory((p, f) => f.Host = "localhost");
        _services.AddRabbitAdmin((p, a) => a.AutoStartup = true);
    }

    public void Dispose()
    {
        var admin = _provider.GetRabbitAdmin();
        if (admin != null)
        {
            admin.DeleteQueue("test.queue");
        }

        _provider.Dispose();
    }

    [Fact]
    public void TestStartupWithLazyDeclaration()
    {
        var queue = new Queue("test.queue");
        _services.AddRabbitQueue(queue);
        _provider = _services.BuildServiceProvider();

        var rabbitAdmin = _provider.GetRabbitAdmin();

        // A new connection is initialized so the queue is declared
        Assert.True(rabbitAdmin.DeleteQueue(queue.QueueName));
    }

    [Fact]
    public void TestDoubleDeclarationOfExclusiveQueue()
    {
        _services.AddRabbitConnectionFactory("connectionFactory1", (p, f) =>
        {
            f.Host = "localhost";
        });
        _services.AddRabbitConnectionFactory("connectionFactory2", (p, f) =>
        {
            f.Host = "localhost";
        });
        _provider = _services.BuildServiceProvider();

        var queue = new Queue("test.queue", false, true, true);
        var rabbitAdmin = _provider.GetRabbitAdmin();
        rabbitAdmin.DeleteQueue(queue.QueueName);

        var context = _provider.GetApplicationContext();
        var admin1 = new RabbitAdmin(context, context.GetService<IConnectionFactory>("connectionFactory1"));
        admin1.DeclareQueue(queue);
        try
        {
            var admin2 = new RabbitAdmin(context, context.GetService<IConnectionFactory>("connectionFactory2"));
            Assert.Throws<RabbitIOException>(() => admin2.DeclareQueue(queue));
        }
        finally
        {
            var cf1 = context.GetService<IConnectionFactory>("connectionFactory1");
            var cf2 = context.GetService<IConnectionFactory>("connectionFactory2");
            cf1.Destroy();
            cf2.Destroy();
        }
    }

    [Fact]
    public void TestDoubleDeclarationOfAutodeleteQueue()
    {
        _services.AddRabbitConnectionFactory("connectionFactory1", (p, f) =>
        {
            f.Host = "localhost";
        });
        _services.AddRabbitConnectionFactory("connectionFactory2", (p, f) =>
        {
            f.Host = "localhost";
        });
        _provider = _services.BuildServiceProvider();
        var queue = new Queue("test.queue", false, false, true);

        var context = _provider.GetApplicationContext();
        var cf1 = context.GetService<IConnectionFactory>("connectionFactory1");
        var cf2 = context.GetService<IConnectionFactory>("connectionFactory2");
        var admin1 = new RabbitAdmin(context, cf1);
        admin1.DeclareQueue(queue);
        var admin2 = new RabbitAdmin(context, cf2);
        admin2.DeclareQueue(queue);
        cf1.Destroy();
        cf2.Destroy();
    }

    [Fact]
    public void TestQueueWithAutoDelete()
    {
        var queue = new Queue("test.queue", false, true, true);
        _services.AddRabbitQueue(queue);
        _provider = _services.BuildServiceProvider();

        var rabbitAdmin = _provider.GetRabbitAdmin();
        rabbitAdmin.Initialize();
        Assert.True(QueueExists(queue));

        var cf = _provider.GetRabbitConnectionFactory();
        cf.Destroy();
        Assert.False(QueueExists(queue));

        cf.CreateConnection();
        Assert.True(QueueExists(queue));

        Assert.True(rabbitAdmin.DeleteQueue(queue.QueueName));
        Assert.False(QueueExists(queue));
    }

    [Fact]
    public void TestQueueWithoutAutoDelete()
    {
        var queue = new Queue("test.queue", false, false, false);
        _services.AddRabbitQueue(queue);
        _provider = _services.BuildServiceProvider();

        var rabbitAdmin = _provider.GetRabbitAdmin();
        rabbitAdmin.Initialize();
        Assert.True(QueueExists(queue));

        var cf = _provider.GetRabbitConnectionFactory();
        cf.Destroy();
        Assert.True(QueueExists(queue));

        cf.CreateConnection();
        Assert.True(QueueExists(queue));

        Assert.True(rabbitAdmin.DeleteQueue(queue.QueueName));
        Assert.False(QueueExists(queue));

        cf.Destroy();
    }

    [Fact]
    public void TestQueueWithoutName()
    {
        _provider = _services.BuildServiceProvider();
        var queue = new Queue(string.Empty, true, false, true);
        var rabbitAdmin = _provider.GetRabbitAdmin();
        var generatedName = rabbitAdmin.DeclareQueue(queue);

        Assert.Equal(string.Empty, queue.QueueName);
        var queueWithGeneratedName = new Queue(generatedName, true, false, true);
        Assert.True(QueueExists(queueWithGeneratedName));

        var cf = _provider.GetRabbitConnectionFactory();
        cf.Destroy();
        Assert.True(QueueExists(queueWithGeneratedName));

        cf.CreateConnection();
        Assert.True(QueueExists(queueWithGeneratedName));

        Assert.True(rabbitAdmin.DeleteQueue(generatedName));
        Assert.False(QueueExists(queueWithGeneratedName));

        cf.Destroy();
    }

    [Fact]
    public void TestDeclareExchangeWithDefaultExchange()
    {
        var exchange = new DirectExchange(string.Empty);
        _provider = _services.BuildServiceProvider();
        var rabbitAdmin = _provider.GetRabbitAdmin();
        rabbitAdmin.DeclareExchange(exchange);

        // Pass by virtue of RabbitMQ not firing a 403 reply code
    }

    [Fact]
    public void TestSpringWithDefaultExchange()
    {
        var exchange = new DirectExchange(string.Empty);
        _services.AddRabbitExchange(exchange);
        _provider = _services.BuildServiceProvider();

        var rabbitAdmin = _provider.GetRabbitAdmin();
        rabbitAdmin.Initialize();

        // Pass by virtue of RabbitMQ not firing a 403 reply code
    }

    [Fact]
    public void TestDeleteExchangeWithDefaultExchange()
    {
        _provider = _services.BuildServiceProvider();
        var rabbitAdmin = _provider.GetRabbitAdmin();
        var result = rabbitAdmin.DeleteExchange(string.Empty);
        Assert.True(result);
    }

    [Fact]
    public async Task TestDeleteExchangeWithInternalOption()
    {
        _provider = _services.BuildServiceProvider();
        var rabbitAdmin = _provider.GetRabbitAdmin();
        var exchangeName = "test.exchange.internal";
        AbstractExchange exchange = new DirectExchange(exchangeName)
        {
            IsInternal = true
        };
        rabbitAdmin.DeclareExchange(exchange);
        var exchange2 = await GetExchange(exchangeName);
        Assert.Equal("direct", exchange2.GetValue<string>("type"));

        // TODO: No way to declare internal exchange in .NET, is possible in Java
        // https://github.com/rabbitmq/rabbitmq-dotnet-client/issues/432

        // Assert.True(exchange2.GetValue<bool>("internal"));
    }

    [Fact]
    public void TestDeclareBindingWithDefaultExchangeImplicitBinding()
    {
        _provider = _services.BuildServiceProvider();
        var rabbitAdmin = _provider.GetRabbitAdmin();
        var exchange = new DirectExchange(string.Empty);
        var queueName = "test.queue";
        var queue = new Queue(queueName, false, false, false);
        rabbitAdmin.DeclareQueue(queue);
        var binding = new Binding("mybinding", queueName, DestinationType.QUEUE, exchange.ExchangeName, queueName, null);
        rabbitAdmin.DeclareBinding(binding);

        // Pass by virtue of RabbitMQ not firing a 403 reply code for both exchange and binding declaration
        Assert.True(QueueExists(queue));
    }

    [Fact]
    public void TestSpringWithDefaultExchangeImplicitBinding()
    {
        var exchange = new DirectExchange(string.Empty);
        _services.AddRabbitExchange(exchange);
        var queueName = "test.queue";
        var queue = new Queue(queueName, false, false, false);
        _services.AddRabbitQueue(queue);
        var binding = new Binding("mybinding", queueName, DestinationType.QUEUE, exchange.ExchangeName, queueName, null);
        _services.AddRabbitBinding(binding);
        _provider = _services.BuildServiceProvider();

        var rabbitAdmin = _provider.GetRabbitAdmin();
        rabbitAdmin.Initialize();

        // Pass by virtue of RabbitMQ not firing a 403 reply code for both exchange and binding declaration
        Assert.True(QueueExists(queue));
    }

    [Fact]
    public void TestRemoveBindingWithDefaultExchangeImplicitBinding()
    {
        _provider = _services.BuildServiceProvider();
        var rabbitAdmin = _provider.GetRabbitAdmin();

        var queueName = "test.queue";
        var queue = new Queue(queueName, false, false, false);
        rabbitAdmin.DeclareQueue(queue);

        var binding = new Binding("mybinding", queueName, DestinationType.QUEUE, string.Empty, queueName, null);
        rabbitAdmin.RemoveBinding(binding);

        // Pass by virtue of RabbitMQ not firing a 403 reply code
    }

    [Fact]
    public void TestDeclareBindingWithDefaultExchangeNonImplicitBinding()
    {
        _provider = _services.BuildServiceProvider();
        var rabbitAdmin = _provider.GetRabbitAdmin();

        var exchange = new DirectExchange(string.Empty);

        var queueName = "test.queue";
        var queue = new Queue(queueName, false, false, false);
        rabbitAdmin.DeclareQueue(queue);

        var binding = new Binding("mybinding", queueName, DestinationType.QUEUE, exchange.ExchangeName, "test.routingKey", null);
        var ex = Assert.Throws<RabbitIOException>(() => rabbitAdmin.DeclareBinding(binding));
        Exception cause = ex;
        Exception rootCause = null;
        while (cause != null)
        {
            rootCause = cause;
            cause = cause.InnerException;
        }

        Assert.Contains("code=403", rootCause.Message);
        Assert.Contains("operation not permitted on the default exchange", rootCause.Message);
    }

    [Fact]
    public void TestSpringWithDefaultExchangeNonImplicitBinding()
    {
        var exchange = new DirectExchange(string.Empty);
        _services.AddRabbitExchange(exchange);
        var queueName = "test.queue";
        var queue = new Queue(queueName, false, false, false);
        _services.AddRabbitQueue(queue);
        var binding = new Binding("baz", queueName, DestinationType.QUEUE, exchange.ExchangeName, "test.routingKey", null);
        _services.AddRabbitBinding(binding);
        _provider = _services.BuildServiceProvider();
        var rabbitAdmin = _provider.GetRabbitAdmin();
        rabbitAdmin.RetryTemplate = null;
        var ex = Assert.Throws<RabbitIOException>(() => rabbitAdmin.DeclareBinding(binding));
        Exception cause = ex;
        Exception rootCause = null;
        while (cause != null)
        {
            rootCause = cause;
            cause = cause.InnerException;
        }

        Assert.Contains("code=403", rootCause.Message);
        Assert.Contains("operation not permitted on the default exchange", rootCause.Message);
    }

    [Fact]
    public void TestQueueDeclareBad()
    {
        _provider = _services.BuildServiceProvider();
        var rabbitAdmin = _provider.GetRabbitAdmin();
        rabbitAdmin.IgnoreDeclarationExceptions = true;
        var queue = new AnonymousQueue();
        Assert.Equal(queue.QueueName, rabbitAdmin.DeclareQueue(queue));
        var queue2 = new Queue(queue.QueueName);
        Assert.Null(rabbitAdmin.DeclareQueue(queue2));
        rabbitAdmin.DeleteQueue(queue2.QueueName);
    }

    [Fact]
    public async Task TestDeclareDelayedExchange()
    {
        _provider = _services.BuildServiceProvider();
        var rabbitAdmin = _provider.GetRabbitAdmin();
        var exchange = new DirectExchange("test.delayed.exchange")
        {
            IsDelayed = true
        };
        var queue = new Queue(Guid.NewGuid().ToString(), true, false, false);
        var exchangeName = exchange.ExchangeName;
        var binding = new Binding("baz", queue.QueueName, DestinationType.QUEUE, exchangeName, queue.QueueName, null);
        try
        {
            rabbitAdmin.DeclareExchange(exchange);
        }
        catch (RabbitIOException e)
        {
            if (RabbitUtils.IsExchangeDeclarationFailure(e))
            {
                var inner = e.InnerException;
                if (inner.Message.Contains("exchange type 'x-delayed-message'"))
                {
                    return; // Broker doesn't support?
                }
            }

            throw;
        }

        rabbitAdmin.DeclareQueue(queue);
        rabbitAdmin.DeclareBinding(binding);
        var cf = _provider.GetRabbitConnectionFactory();
        var context = _provider.GetApplicationContext();
        var pp = new TestPostProcessor();
        var template = new RabbitTemplate(cf)
        {
            ReceiveTimeout = 10000
        };
        template.ConvertAndSend(exchangeName, queue.QueueName, "foo", pp);
        var headers = RabbitHeaderAccessor.GetMutableAccessor(new MessageHeaders());
        headers.Delay = 500;
        var send = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("foo")).SetHeaders(headers).Build();
        template.Send(exchangeName, queue.QueueName, send);
        var t1 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var received = template.Receive(queue.QueueName);
        Assert.NotNull(received);
        var delay = received.Headers.ReceivedDelay();
        Assert.NotNull(delay);
        Assert.Equal(500, delay.Value);
        received = template.Receive(queue.QueueName);
        Assert.NotNull(received);
        delay = received.Headers.ReceivedDelay();
        Assert.NotNull(delay);
        Assert.Equal(1000, delay.Value);
        var t2 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var dif = t2 - t1;
        Assert.InRange(dif, 950, 1250);
        var config = await GetExchange(exchangeName);
        Assert.Equal("direct", config.GetValue<string>("x-delayed-type") ?? config.GetValue<string>("arguments:x-delayed-type"));
        Assert.Equal("x-delayed-message", config.GetValue<string>("type"));
    }

    private async Task<IConfiguration> GetExchange(string exchangeName)
    {
        var client = new HttpClient();
        var authToken = Encoding.ASCII.GetBytes("guest:guest");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

        var result = await client.GetAsync($"http://localhost:15672/api/exchanges/%2F/{exchangeName}");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        var config = new ConfigurationBuilder()
            .AddJsonStream(await result.Content.ReadAsStreamAsync())
            .Build();
        return config;
    }

    private bool QueueExists(Queue queue)
    {
        var cf = new RC.ConnectionFactory
        {
            HostName = "localhost"
        };
        var connection = cf.CreateConnection();
        var channel = connection.CreateModel();
        try
        {
            var result = channel.QueueDeclarePassive(queue.QueueName);
            return result != null;
        }
        catch (Exception e)
        {
            return e.Message.Contains("RESOURCE_LOCKED");
        }
        finally
        {
            connection.Close();
        }
    }

    private sealed class TestPostProcessor : IMessagePostProcessor
    {
        public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
        {
            return PostProcessMessage(message);
        }

        public IMessage PostProcessMessage(IMessage message)
        {
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.Delay = 1000;
            return message;
        }
    }
}
