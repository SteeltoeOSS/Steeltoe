// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Util;
using Steeltoe.Messaging.RabbitMQ.Config;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Exceptions;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Attributes.AsyncListenerTest;

namespace Steeltoe.Messaging.RabbitMQ.Attributes;

[Trait("Category", "Integration")]
public class AsyncListenerTest : IClassFixture<StartupFixture>
{
    private readonly ServiceProvider _provider;
    private readonly StartupFixture _fixture;

    public AsyncListenerTest(StartupFixture fix)
    {
        _fixture = fix;
        _provider = _fixture.Provider;
    }

    [Fact]
    public async Task TestAsyncListener()
    {
        var template = _provider.GetRabbitTemplate();
        var context = _provider.GetApplicationContext();
        var queue1 = context.GetService<IQueue>("queue1");
        var reply = template.ConvertSendAndReceive<string>(queue1.QueueName, "foo");
        Assert.Equal("FOO", reply);

        var reply2 = await template.ConvertSendAndReceiveAsync<string>(queue1.QueueName, "foo");
        Assert.Equal("FOO", reply2);
        var pp = template.AfterReceivePostProcessors[0] as TemplateAfterReceivePostProcessor;
        Assert.Equal("System.String", pp.TypeId);

        var queue2 = context.GetService<IQueue>("queue2");
        var reply3 = template.ConvertSendAndReceive<string>(queue2.QueueName, "foo");
        Assert.Equal("FOO", reply3);
        Assert.Equal("System.String", pp.TypeId);

        var queue3 = context.GetService<IQueue>("queue3");
        var reply4 = template.ConvertSendAndReceive<List<string>>(queue3.QueueName, "foo");
        Assert.Equal("FOO", reply4[0]);
        Assert.Equal("System.Collections.Generic.List`1", pp.TypeId);
        Assert.Equal("System.String", pp.ContentTypeId);

        var queue4 = context.GetService<IQueue>("queue4");
        template.ConvertAndSend(queue4.QueueName, "foo");
        var listener = _provider.GetService<Listener>();
        Assert.True(listener.Latch4.Wait(TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void TestRouteToDlq()
    {
        var template = _provider.GetRabbitTemplate();
        var context = _provider.GetApplicationContext();
        var queue5 = context.GetService<IQueue>("queue5");
        var listener = _provider.GetService<Listener>();

        template.ConvertAndSend(queue5.QueueName, "foo");
        Assert.True(listener.Latch5.Wait(TimeSpan.FromSeconds(10)));

        var queue6 = context.GetService<IQueue>("queue6");
        template.ConvertAndSend(queue6.QueueName, "foo");
        Assert.True(listener.Latch6.Wait(TimeSpan.FromSeconds(10)));
    }

    [Fact]
    public void TestOverrideDoNotRequeue()
    {
        var template = _provider.GetRabbitTemplate();
        var context = _provider.GetApplicationContext();
        var queue7 = context.GetService<IQueue>("queue7");
        Assert.Equal("listen7", template.ConvertSendAndReceive<string>(queue7.QueueName, "foo"));
    }

    [Fact]
    public void TestAuthByProps()
    {
        var registry = _provider.GetRequiredService<IRabbitListenerEndpointRegistry>() as RabbitListenerEndpointRegistry;
        var container = registry.GetListenerContainer("foo") as DirectMessageListenerContainer;
        Assert.False(container.PossibleAuthenticationFailureFatal);
    }

    [Fact]
    public async Task TestAsyncListenerErrorHandler()
    {
        var template = _provider.GetRabbitTemplate();
        var context = _provider.GetApplicationContext();
        var queueAsyncErrorHandler = context.GetService<IQueue>("queueAsyncErrorHandler");
        var reply = await template.ConvertSendAndReceiveAsync<string>(queueAsyncErrorHandler.QueueName, "foo");
        Assert.Equal($"{nameof(CustomListenerErrorHandler)} handled/processed", reply);
    }

    public sealed class StartupFixture : IDisposable
    {
        private readonly IServiceCollection _services;

        public ServiceProvider Provider { get; set; }

        public StartupFixture()
        {
            _services = CreateContainer();
            Provider = _services.BuildServiceProvider();
            Provider.GetRequiredService<IHostedService>().StartAsync(default).Wait();
        }

        public ServiceCollection CreateContainer(IConfiguration config = null)
        {
            var services = new ServiceCollection();
            config ??= new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "spring:rabbitmq:listener:direct:PossibleAuthenticationFailureFatal", "False" }
                })
                .Build();

            services.AddLogging(b =>
            {
                b.SetMinimumLevel(LogLevel.Debug);
                b.AddDebug();
                b.AddConsole();
            });

            services.ConfigureRabbitOptions(config);
            services.AddSingleton(config);
            services.AddRabbitHostingServices();
            services.AddRabbitJsonMessageConverter();
            services.AddRabbitMessageHandlerMethodFactory();
            services.AddRabbitListenerEndpointRegistry();
            services.AddRabbitListenerEndpointRegistrar();
            services.AddRabbitListenerAttributeProcessor();
            services.AddRabbitConnectionFactory();
            services.AddRabbitAdmin();
            services.AddRabbitTemplate((_, t) =>
            {
                t.SetAfterReceivePostProcessors(new TemplateAfterReceivePostProcessor());
            });

            var queue5Dlq = new AnonymousQueue("queue5DLQ");
            var queue6Dlq = new AnonymousQueue("queue6DLQ");
            var queue1 = new AnonymousQueue("queue1");
            var queue2 = new AnonymousQueue("queue2");
            var queue3 = new AnonymousQueue("queue3");
            var queue4 = new AnonymousQueue("queue4");
            var queue5 = new AnonymousQueue("queue5");
            var queueAsyncErrorHandler = new AnonymousQueue("queueAsyncErrorHandler");
            queue5.Arguments.Add("x-dead-letter-exchange", string.Empty);
            queue5.Arguments.Add("x-dead-letter-routing-key", queue5Dlq.QueueName);
            var queue6 = new AnonymousQueue("queue6");
            queue6.Arguments.Add("x-dead-letter-exchange", string.Empty);
            queue6.Arguments.Add("x-dead-letter-routing-key", queue6Dlq.QueueName);
            var queue7 = new AnonymousQueue("queue7");
            services.AddRabbitQueues(queue1, queue2, queue3, queue4, queue5, queue6, queue5Dlq, queue6Dlq, queue7, queueAsyncErrorHandler);

            // Add default container factory
            services.AddRabbitListenerContainerFactory((_, f) =>
            {
                f.MismatchedQueuesFatal = true;
                f.AcknowledgeMode = AcknowledgeMode.Manual;
            });

            // Add doNotRequeueFactory container factory
            services.AddRabbitListenerContainerFactory((_, f) =>
            {
                f.ServiceName = "doNotRequeueFactory";
                f.MismatchedQueuesFatal = true;
                f.AcknowledgeMode = AcknowledgeMode.Manual;
                f.DefaultRequeueRejected = false;
            });

            services.AddSingleton<Listener>();
            services.AddRabbitListeners<Listener>(config);
            services.AddRabbitListenerErrorHandler<CustomListenerErrorHandler>(nameof(CustomListenerErrorHandler));

            return services;
        }

        public void Dispose()
        {
            Provider.Dispose();
        }
    }

