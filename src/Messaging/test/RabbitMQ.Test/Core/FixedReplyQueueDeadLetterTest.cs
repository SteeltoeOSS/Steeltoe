// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Listener.Adapters;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using static Steeltoe.Messaging.RabbitMQ.Core.FixedReplyQueueDeadLetterTest;

namespace Steeltoe.Messaging.RabbitMQ.Core
{
    [Trait("Category", "Integration")]
    public class FixedReplyQueueDeadLetterTest : IClassFixture<FixedReplyStartupFixture>
    {
        private readonly ServiceProvider provider;
        private readonly FixedReplyStartupFixture fixture;

        public FixedReplyQueueDeadLetterTest(FixedReplyStartupFixture fix)
        {
            fixture = fix;
            provider = fixture.Provider;
        }

        [Fact]
        public void Test()
        {
            var template = provider.GetRabbitTemplate("fixedReplyQRabbitTemplate");
            var deadListener = provider.GetService<DeadListener>();
            Assert.Null(template.ConvertSendAndReceive<string>("foo"));
            Assert.True(deadListener.Latch.Wait(TimeSpan.FromSeconds(10)));
        }

        [Fact]
        public async Task TestQueueArgs1()
        {
            var config = await GetQueueConfiguration("all.args.1");
            var arguments = config.GetSection("arguments");
            Assert.Equal(1000, arguments.GetValue<int>("x-message-ttl"));
            Assert.Equal(200000, arguments.GetValue<int>("x-expires"));
            Assert.Equal(42, arguments.GetValue<int>("x-max-length"));
            Assert.Equal("reject-publish", arguments.GetValue<string>("x-overflow"));
            Assert.Equal("reply.dlx", arguments.GetValue<string>("x-dead-letter-exchange"));
            Assert.Equal("reply.dlrk", arguments.GetValue<string>("x-dead-letter-routing-key"));
            Assert.Equal(4, arguments.GetValue<int>("x-max-priority"));
            Assert.Equal("lazy", arguments.GetValue<string>("x-queue-mode"));
            Assert.Equal("min-masters", arguments.GetValue<string>("x-queue-master-locator"));
            Assert.True(arguments.GetValue<bool>("x-single-active-consumer"));
        }

        [Fact]
        public async Task TestQueueArgs2()
        {
            var config = await GetQueueConfiguration("all.args.2");
            var arguments = config.GetSection("arguments");
            Assert.Equal(1000, arguments.GetValue<int>("x-message-ttl"));
            Assert.Equal(200000, arguments.GetValue<int>("x-expires"));
            Assert.Equal(42, arguments.GetValue<int>("x-max-length"));
            Assert.Equal(10000, arguments.GetValue<int>("x-max-length-bytes"));
            Assert.Equal("drop-head", arguments.GetValue<string>("x-overflow"));
            Assert.Equal("reply.dlx", arguments.GetValue<string>("x-dead-letter-exchange"));
            Assert.Equal("reply.dlrk", arguments.GetValue<string>("x-dead-letter-routing-key"));
            Assert.Equal(4, arguments.GetValue<int>("x-max-priority"));
            Assert.Equal("lazy", arguments.GetValue<string>("x-queue-mode"));
            Assert.Equal("client-local", arguments.GetValue<string>("x-queue-master-locator"));
        }

        [Fact]
        public async Task TestQueueArgs3()
        {
            var config = await GetQueueConfiguration("all.args.3");
            var arguments = config.GetSection("arguments");
            Assert.Equal(1000, arguments.GetValue<int>("x-message-ttl"));
            Assert.Equal(200000, arguments.GetValue<int>("x-expires"));
            Assert.Equal(42, arguments.GetValue<int>("x-max-length"));
            Assert.Equal(10000, arguments.GetValue<int>("x-max-length-bytes"));
            Assert.Equal("reject-publish", arguments.GetValue<string>("x-overflow"));
            Assert.Equal("reply.dlx", arguments.GetValue<string>("x-dead-letter-exchange"));
            Assert.Equal("reply.dlrk", arguments.GetValue<string>("x-dead-letter-routing-key"));
            Assert.Equal(4, arguments.GetValue<int>("x-max-priority"));
            Assert.Equal("lazy", arguments.GetValue<string>("x-queue-mode"));
            Assert.Equal("random", arguments.GetValue<string>("x-queue-master-locator"));

            var exchConfig = await GetExchangeConfiguration("dlx.test.requestEx");
            var arguments2 = exchConfig.GetSection("arguments");
            Assert.Equal("alternate", arguments2.GetValue<string>("alternate-exchange"));
        }

