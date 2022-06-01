// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.RabbitMQ.Connection;
using Steeltoe.Messaging.RabbitMQ.Core;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Support;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Attributes.ComplexTypeJsonIntegrationTest;

namespace Steeltoe.Messaging.RabbitMQ.Attributes;

[Trait("Category", "Integration")]
public class ComplexTypeJsonIntegrationTest : IClassFixture<StartupFixture>
{
    public const string TEST_QUEUE = "test.complex.send.and.receive";
    public const string TEST_QUEUE2 = "test.complex.receive";

    private readonly ServiceProvider provider;
    private readonly StartupFixture fixture;

    public ComplexTypeJsonIntegrationTest(StartupFixture fix)
    {
        fixture = fix;
        provider = fixture.Provider;
    }

    public static Foo<Bar<Baz, Qux>> MakeAFoo()
    {
        var foo = new Foo<Bar<Baz, Qux>>();
        var bar = new Bar<Baz, Qux>
        {
            AField = new Baz("foo"),
            BField = new Qux(42)
        };
        foo.Field = bar;
        return foo;
    }

    [Fact]
    public void TestSendAndReceive()
    {
        var template = provider.GetRabbitTemplate();
        IMessagePostProcessor pp = new EmptyPostProcessor();
        object message = "foo";
        Assert.NotNull(template.ConvertSendAndReceiveAsType(message, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(template.ConvertSendAndReceiveAsType(message, pp, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(template.ConvertSendAndReceiveAsType(TEST_QUEUE, message, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(template.ConvertSendAndReceiveAsType(TEST_QUEUE, message, pp, null, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(template.ConvertSendAndReceiveAsType(string.Empty, TEST_QUEUE, message, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(template.ConvertSendAndReceiveAsType(string.Empty, TEST_QUEUE, message, pp, typeof(Foo<Bar<Baz, Qux>>)));
    }

    [Fact]
    public void TestReceive()
    {
        var template = provider.GetRabbitTemplate();
        var foo = MakeAFoo();
        var pp = new TestPostProcessor();
        template.ConvertAndSend(TEST_QUEUE2, foo, pp);
        var result = template.ReceiveAndConvert<Foo<Bar<Baz, Qux>>>(10000);
        Assert.NotNull(result);
    }

    [Fact]
    public void TestReceiveNoWait()
    {
        var template = provider.GetRabbitTemplate();
        var foo = MakeAFoo();
        var pp = new TestPostProcessor();
        template.ConvertAndSend(TEST_QUEUE2, foo, pp);
        var result = template.ReceiveAndConvert<Foo<Bar<Baz, Qux>>>();

        var n = 0;
        while (n++ < 100 && foo == null)
        {
            Thread.Sleep(100);
            result = template.ReceiveAndConvert<Foo<Bar<Baz, Qux>>>();
        }

        Assert.NotNull(result);
    }

    [Fact]
    public async Task TestAsyncSendAndReceive()
    {
        var template = provider.GetRabbitTemplate();
        IMessagePostProcessor pp = new EmptyPostProcessor();
        object message = "foo";
        Assert.NotNull(await template.ConvertSendAndReceiveAsTypeAsync(message, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(await template.ConvertSendAndReceiveAsTypeAsync(message, pp, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(await template.ConvertSendAndReceiveAsTypeAsync(TEST_QUEUE, message, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(await template.ConvertSendAndReceiveAsTypeAsync(TEST_QUEUE, message, pp, null, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(await template.ConvertSendAndReceiveAsTypeAsync(string.Empty, TEST_QUEUE, message, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(await template.ConvertSendAndReceiveAsTypeAsync(string.Empty, TEST_QUEUE, message, pp, typeof(Foo<Bar<Baz, Qux>>)));
    }

    public class EmptyPostProcessor : IMessagePostProcessor
    {
        public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
        {
            return message;
        }

        public IMessage PostProcessMessage(IMessage message)
        {
            return message;
        }
    }

    public class TestPostProcessor : IMessagePostProcessor
    {
        public IMessage PostProcessMessage(IMessage message, CorrelationData correlation)
        {
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.RemoveHeaders("__TypeId__");
            return message;
        }

        public IMessage PostProcessMessage(IMessage message)
        {
            var accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.RemoveHeaders("__TypeId__");
            return message;
        }
    }

    public class StartupFixture : IDisposable
    {
        private readonly IServiceCollection services;

        public ServiceProvider Provider { get; set; }

        public StartupFixture()
        {
            services = CreateContainer();
            Provider = services.BuildServiceProvider();
            Provider.GetRequiredService<IHostedService>().StartAsync(default).Wait();
        }

        public ServiceCollection CreateContainer(IConfiguration config = null)
        {
            var services = new ServiceCollection();
            config ??= new ConfigurationBuilder()
                .Build();

            services.AddLogging(b =>
            {
                b.SetMinimumLevel(LogLevel.Debug);
                b.AddDebug();
                b.AddConsole();
            });

            services.AddSingleton(config);
            services.AddRabbitHostingServices();
            services.AddRabbitJsonMessageConverter();
            services.AddRabbitMessageHandlerMethodFactory();
            services.AddRabbitListenerContainerFactory();
            services.AddRabbitListenerEndpointRegistry();
            services.AddRabbitListenerEndpointRegistrar();
            services.AddRabbitListenerAttributeProcessor();
            services.AddRabbitConnectionFactory();
            services.AddRabbitAdmin();
            services.AddRabbitTemplate((p, t) =>
            {
                t.DefaultReceiveDestination = TEST_QUEUE2;
                t.DefaultSendDestination = TEST_QUEUE;
            });

            services.AddRabbitQueue(TEST_QUEUE);
            services.AddRabbitQueue(TEST_QUEUE2);

            services.AddSingleton<Listener>();
            services.AddRabbitListeners<Listener>(config);
            return services;
        }

        public void Dispose()
        {
            var admin = Provider.GetRabbitAdmin();
            admin.DeleteQueue(TEST_QUEUE);
            admin.DeleteQueue(TEST_QUEUE2);
            Provider.Dispose();
        }
    }

    public class Listener
    {
        [RabbitListener(TEST_QUEUE)]
        public Foo<Bar<Baz, Qux>> Listen(string input)
        {
            return MakeAFoo();
        }
    }

    public class Foo<T>
    {
        public T Field { get; set; }

        public override string ToString()
        {
            return $"Foo [field={Field}]";
        }
    }

    public class Bar<A, B>
    {
        public A AField { get; set; }

        public B BField { get; set; }

        public override string ToString()
        {
            return $"Bar [aField={AField}, bField={BField}]";
        }
    }

    public class Baz
    {
        public Baz(string s)
        {
            BazField = s;
        }

        public string BazField { get; set; }

        public override string ToString()
        {
            return $"Baz [baz={BazField}]";
        }
    }

    public class Qux
    {
        public int QuxField { get; set; }

        public Qux(int i)
        {
            QuxField = i;
        }

        public override string ToString()
        {
            return $"Qux [qux={QuxField}]";
        }
    }
}