    public class Listener
    {
        public AtomicBoolean FooFirst { get; set; } = new (true);

        public AtomicBoolean BarFirst { get; set; } = new (true);

        public CountdownEvent Latch4 { get; set; } = new (1);

        public CountdownEvent Latch5 { get; set; } = new (1);

        public CountdownEvent Latch6 { get; set; } = new (1);

        public AtomicBoolean First7 { get; set; } = new (true);

        [RabbitListener("queue1", Id = "foo")]
        public Task<string> Listen1(string foo)
        {
            if (FooFirst.GetAndSet(false))
            {
                return Task.FromException<string>(new Exception("Future.exception"));
            }
            else
            {
                return Task.FromResult(foo.ToUpper());
            }
        }

        [RabbitListener("queue2", Id = "bar")]
        public Task<string> Listen2(string foo)
        {
            if (BarFirst.GetAndSet(false))
            {
                return Task.FromException<string>(new Exception("Mono.error()"));
            }
            else
            {
                return Task.FromResult(foo.ToUpper());
            }
        }

        [RabbitListener("queue3", Id = "baz")]
        public Task<List<string>> Listen3(string foo)
        {
            return Task.FromResult(new List<string> { foo.ToUpper() });
        }

        [RabbitListener("queue4", Id = "qux")]
        public Task Listen4(string foo)
        {
            Latch4.Signal();
            return Task.CompletedTask;
        }

        [RabbitListener("queue5", Id = "fiz")]
        public Task Listen5(string foo)
        {
            return Task.FromException(new RabbitRejectAndDoNotRequeueException("asyncToDLQ"));
        }

        [RabbitListener("queue5DLQ", Id = "buz")]
        public void Listen5Dlq(string foo)
        {
            Latch5.Signal();
        }

        [RabbitListener("queue6", Id = "fix", ContainerFactory = "doNotRequeueFactory")]
        public Task Listen6(string foo)
        {
            return Task.FromException(new InvalidOperationException("asyncDefaultToDLQ"));
        }

        [RabbitListener("queue6DLQ", Id = "fox")]
        public void Listen6Dlq(string foo)
        {
            Latch6.Signal();
        }

        [RabbitListener("queue7", Id = "overrideFactoryRequeue", ContainerFactory = "doNotRequeueFactory")]
        public Task<string> Listen7(string foo)
        {
            if (First7.CompareAndSet(true, false))
            {
                return Task.FromException<string>(new ImmediateRequeueException("asyncOverrideDefaultToDLQ"));
            }
            else
            {
                return Task.FromResult("listen7");
            }
        }

        [RabbitListener(Queue = "queueAsyncErrorHandler", Id = "asycErrorHandler", ErrorHandler = nameof(CustomListenerErrorHandler))]
        public async Task<string> HandleMessage(string msg)
        {
            await Task.Run(() => Console.WriteLine("Running Listener"));

            throw new Exception($"Test {nameof(Listener)} exception");
        }
    }

    public class TemplateAfterReceivePostProcessor : IMessagePostProcessor
    {
        public object TypeId { get; set; }

        public object ContentTypeId { get; set; }

        public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
        {
            TypeId = message.Headers.Get<object>("__TypeId__");
            ContentTypeId = message.Headers.Get<object>("__ContentTypeId__");
            return message;
        }

        public IMessage PostProcessMessage(IMessage message)
        {
            TypeId = message.Headers.Get<object>("__TypeId__");
            ContentTypeId = message.Headers.Get<object>("__ContentTypeId__");
            return message;
        }
    }

    public class CustomListenerErrorHandler : IRabbitListenerErrorHandler
    {
        public string ServiceName { get; set; } = nameof(CustomListenerErrorHandler);

        public object HandleError(IMessage origMessage, IMessage message, ListenerExecutionFailedException exception)
        {
            return $"{ServiceName} handled/processed";
        }
    }
}
