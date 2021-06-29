// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Support;
using Steeltoe.Messaging.RabbitMQ.Util;
using System;
using System.Threading;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Core
{
    [Trait("Category", "Integration")]
    public class RabbitBindingIntegrationTest : IDisposable
    {
        private const string QueueName = "test.queue.RabbitBindingIntegrationTests";
        private readonly Queue queue = new Queue(QueueName);
        private readonly ServiceCollection services;
        private ServiceProvider provider;

        public RabbitBindingIntegrationTest()
        {
            services = new ServiceCollection();
            var config = new ConfigurationBuilder().Build();
            services.AddLogging(b =>
            {
                b.AddDebug();
                b.AddConsole();
            });

            services.AddSingleton<IConfiguration>(config);
            services.AddRabbitHostingServices();
            services.AddRabbitDefaultMessageConverter();
            services.AddRabbitConnectionFactory((p, f) => f.Host = "localhost");
            services.AddRabbitAdmin((p, a) => a.AutoStartup = true);
            services.AddRabbitTemplate();
            services.AddRabbitQueue(queue);
        }

        public void Dispose()
        {
            var admin = provider.GetRabbitAdmin();
            admin.DeleteQueue(QueueName);
            provider.Dispose();
        }

        [Fact]
        public void TestSendAndReceiveWithTopicSingleCallback()
        {
            provider = services.BuildServiceProvider();
            var admin = provider.GetRabbitAdmin();
            var exchange = new TopicExchange("topic");
            admin.DeclareExchange(exchange);
            var template = provider.GetRabbitTemplate();
            template.DefaultSendDestination = new RabbitDestination(exchange.ExchangeName, string.Empty);
            var binding = BindingBuilder.Bind(queue).To(exchange).With("*.end");
            admin.DeclareBinding(binding);
            try
            {
                template.Execute(c =>
                {
                    var consumer = CreateConsumer(template.ConnectionFactory);
                    var tag = consumer.GetConsumerTags()[0];
                    Assert.NotNull(tag);
                    template.ConvertAndSend("foo", "message");
                    try
                    {
                        var result = GetResult(consumer, false);
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
            provider = services.BuildServiceProvider();
            var admin = provider.GetRabbitAdmin();
            var exchange = new TopicExchange("topic");
            admin.DeclareExchange(exchange);
            var template = provider.GetRabbitTemplate();
            var binding = BindingBuilder.Bind(queue).To(exchange).With("*.end");
            admin.DeclareBinding(binding);
            try
            {
                template.Execute(c =>
                {
                    var consumer = CreateConsumer(template.ConnectionFactory);
                    var tag = consumer.GetConsumerTags()[0];
                    Assert.NotNull(tag);
                    template.ConvertAndSend("topic", "foo", "message");
                    try
                    {
                        var result = GetResult(consumer, false);
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
            provider = services.BuildServiceProvider();
            var admin = provider.GetRabbitAdmin();
            var exchange = new TopicExchange("topic");
            admin.DeclareExchange(exchange);
            var template = provider.GetRabbitTemplate();
            template.DefaultSendDestination = new RabbitDestination(exchange.ExchangeName, string.Empty);
            var binding = BindingBuilder.Bind(queue).To(exchange).With("*.end");
            admin.DeclareBinding(binding);

            var cachingConnectionFactory = new CachingConnectionFactory("localhost");
            var template1 = new RabbitTemplate(cachingConnectionFactory)
            {
                DefaultSendDestination = new RabbitDestination(exchange.ExchangeName, string.Empty)
            };

            var consumer = template1.Execute(channel =>
            {
                var consumer1 = CreateConsumer(template1.ConnectionFactory);
                var tag = consumer1.GetConsumerTags()[0];
                Assert.NotNull(tag);

                return consumer1;
            });

            template1.ConvertAndSend("foo", "message");
            var result = GetResult(consumer, false);
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
            provider = services.BuildServiceProvider();
            var admin = provider.GetRabbitAdmin();
            var exchange = new TopicExchange("topic");
            admin.DeclareExchange(exchange);
            var binding = BindingBuilder.Bind(queue).To(exchange).With("*.end");
            admin.DeclareBinding(binding);

            var template = provider.GetRabbitTemplate();
            template.DefaultSendDestination = new RabbitDestination(exchange.ExchangeName, string.Empty);
            try
            {
                template.Execute(c =>
                {
                    var consumer = CreateConsumer(template.ConnectionFactory);
                    var tag = consumer.GetConsumerTags()[0];
                    Assert.NotNull(tag);
                    try
                    {
                        template.ConvertAndSend("foo", "message");
                        var result = GetResult(consumer, false);
                        Assert.Null(result);
                    }
                    finally
                    {
                        consumer.Stop();
                    }
                });

                template.Execute(c =>
                {
                    var consumer = CreateConsumer(template.ConnectionFactory);
                    var tag = consumer.GetConsumerTags()[0];
                    Assert.NotNull(tag);
                    try
                    {
                        template.ConvertAndSend("foo.end", "message");
                        var result = GetResult(consumer, true);
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
        public void TestSendAndReceiveWithFanout()
        {
            provider = services.BuildServiceProvider();
            var admin = provider.GetRabbitAdmin();
            var exchange = new FanoutExchange("fanout");
            admin.DeclareExchange(exchange);
            admin.DeclareBinding(BindingBuilder.Bind(queue).To(exchange));

            var template = provider.GetRabbitTemplate();
            template.DefaultSendDestination = new RabbitDestination(exchange.ExchangeName, string.Empty);
            try
            {
                template.Execute(channel =>
                {
                    var consumer = CreateConsumer(template.ConnectionFactory);
                    var tag = consumer.GetConsumerTags()[0];
                    Assert.NotNull(tag);

                    try
                    {
                        template.ConvertAndSend("message");
                        var result = GetResult(consumer, true);
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

        private string GetResult(BlockingQueueConsumer consumer, bool expected)
        {
            var response = consumer.NextMessage(expected ? 2000 : 100);
            if (response == null)
            {
                return null;
            }

            return new RabbitMQ.Support.Converter.SimpleMessageConverter().FromMessage<string>(response);
        }

        private BlockingQueueConsumer CreateConsumer(IConnectionFactory connectionFactory)
        {
            var consumer = new BlockingQueueConsumer(
                connectionFactory,
                new DefaultMessageHeadersConverter(),
                new ActiveObjectCounter<BlockingQueueConsumer>(),
                AcknowledgeMode.AUTO,
                true,
                1,
                null,
                queue.QueueName);
            consumer.Start();

            var n = 0;
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
}
