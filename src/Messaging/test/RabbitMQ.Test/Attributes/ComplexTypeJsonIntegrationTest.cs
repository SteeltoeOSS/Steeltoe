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
using Xunit;
using static Steeltoe.Messaging.RabbitMQ.Attributes.ComplexTypeJsonIntegrationTest;

namespace Steeltoe.Messaging.RabbitMQ.Attributes;

[Trait("Category", "Integration")]
public class ComplexTypeJsonIntegrationTest : IClassFixture<StartupFixture>
{
    public const string TestQueue = "test.complex.send.and.receive";
    public const string TestQueue2 = "test.complex.receive";

    private readonly ServiceProvider _provider;
    private readonly StartupFixture _fixture;

    public ComplexTypeJsonIntegrationTest(StartupFixture fix)
    {
        _fixture = fix;
        _provider = _fixture.Provider;
    }

    [Fact]
    public void TestSendAndReceive()
    {
        RabbitTemplate template = _provider.GetRabbitTemplate();
        IMessagePostProcessor pp = new EmptyPostProcessor();
        object message = "foo";
        Assert.NotNull(template.ConvertSendAndReceiveAsType(message, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(template.ConvertSendAndReceiveAsType(message, pp, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(template.ConvertSendAndReceiveAsType(TestQueue, message, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(template.ConvertSendAndReceiveAsType(TestQueue, message, pp, null, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(template.ConvertSendAndReceiveAsType(string.Empty, TestQueue, message, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(template.ConvertSendAndReceiveAsType(string.Empty, TestQueue, message, pp, typeof(Foo<Bar<Baz, Qux>>)));
    }

    [Fact]
    public void TestReceive()
    {
        RabbitTemplate template = _provider.GetRabbitTemplate();
        Foo<Bar<Baz, Qux>> foo = MakeAFoo();
        var pp = new TestPostProcessor();
        template.ConvertAndSend(TestQueue2, foo, pp);
        var result = template.ReceiveAndConvert<Foo<Bar<Baz, Qux>>>(10000);
        Assert.NotNull(result);
    }

    [Fact]
    public void TestReceiveNoWait()
    {
        RabbitTemplate template = _provider.GetRabbitTemplate();
        Foo<Bar<Baz, Qux>> foo = MakeAFoo();
        var pp = new TestPostProcessor();
        template.ConvertAndSend(TestQueue2, foo, pp);
        var result = template.ReceiveAndConvert<Foo<Bar<Baz, Qux>>>();

        int n = 0;

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
        RabbitTemplate template = _provider.GetRabbitTemplate();
        IMessagePostProcessor pp = new EmptyPostProcessor();
        object message = "foo";
        Assert.NotNull(await template.ConvertSendAndReceiveAsTypeAsync(message, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(await template.ConvertSendAndReceiveAsTypeAsync(message, pp, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(await template.ConvertSendAndReceiveAsTypeAsync(TestQueue, message, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(await template.ConvertSendAndReceiveAsTypeAsync(TestQueue, message, pp, null, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(await template.ConvertSendAndReceiveAsTypeAsync(string.Empty, TestQueue, message, typeof(Foo<Bar<Baz, Qux>>)));
        Assert.NotNull(await template.ConvertSendAndReceiveAsTypeAsync(string.Empty, TestQueue, message, pp, typeof(Foo<Bar<Baz, Qux>>)));
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
            RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.RemoveHeaders("__TypeId__");
            return message;
        }

        public IMessage PostProcessMessage(IMessage message)
        {
            RabbitHeaderAccessor accessor = RabbitHeaderAccessor.GetMutableAccessor(message);
            accessor.RemoveHeaders("__TypeId__");
            return message;
        }
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

        private ServiceCollection CreateContainer(IConfiguration configuration = null)
        {
            var services = new ServiceCollection();
            configuration ??= new ConfigurationBuilder().Build();

            services.AddLogging(b =>
            {
                b.SetMinimumLevel(LogLevel.Debug);
                b.AddDebug();
                b.AddConsole();
            });

            services.AddSingleton(configuration);
            services.AddRabbitHostingServices();
            services.AddRabbitJsonMessageConverter();
            services.AddRabbitMessageHandlerMethodFactory();
            services.AddRabbitListenerContainerFactory();
            services.AddRabbitListenerEndpointRegistry();
            services.AddRabbitListenerEndpointRegistrar();
            services.AddRabbitListenerAttributeProcessor();
            services.AddRabbitConnectionFactory();
            services.AddRabbitAdmin();

            services.AddRabbitTemplate((_, t) =>
            {
                t.DefaultReceiveDestination = TestQueue2;
                t.DefaultSendDestination = TestQueue;
            });

            services.AddRabbitQueue(TestQueue);
            services.AddRabbitQueue(TestQueue2);

            services.AddSingleton<Listener>();
            services.AddRabbitListeners<Listener>(configuration);
            return services;
        }

        public void Dispose()
        {
            RabbitAdmin admin = Provider.GetRabbitAdmin();
            admin.DeleteQueue(TestQueue);
            admin.DeleteQueue(TestQueue2);
            Provider.Dispose();
        }
    }

    public class Listener
    {
        [RabbitListener(TestQueue)]
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

    public class Bar<TFieldA, TFieldB>
    {
        public TFieldA AField { get; set; }

        public TFieldB BField { get; set; }

        public override string ToString()
        {
            return $"Bar [aField={AField}, bField={BField}]";
        }
    }

    public class Baz
    {
        public string BazField { get; set; }

        public Baz(string s)
        {
            BazField = s;
        }

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
