// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.Support;
using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Configuration.Binding;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Test.Core;

[Trait("Category", "Integration")]
public sealed class RabbitAdminIntegrationTest : IDisposable
{
    private readonly ServiceCollection _services;
    private ServiceProvider _provider;

    public RabbitAdminIntegrationTest()
    {
        _services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        _services.AddLogging(b =>
        {
            b.AddDebug();
            b.AddConsole();
        });

        _services.AddSingleton<IConfiguration>(configurationRoot);
        _services.AddRabbitHostingServices();
        _services.AddRabbitConnectionFactory((_, f) => f.Host = "localhost");
        _services.AddRabbitAdmin((_, a) => a.AutoStartup = true);
    }

    [Fact]
    public void TestStartupWithLazyDeclaration()
    {
        var queue = new Queue("test.queue");
        _services.AddRabbitQueue(queue);
        _provider = _services.BuildServiceProvider();

        RabbitAdmin rabbitAdmin = _provider.GetRabbitAdmin();

        // A new connection is initialized so the queue is declared
        Assert.True(rabbitAdmin.DeleteQueue(queue.QueueName));
    }

    [Fact]
    public void TestDoubleDeclarationOfExclusiveQueue()
    {
        _services.AddRabbitConnectionFactory("connectionFactory1", (_, f) =>
        {
            f.Host = "localhost";
        });

        _services.AddRabbitConnectionFactory("connectionFactory2", (_, f) =>
        {
            f.Host = "localhost";
        });

        _provider = _services.BuildServiceProvider();

        var queue = new Queue("test.queue", false, true, true);
        RabbitAdmin rabbitAdmin = _provider.GetRabbitAdmin();
        rabbitAdmin.DeleteQueue(queue.QueueName);

        IApplicationContext context = _provider.GetApplicationContext();
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
    public void TestQueueWithAutoDelete()
    {
        var queue = new Queue("test.queue", false, true, true);
        _services.AddRabbitQueue(queue);
        _provider = _services.BuildServiceProvider();

        RabbitAdmin rabbitAdmin = _provider.GetRabbitAdmin();
        rabbitAdmin.Initialize();
        Assert.True(QueueExists(queue));

        IConnectionFactory cf = _provider.GetRabbitConnectionFactory();
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

        RabbitAdmin rabbitAdmin = _provider.GetRabbitAdmin();
        rabbitAdmin.Initialize();
        Assert.True(QueueExists(queue));

        IConnectionFactory cf = _provider.GetRabbitConnectionFactory();
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
        RabbitAdmin rabbitAdmin = _provider.GetRabbitAdmin();
        string generatedName = rabbitAdmin.DeclareQueue(queue);

        Assert.Equal(string.Empty, queue.QueueName);
        var queueWithGeneratedName = new Queue(generatedName, true, false, true);
        Assert.True(QueueExists(queueWithGeneratedName));

        IConnectionFactory cf = _provider.GetRabbitConnectionFactory();
        cf.Destroy();
        Assert.True(QueueExists(queueWithGeneratedName));

        cf.CreateConnection();
        Assert.True(QueueExists(queueWithGeneratedName));

        Assert.True(rabbitAdmin.DeleteQueue(generatedName));
        Assert.False(QueueExists(queueWithGeneratedName));

        cf.Destroy();
    }

    [Fact]
    public void TestDeleteExchangeWithDefaultExchange()
    {
        _provider = _services.BuildServiceProvider();
        RabbitAdmin rabbitAdmin = _provider.GetRabbitAdmin();
        bool result = rabbitAdmin.DeleteExchange(string.Empty);
        Assert.True(result);
    }

    [Fact]
    public async Task TestDeleteExchangeWithInternalOption()
    {
        _provider = _services.BuildServiceProvider();
        RabbitAdmin rabbitAdmin = _provider.GetRabbitAdmin();
        const string exchangeName = "test.exchange.internal";

        AbstractExchange exchange = new DirectExchange(exchangeName)
        {
            IsInternal = true
        };

        rabbitAdmin.DeclareExchange(exchange);
        IConfiguration exchange2 = await GetExchangeAsync(exchangeName);
        Assert.Equal("direct", exchange2.GetValue<string>("type"));

        rabbitAdmin.DeleteExchange(exchangeName);
    }

    [Fact]
    public void TestDeclareBindingWithDefaultExchangeImplicitBinding()
    {
        _provider = _services.BuildServiceProvider();
        RabbitAdmin rabbitAdmin = _provider.GetRabbitAdmin();
        var exchange = new DirectExchange(string.Empty);
        const string queueName = "test.queue";
        var queue = new Queue(queueName, false, false, false);
        rabbitAdmin.DeclareQueue(queue);
        var binding = new Binding("mybinding", queueName, DestinationType.Queue, exchange.ExchangeName, queueName, null);
        rabbitAdmin.DeclareBinding(binding);

        // Pass by virtue of RabbitMQ not firing a 403 reply code for both exchange and binding declaration
        Assert.True(QueueExists(queue));
    }

    [Fact]
    public void TestSpringWithDefaultExchangeImplicitBinding()
    {
        var exchange = new DirectExchange(string.Empty);
        _services.AddRabbitExchange(exchange);
        const string queueName = "test.queue";
        var queue = new Queue(queueName, false, false, false);
        _services.AddRabbitQueue(queue);
        var binding = new Binding("mybinding", queueName, DestinationType.Queue, exchange.ExchangeName, queueName, null);
        _services.AddRabbitBinding(binding);
        _provider = _services.BuildServiceProvider();

        RabbitAdmin rabbitAdmin = _provider.GetRabbitAdmin();
        rabbitAdmin.Initialize();

        // Pass by virtue of RabbitMQ not firing a 403 reply code for both exchange and binding declaration
        Assert.True(QueueExists(queue));
    }

    [Fact]
    public void TestDeclareBindingWithDefaultExchangeNonImplicitBinding()
    {
        _provider = _services.BuildServiceProvider();
        RabbitAdmin rabbitAdmin = _provider.GetRabbitAdmin();

        var exchange = new DirectExchange(string.Empty);

        const string queueName = "test.queue";
        var queue = new Queue(queueName, false, false, false);
        rabbitAdmin.DeclareQueue(queue);

        var binding = new Binding("mybinding", queueName, DestinationType.Queue, exchange.ExchangeName, "test.routingKey", null);
        var ex = Assert.Throws<RabbitIOException>(() => rabbitAdmin.DeclareBinding(binding));
        Exception cause = ex;
        Exception rootCause = null;

        while (cause != null)
        {
            rootCause = cause;
            cause = cause.InnerException;
        }

        Assert.Contains("code=403", rootCause.Message, StringComparison.Ordinal);
        Assert.Contains("operation not permitted on the default exchange", rootCause.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TestSpringWithDefaultExchangeNonImplicitBinding()
    {
        var exchange = new DirectExchange(string.Empty);
        _services.AddRabbitExchange(exchange);
        const string queueName = "test.queue";
        var queue = new Queue(queueName, false, false, false);
        _services.AddRabbitQueue(queue);
        var binding = new Binding("baz", queueName, DestinationType.Queue, exchange.ExchangeName, "test.routingKey", null);
        _services.AddRabbitBinding(binding);
        _provider = _services.BuildServiceProvider();
        RabbitAdmin rabbitAdmin = _provider.GetRabbitAdmin();
        rabbitAdmin.RetryTemplate = null;
        var ex = Assert.Throws<RabbitIOException>(() => rabbitAdmin.DeclareBinding(binding));
        Exception cause = ex;
        Exception rootCause = null;

        while (cause != null)
        {
            rootCause = cause;
            cause = cause.InnerException;
        }

        Assert.Contains("code=403", rootCause.Message, StringComparison.Ordinal);
        Assert.Contains("operation not permitted on the default exchange", rootCause.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TestQueueDeclareBad()
    {
        _provider = _services.BuildServiceProvider();
        RabbitAdmin rabbitAdmin = _provider.GetRabbitAdmin();
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
        RabbitAdmin rabbitAdmin = _provider.GetRabbitAdmin();

        var exchange = new DirectExchange("test.delayed.exchange")
        {
            IsDelayed = true
        };

        var queue = new Queue(Guid.NewGuid().ToString(), true, false, false);
        string exchangeName = exchange.ExchangeName;
        var binding = new Binding("baz", queue.QueueName, DestinationType.Queue, exchangeName, queue.QueueName, null);

        try
        {
            rabbitAdmin.DeclareExchange(exchange);
        }
        catch (RabbitIOException e)
        {
            if (RabbitUtils.IsExchangeDeclarationFailure(e))
            {
                Exception inner = e.InnerException;

                if (inner.Message.Contains("exchange type 'x-delayed-message'", StringComparison.Ordinal))
                {
                    return; // Broker doesn't support?
                }
            }

            throw;
        }

        rabbitAdmin.DeclareQueue(queue);
        rabbitAdmin.DeclareBinding(binding);
        IConnectionFactory cf = _provider.GetRabbitConnectionFactory();
        var pp = new TestPostProcessor();

        var template = new RabbitTemplate(cf)
        {
            ReceiveTimeout = 10000
        };

        template.ConvertAndSend(exchangeName, queue.QueueName, "foo", pp);
        RabbitHeaderAccessor headers = RabbitHeaderAccessor.GetMutableAccessor(new MessageHeaders());
        headers.Delay = 500;
        IMessage send = MessageBuilder.WithPayload(Encoding.UTF8.GetBytes("foo")).SetHeaders(headers).Build();
        template.Send(exchangeName, queue.QueueName, send);
        long t1 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        IMessage received = template.Receive(queue.QueueName);
        Assert.NotNull(received);
        int? delay = received.Headers.ReceivedDelay();
        Assert.NotNull(delay);
        Assert.Equal(500, delay.Value);
        received = template.Receive(queue.QueueName);
        Assert.NotNull(received);
        delay = received.Headers.ReceivedDelay();
        Assert.NotNull(delay);
        Assert.Equal(1000, delay.Value);
        long t2 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        long dif = t2 - t1;
        Assert.InRange(dif, 950, 1250);
        IConfiguration configuration = await GetExchangeAsync(exchangeName);
        Assert.Equal("direct", configuration.GetValue<string>("x-delayed-type") ?? configuration.GetValue<string>("arguments:x-delayed-type"));
        Assert.Equal("x-delayed-message", configuration.GetValue<string>("type"));
    }

    public void Dispose()
    {
        RabbitAdmin admin = _provider.GetRabbitAdmin();
        admin?.DeleteQueue("test.queue");

        _provider.Dispose();
    }

    private async Task<IConfiguration> GetExchangeAsync(string exchangeName)
    {
        var client = new HttpClient();
        byte[] authToken = Encoding.ASCII.GetBytes("guest:guest");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

        HttpResponseMessage result = await client.GetAsync($"http://localhost:15672/api/exchanges/%2F/{exchangeName}");

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().AddJsonStream(await result.Content.ReadAsStreamAsync()).Build();
        return configurationRoot;
    }

    private bool QueueExists(Queue queue)
    {
        var cf = new RC.ConnectionFactory
        {
            HostName = "localhost"
        };

        RC.IConnection connection = cf.CreateConnection();
        RC.IModel channel = connection.CreateModel();

        try
        {
            RC.QueueDeclareOk result = channel.QueueDeclarePassive(queue.QueueName);
            return result != null;
        }
        catch (Exception e)
        {
            return e.Message.Contains("RESOURCE_LOCKED", StringComparison.Ordinal);
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
            RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.Delay = 1000;
            return message;
        }
    }
}
