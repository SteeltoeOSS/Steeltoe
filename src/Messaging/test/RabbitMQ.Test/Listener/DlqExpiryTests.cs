// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.RabbitMQ.Attributes;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Messaging.RabbitMQ.Listener
{

    [Trait("Category", "Integration")]
    public class DlqExpiryTests : IClassFixture<DlqStartupFixture>
    {
        private DlqStartupFixture fixture;
        private ServiceProvider provider;

        public DlqExpiryTests(DlqStartupFixture fix)
        {
            fixture = fix;
            provider = fixture.Provider;
        }

        [Fact]
        public void TestExpiredDies()
        {
            var template = provider.GetRabbitTemplate();
            var listener = provider.GetService<Listener>();
            var context = provider.GetApplicationContext();
            var queue1 = context.GetService<IQueue>("test.expiry.main");

            template.ConvertAndSend(queue1.QueueName, "foo");
            Assert.True(listener.Latch.Wait(TimeSpan.FromSeconds(10)));
            Thread.Sleep(300);
            Assert.Equal(2, listener.Counter);
        }
    }

    public class Listener
    {
        private int counter;

        public CountdownEvent Latch { get; set; } = new CountdownEvent(2);

        public int Counter { get => counter; set => counter = value; }

        [RabbitListener("test.expiry.main")]
        public Task Listen(string foo)
        {
            Latch.Signal();
            Counter++;
            return Task.FromException(new MessageConversionException("test.expiry"));
        }
    }

    public class DlqStartupFixture : IDisposable
    {
        private readonly IServiceCollection services;

        public ServiceProvider Provider { get; set; }

        public DlqStartupFixture()
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
                config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>()
                    {
                            { "spring:rabbitmq:listener:direct:PossibleAuthenticationFailureFatal", "False" }
                    })
                    .Build();
            }

            services.AddLogging(b =>
            {
                b.SetMinimumLevel(LogLevel.Debug);
                b.AddDebug();
                b.AddConsole();
            });

            services.ConfigureRabbitOptions(config);
            services.AddSingleton<IConfiguration>(config);
            services.AddRabbitHostingServices();
            services.AddRabbitJsonMessageConverter();
            services.AddRabbitMessageHandlerMethodFactory();
            services.AddRabbitListenerEndpointRegistry();
            services.AddRabbitListenerEndpointRegistrar();
            services.AddRabbitListenerAttributeProcessor();
            services.AddRabbitConnectionFactory();
            services.AddRabbitAdmin();

            var mainQueue = new AnonymousQueue("test.expiry.main");
            mainQueue.AddArgument("x-dead-letter-exchange", string.Empty);
            mainQueue.AddArgument("x-dead-letter-routing-key", mainQueue.QueueName);

            var dlq = new AnonymousQueue("test.expiry.dlq");
            dlq.AddArgument("x-dead-letter-exchange", string.Empty);
            dlq.AddArgument("x-dead-letter-routing-key", "test.expiry.main");
            dlq.AddArgument("x-message-ttl", 100);

            services.AddRabbitQueues(mainQueue, dlq);

            // Add default container factory
            services.AddRabbitListenerContainerFactory((p, f) =>
            {
                f.MismatchedQueuesFatal = true;
                f.AcknowledgeMode = Core.AcknowledgeMode.MANUAL;
            });

            // Add dontRequeueFactory container factory
            services.AddRabbitListenerContainerFactory((p, f) =>
            {
                f.ServiceName = "dontRequeueFactory";
                f.MismatchedQueuesFatal = true;
                f.AcknowledgeMode = Core.AcknowledgeMode.MANUAL;
                f.DefaultRequeueRejected = false;
            });

            services.AddSingleton<Listener>();
            services.AddRabbitListeners<Listener>(config);
            services.AddRabbitTemplate();

            return services;
        }

        public void Dispose()
        {
            Provider.Dispose();
        }
    }
}