        [Fact]
        public async Task TestQuorumArgs()
        {
            var config = await GetQueueConfiguration("test.quorum");
            var arguments = config.GetSection("arguments");
            Assert.Equal(10, arguments.GetValue<int>("x-delivery-limit"));
            Assert.Equal("quorum", arguments.GetValue<string>("x-queue-type"));
        }

        private async Task<IConfiguration> GetQueueConfiguration(string queueName)
        {
            var client = new HttpClient();
            var authToken = Encoding.ASCII.GetBytes("guest:guest");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

            var result = await client.GetAsync("http://localhost:15672/api/queues/%3F/" + queueName);
            var n = 0;
            while (n++ < 100 && result.StatusCode == HttpStatusCode.NotFound)
            {
                await Task.Delay(100);
                result = await client.GetAsync("http://localhost:15672/api/queues/%2F/" + queueName);
            }

            Assert.True(n < 100);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var config = new ConfigurationBuilder()
                .AddJsonStream(await result.Content.ReadAsStreamAsync())
                .Build();
            return config;
        }

        private async Task<IConfiguration> GetExchangeConfiguration(string exchangeName)
        {
            var client = new HttpClient();
            var authToken = Encoding.ASCII.GetBytes("guest:guest");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));

            var result = await client.GetAsync("http://localhost:15672/api/exchanges/%2F/" + exchangeName);

            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
            var config = new ConfigurationBuilder()
                .AddJsonStream(await result.Content.ReadAsStreamAsync())
                .Build();
            return config;
        }

        public class FixedReplyStartupFixture : IDisposable
        {
            private readonly IServiceCollection services;

            public ServiceProvider Provider { get; set; }

            public FixedReplyStartupFixture()
            {
                services = CreateContainer();
                Provider = services.BuildServiceProvider();
                Provider.GetRequiredService<IHostedService>().StartAsync(default).Wait();
            }

            public ServiceCollection CreateContainer(IConfiguration config = null)
            {
                var services = new ServiceCollection();
                if (config == null)
                {
                    config = new ConfigurationBuilder().Build();
                }

                services.AddLogging(b =>
                {
                    b.AddDebug();
                    b.AddConsole();
                });

                services.AddSingleton<IConfiguration>(config);
                services.AddRabbitHostingServices();
                services.AddRabbitDefaultMessageConverter();

                // services.AddRabbitListenerEndpointRegistry();
                // services.AddRabbitListenerEndpointRegistrar();
                // services.AddRabbitListenerAttributeProcessor();
                services.AddRabbitConnectionFactory();

                var requestQueue = QueueBuilder.NonDurable("dlx.test.requestQ")
                    .AutoDelete()
                    .Build();
                services.AddRabbitQueue(requestQueue);

                var replyQueue = QueueBuilder.NonDurable("dlx.test.replyQ")
                    .AutoDelete()
                    .WithArgument("x-dead-letter-exchange", "reply.dlx")
                    .Build();
                services.AddRabbitQueue(replyQueue);

                var dlq = QueueBuilder.NonDurable("dlx.test.DLQ")
                    .AutoDelete()
                    .Build();
                services.AddRabbitQueue(dlq);

                var ex = ExchangeBuilder.DirectExchange("dlx.test.requestEx")
                    .Durable(false)
                    .AutoDelete()
                    .Alternate("alternate")
                    .Build() as DirectExchange;
                services.AddRabbitExchange(ex);

                var dlx = new DirectExchange("reply.dlx", false, true);
                services.AddRabbitExchange(dlx);

                var allArgs1 = QueueBuilder.NonDurable("all.args.1")
                    .TTL(1000)
                    .Expires(200000)
                    .MaxLength(42)
                    .MaxLengthBytes(10000)
                    .Overflow(QueueBuilder.OverFlow.RejectPublish)
                    .DeadLetterExchange("reply.dlx")
                    .DeadLetterRoutingKey("reply.dlrk")
                    .MaxPriority(4)
                    .Lazy()
                    .Masterlocator(QueueBuilder.MasterLocator.MinMasters)
                    .SingleActiveConsumer()
                    .Build();
                services.AddRabbitQueue(allArgs1);

                var allArgs2 = QueueBuilder.NonDurable("all.args.2")
                    .TTL(1000)
                    .Expires(200000)
                    .MaxLength(42)
                    .MaxLengthBytes(10000)
                    .Overflow(QueueBuilder.OverFlow.DropHead)
                    .DeadLetterExchange("reply.dlx")
                    .DeadLetterRoutingKey("reply.dlrk")
                    .MaxPriority(4)
                    .Lazy()
                    .Masterlocator(QueueBuilder.MasterLocator.ClientLocal)
                    .Build();
                services.AddRabbitQueue(allArgs2);

                var allArgs3 = QueueBuilder.NonDurable("all.args.3")
                    .TTL(1000)
                    .Expires(200000)
                    .MaxLength(42)
                    .MaxLengthBytes(10000)
                    .Overflow(QueueBuilder.OverFlow.RejectPublish)
                    .DeadLetterExchange("reply.dlx")
                    .DeadLetterRoutingKey("reply.dlrk")
                    .MaxPriority(4)
                    .Lazy()
                    .Masterlocator(QueueBuilder.MasterLocator.Random)
                    .Build();
                services.AddRabbitQueue(allArgs3);

                var quorum = QueueBuilder.Durable("test.quorum")
                    .Quorum()
                    .DeliveryLimit(10)
                    .Build();
                services.AddRabbitQueue(quorum);

                var dlBinding = BindingBuilder.Bind(dlq)
                    .To(dlx)
                    .With(replyQueue.QueueName);
                services.AddRabbitBinding(dlBinding);

                var binding = BindingBuilder.Bind(requestQueue)
                    .To(ex)
                    .With("dlx.reply.test");
                services.AddRabbitBinding(binding);

                // Add a container "replyListenerContainer"
                services.AddSingleton<ILifecycle>((p) =>
                {
                    var context = p.GetApplicationContext();
                    var factory = p.GetService<IConnectionFactory>();
                    var logFactory = p.GetService<ILoggerFactory>();
                    var rqueue = p.GetRabbitQueue(replyQueue.QueueName);
                    var template = p.GetRabbitTemplate("fixedReplyQRabbitTemplate");
                    var container = new DirectMessageListenerContainer(context, factory, "replyListenerContainer", logFactory);
                    container.SetQueues(rqueue);
                    container.MessageListener = template;
                    return container;
                });

                // Add a container "serviceListenerContainer"
                services.AddSingleton<ILifecycle>((p) =>
                {
                    var context = p.GetApplicationContext();
                    var factory = p.GetService<IConnectionFactory>();
                    var logFactory = p.GetService<ILoggerFactory>();
                    var rqueue = p.GetRabbitQueue(requestQueue.QueueName);
                    var container = new DirectMessageListenerContainer(context, factory, "serviceListenerContainer", logFactory);
                    container.SetQueues(rqueue);
                    var pojoListener = p.GetService<PojoListener>();
                    container.MessageListener = new MessageListenerAdapter(context, pojoListener, p.GetService<ILogger<MessageListenerAdapter>>());
                    return container;
                });

                // Add a container "dlListenerContainer"
                services.AddSingleton<ILifecycle>((p) =>
                {
                    var context = p.GetApplicationContext();
                    var factory = p.GetService<IConnectionFactory>();
                    var logFactory = p.GetService<ILoggerFactory>();
                    var q = p.GetRabbitQueue(dlq.QueueName);
                    var container = new DirectMessageListenerContainer(context, factory, "dlListenerContainer", logFactory);
                    container.SetQueues(q);
                    var deadListener = p.GetService<DeadListener>();
                    container.MessageListener = new MessageListenerAdapter(context, deadListener, p.GetService<ILogger<MessageListenerAdapter>>());
                    return container;
                });

                services.AddRabbitAdmin();

                // Add RabbitTemplate named fixedReplyQRabbitTemplate
                services.AddRabbitTemplate((p, t) =>
                {
                    var context = p.GetService<IApplicationContext>();
                    t.DefaultSendDestination = new RabbitDestination(ex.ExchangeName, "dlx.reply.test");
                    t.ReplyAddress = replyQueue.QueueName;
                    t.ReplyTimeout = 1;
                    t.ServiceName = "fixedReplyQRabbitTemplate";
                });

                services.AddSingleton<DeadListener>();
                services.AddSingleton<PojoListener>();

                return services;
            }

            public void Dispose()
            {
                var admin = Provider.GetRabbitAdmin();
                admin.DeleteQueue("all.args.1");
                admin.DeleteQueue("all.args.2");
                admin.DeleteQueue("all.args.3");
                admin.DeleteQueue("test.quorum");
                Provider.Dispose();
            }
        }

        public class PojoListener
        {
            public string HandleMessage(string foo)
            {
                Thread.Sleep(500);
                return foo.ToUpper();
            }
        }

        public class DeadListener
        {
            public CountdownEvent Latch { get; set; } = new CountdownEvent(1);

            public void HandleMessage(string foo)
            {
                Latch.Signal();
            }
        }
    }
}
