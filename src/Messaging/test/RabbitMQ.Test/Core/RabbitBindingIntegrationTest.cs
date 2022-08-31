// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.RabbitMQ.Configuration;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.RabbitMQ.Support.Converter;
using Steeltoe.Messaging.RabbitMQ.Util;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Core;

[Trait("Category", "Integration")]
public sealed class RabbitBindingIntegrationTest : IDisposable
{
    private const string QueueName = "test.queue.RabbitBindingIntegrationTests";
    private readonly Queue _queue = new(QueueName);
    private readonly ServiceCollection _services;
    private ServiceProvider _provider;

    public RabbitBindingIntegrationTest()
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
        _services.AddRabbitDefaultMessageConverter();
        _services.AddRabbitConnectionFactory((_, f) => f.Host = "localhost");
        _services.AddRabbitAdmin((_, a) => a.AutoStartup = true);
        _services.AddRabbitTemplate();
        _services.AddRabbitQueue(_queue);
    }

    [Fact]
    public void TestSendAndReceiveWithTopicSingleCallback()
    {
        _provider = _services.BuildServiceProvider();
        RabbitAdmin admin = _provider.GetRabbitAdmin();
        var exchange = new TopicExchange("topic");
        admin.DeclareExchange(exchange);
        RabbitTemplate template = _provider.GetRabbitTemplate();
        template.DefaultSendDestination = new RabbitDestination(exchange.ExchangeName, string.Empty);
        IBinding binding = BindingBuilder.Bind(_queue).To(exchange).With("*.end");
        admin.DeclareBinding(binding);

        try
        {
            template.Execute(_ =>
            {
                BlockingQueueConsumer consumer = CreateConsumer(template.ConnectionFactory);
                string tag = consumer.GetConsumerTags()[0];
                Assert.NotNull(tag);
                template.ConvertAndSend("foo", "message");

                try
                {
                    string result = GetResult(consumer, false);
                    Assert.Null(result);
                    template.ConvertAndSend("foo.end", "message");
                    result = GetResult(consumer, true);
                    Assert.Equal("message", result);
                }
                finally
                {
                    consumer.Channel.BasicCancel(tag);
                }
            });
        }
        finally
        {
            Assert.True(admin.DeleteExchange("topic"));
        }
    }

    [Fact]
    public void TestSendAndReceiveWithNonDefaultExchange()
    {
        _provider = _services.BuildServiceProvider();
        RabbitAdmin admin = _provider.GetRabbitAdmin();
        var exchange = new TopicExchange("topic");
        admin.DeclareExchange(exchange);
        RabbitTemplate template = _provider.GetRabbitTemplate();
        IBinding binding = BindingBuilder.Bind(_queue).To(exchange).With("*.end");
        admin.DeclareBinding(binding);

        try
        {
            template.Execute(_ =>
            {
                BlockingQueueConsumer consumer = CreateConsumer(template.ConnectionFactory);
                string tag = consumer.GetConsumerTags()[0];
                Assert.NotNull(tag);
                template.ConvertAndSend("topic", "foo", "message");

                try
                {
                    string result = GetResult(consumer, false);
                    Assert.Null(result);
                    template.ConvertAndSend("topic", "foo.end", "message");
                    result = GetResult(consumer, true);
                    Assert.Equal("message", result);
                }
                finally
                {
                    consumer.Channel.BasicCancel(tag);
                }
            });
        }
        finally
        {
            Assert.True(admin.DeleteExchange("topic"));
        }
    }

    [Fact]
    public void TestSendAndReceiveWithTopicConsumeInBackground()
    {
        _provider = _services.BuildServiceProvider();
        RabbitAdmin admin = _provider.GetRabbitAdmin();
        var exchange = new TopicExchange("topic");
        admin.DeclareExchange(exchange);
        RabbitTemplate template = _provider.GetRabbitTemplate();
        template.DefaultSendDestination = new RabbitDestination(exchange.ExchangeName, string.Empty);
        IBinding binding = BindingBuilder.Bind(_queue).To(exchange).With("*.end");
        admin.DeclareBinding(binding);

        var cachingConnectionFactory = new CachingConnectionFactory("localhost");

        var template1 = new RabbitTemplate(cachingConnectionFactory)
        {
            DefaultSendDestination = new RabbitDestination(exchange.ExchangeName, string.Empty)
        };

        BlockingQueueConsumer consumer = template1.Execute(_ =>
        {
            BlockingQueueConsumer consumer1 = CreateConsumer(template1.ConnectionFactory);
            string tag = consumer1.GetConsumerTags()[0];
            Assert.NotNull(tag);

            return consumer1;
        });

        template1.ConvertAndSend("foo", "message");
        string result = GetResult(consumer, false);
        Assert.Null(result);

        template1.ConvertAndSend("foo.end", "message");
        result = GetResult(consumer, true);
        Assert.Equal("message", result);

        consumer.Stop();
        admin.DeleteExchange("topic");
        cachingConnectionFactory.Destroy();
    }

    [Fact]
    public void TestSendAndReceiveWithTopicTwoCallbacks()
    {
        _provider = _services.BuildServiceProvider();
        RabbitAdmin admin = _provider.GetRabbitAdmin();
        var exchange = new TopicExchange("topic");
        admin.DeclareExchange(exchange);
        IBinding binding = BindingBuilder.Bind(_queue).To(exchange).With("*.end");
        admin.DeclareBinding(binding);

        RabbitTemplate template = _provider.GetRabbitTemplate();
        template.DefaultSendDestination = new RabbitDestination(exchange.ExchangeName, string.Empty);

        try
        {
            template.Execute(_ =>
            {
                BlockingQueueConsumer consumer = CreateConsumer(template.ConnectionFactory);
                string tag = consumer.GetConsumerTags()[0];
                Assert.NotNull(tag);

                try
                {
                    template.ConvertAndSend("foo", "message");
                    string result = GetResult(consumer, false);
                    Assert.Null(result);
                }
                finally
                {
                    consumer.Stop();
                }
            });

            template.Execute(_ =>
            {
                BlockingQueueConsumer consumer = CreateConsumer(template.ConnectionFactory);
                string tag = consumer.GetConsumerTags()[0];
                Assert.NotNull(tag);

                try
                {
                    template.ConvertAndSend("foo.end", "message");
                    string result = GetResult(consumer, true);
                    Assert.Equal("message", result);
                }
                finally
                {
                    consumer.Stop();
                }
            });
        }
        finally
        {
            Assert.True(admin.DeleteExchange("topic"));
        }
    }

    [Fact]
    public void TestSendAndReceiveWithFanOut()
    {
        _provider = _services.BuildServiceProvider();
        RabbitAdmin admin = _provider.GetRabbitAdmin();
        var exchange = new FanOutExchange("fanout");
        admin.DeclareExchange(exchange);
        admin.DeclareBinding(BindingBuilder.Bind(_queue).To(exchange));

        RabbitTemplate template = _provider.GetRabbitTemplate();
        template.DefaultSendDestination = new RabbitDestination(exchange.ExchangeName, string.Empty);

        try
        {
            template.Execute(_ =>
            {
                BlockingQueueConsumer consumer = CreateConsumer(template.ConnectionFactory);
                string tag = consumer.GetConsumerTags()[0];
                Assert.NotNull(tag);

                try
                {
                    template.ConvertAndSend("message");
                    string result = GetResult(consumer, true);
                    Assert.Equal("message", result);
                }
                finally
                {
                    consumer.Stop();
                }
            });
        }
        finally
        {
            admin.DeleteExchange("fanout");
        }
    }

    public void Dispose()
    {
        RabbitAdmin admin = _provider.GetRabbitAdmin();
        admin.DeleteQueue(QueueName);
        _provider.Dispose();
    }

    private string GetResult(BlockingQueueConsumer consumer, bool expected)
    {
        IMessage response = consumer.NextMessage(expected ? 2000 : 100);

        if (response == null)
        {
            return null;
        }

        return new SimpleMessageConverter().FromMessage<string>(response);
    }

    private BlockingQueueConsumer CreateConsumer(IConnectionFactory connectionFactory)
    {
        var consumer = new BlockingQueueConsumer(connectionFactory, new DefaultMessageHeadersConverter(), new ActiveObjectCounter<BlockingQueueConsumer>(),
            AcknowledgeMode.Auto, true, 1, null, _queue.QueueName);

        consumer.Start();

        int n = 0;

        while (n++ < 100)
        {
            if (consumer.CurrentConsumers().Count > 0)
            {
                break;
            }

            Thread.Sleep(100);
        }

        return consumer;
    }
}
