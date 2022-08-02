// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Expression.Internal.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Integration.Attributes;
using Steeltoe.Integration.Config;
using Steeltoe.Integration.Extensions;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Attributes;
using Steeltoe.Messaging.Handler.Attributes.Support;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.Support;
using Xunit;

namespace Steeltoe.Integration.Handler;

public class MethodInvokingMessageProcessorTest
{
    [Fact]
    public async Task TestMessageHandlerMethodFactoryOverride()
    {
        IServiceCollection serviceCollection = GetDefaultContainer();

        serviceCollection.AddServiceActivators<MyConfiguration>();
        var f = new DefaultMessageHandlerMethodFactory();

        f.SetArgumentResolvers(new List<IHandlerMethodArgumentResolver>
        {
            new TestHandlerMethodArgumentResolver()
        });

        serviceCollection.AddSingleton<IMessageHandlerMethodFactory>(f);

        ServiceProvider container = serviceCollection.BuildServiceProvider();
        ILifecycleProcessor lifeCycleProcessor = await Start(container);

        var appContext = container.GetService<IApplicationContext>();
        var channel = appContext.GetService<IMessageChannel>("foo");
        channel.Send(MessageBuilder.WithPayload("Bob Smith").Build());
        var outChan = appContext.GetService<IPollableChannel>("out");
        Assert.Equal("Person: Bob Smith", outChan.Receive().Payload);

        await lifeCycleProcessor.Stop();
    }

    [Fact]
    public async Task TestHandlerInheritanceMethodImplInSuper()
    {
        IServiceCollection serviceCollection = GetDefaultContainer();

        serviceCollection.AddServiceActivators<B1>();
        ServiceProvider container = serviceCollection.BuildServiceProvider();
        ILifecycleProcessor lifeCycleProcessor = await Start(container);

        var appContext = container.GetService<IApplicationContext>();
        var channel = appContext.GetService<IMessageChannel>("in");
        channel.Send(Message.Create(string.Empty));
        var outChan = appContext.GetService<IPollableChannel>("out");
        IMessage received = outChan.Receive();
        Assert.Equal("A1", received.Headers.Get<string>("A1"));

        await lifeCycleProcessor.Stop();
    }

    [Fact]
    public async Task TestHandlerInheritanceMethodImplInSubClass()
    {
        IServiceCollection serviceCollection = GetDefaultContainer();

        serviceCollection.AddServiceActivators<C2>();
        ServiceProvider container = serviceCollection.BuildServiceProvider();
        ILifecycleProcessor lifeCycleProcessor = await Start(container);

        var appContext = container.GetService<IApplicationContext>();
        var channel = appContext.GetService<IMessageChannel>("in");
        channel.Send(Message.Create(string.Empty));
        var outChan = appContext.GetService<IPollableChannel>("out");
        IMessage received = outChan.Receive();
        Assert.Equal("C2", received.Headers.Get<string>("C2"));

        await lifeCycleProcessor.Stop();
    }

    [Fact]
    public async Task TestHandlerInheritanceMethodImplInSubClassAndSuper()
    {
        IServiceCollection serviceCollection = GetDefaultContainer();

        serviceCollection.AddServiceActivators<C3>();
        ServiceProvider container = serviceCollection.BuildServiceProvider();
        ILifecycleProcessor lifeCycleProcessor = await Start(container);

        var appContext = container.GetService<IApplicationContext>();
        var channel = appContext.GetService<IMessageChannel>("in");
        channel.Send(Message.Create(string.Empty));
        var outChan = appContext.GetService<IPollableChannel>("out");
        IMessage received = outChan.Receive();
        Assert.Equal("C3", received.Headers.Get<string>("C3"));

        await lifeCycleProcessor.Stop();
    }

    [Fact]
    public void PayloadAsMethodParameterAndObjectAsReturnValue()
    {
        var testService = new TestService();
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.AcceptPayloadAndReturnObject));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        object result = processor.ProcessMessage(Message.Create("testing"));
        Assert.Equal("testing-1", result);
    }

    [Fact]
    public void TestPayloadCoercedToString()
    {
        var testService = new TestService();
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.AcceptPayloadAndReturnObject));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        object result = processor.ProcessMessage(Message.Create(123_456_789));
        Assert.Equal("123456789-1", result);
    }

    [Fact]
    public void PayloadAsMethodParameterAndMessageAsReturnValue()
    {
        var testService = new TestService();
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.AcceptPayloadAndReturnMessage));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        var result = processor.ProcessMessage(Message.Create("testing")) as IMessage;
        Assert.NotNull(result);
        Assert.Equal("testing-2", result.Payload);
    }

    [Fact]
    public void MessageAsMethodParameterAndObjectAsReturnValue()
    {
        var testService = new TestService();
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.AcceptMessageAndReturnObject));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        object result = processor.ProcessMessage(Message.Create("testing"));
        Assert.NotNull(result);
        Assert.Equal("testing-3", result);
    }

    [Fact]
    public void MessageAsMethodParameterAndMessageAsReturnValue()
    {
        var testService = new TestService();
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.AcceptMessageAndReturnMessage));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        var result = processor.ProcessMessage(Message.Create("testing")) as IMessage;
        Assert.NotNull(result);
        Assert.Equal("testing-4", result.Payload);
    }

    [Fact]
    public void MessageSubclassAsMethodParameterAndMessageAsReturnValue()
    {
        var testService = new TestService();
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.AcceptMessageSubclassAndReturnMessage));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        var result = processor.ProcessMessage(Message.Create("testing")) as IMessage;
        Assert.NotNull(result);
        Assert.Equal("testing-5", result.Payload);
    }

    [Fact]
    public void MessageSubclassAsMethodParameterAndMessageSubclassAsReturnValue()
    {
        var testService = new TestService();
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.AcceptMessageSubclassAndReturnMessageSubclass));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        var result = processor.ProcessMessage(Message.Create("testing")) as IMessage;
        Assert.NotNull(result);
        Assert.Equal("testing-6", result.Payload);
    }

    [Fact]
    public void PayloadAndHeaderAnnotationMethodParametersAndObjectAsReturnValue()
    {
        var testService = new TestService();
        MethodInfo method = typeof(TestService).GetMethod(nameof(TestService.AcceptPayloadAndHeaderAndReturnObject));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        IMessage request = MessageBuilder.WithPayload("testing").SetHeader("number", 123).Build();
        object result = processor.ProcessMessage(request);
        Assert.NotNull(result);
        Assert.Equal("testing-123", result);
    }

    // [Fact]
    // public void TestVoidMethodsIncludedByDefault()
    // {
    //    var testService = new TestService();
    //    var method = typeof(TestService).GetMethod(nameof(TestService.TestVoidReturningMethods));
    //    var context = GetDefaultContext();
    //    var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
    //    var request = MessageBuilder.WithPayload("Something").Build();
    //    var result = processor.ProcessMessage(request);
    //    Assert.Null(result);
    //    var request2 = MessageBuilder.WithPayload(12).Build();
    //    var result2 = processor.ProcessMessage(request2);
    //    Assert.Equal(12, result2);
    // }
    [Fact]
    public void MessageOnlyWithAnnotatedMethod()
    {
        var testService = new AnnotatedTestService();
        MethodInfo method = typeof(AnnotatedTestService).GetMethod(nameof(AnnotatedTestService.MessageOnly));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        object result = processor.ProcessMessage(Message.Create("foo"));
        Assert.Equal("foo", result);
    }

    [Fact]
    public void PayloadWithAnnotatedMethod()
    {
        var testService = new AnnotatedTestService();
        MethodInfo method = typeof(AnnotatedTestService).GetMethod(nameof(AnnotatedTestService.IntegerMethod));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        object result = processor.ProcessMessage(Message.Create(123));
        Assert.Equal(123, result);
    }

    [Fact]
    public void ConvertedPayloadWithAnnotatedMethod()
    {
        var testService = new AnnotatedTestService();
        MethodInfo method = typeof(AnnotatedTestService).GetMethod(nameof(AnnotatedTestService.IntegerMethod));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        object result = processor.ProcessMessage(Message.Create("456"));
        Assert.Equal(456, result);
    }

    [Fact]
    public void ConversionFailureWithAnnotatedMethod()
    {
        var testService = new AnnotatedTestService();
        MethodInfo method = typeof(AnnotatedTestService).GetMethod(nameof(AnnotatedTestService.IntegerMethod));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        var ex = Assert.Throws<MessageHandlingException>(() => processor.ProcessMessage(Message.Create("foo")));
        Assert.IsType<MessageConversionException>(ex.InnerException);
    }

    [Fact]
    public void FilterSelectsAnnotationMethodsOnly()
    {
        var service = new OverloadedMethodService();
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, service, typeof(ServiceActivatorAttribute));
        processor.ProcessMessage(MessageBuilder.WithPayload(123).Build());
        Assert.NotNull(service.LastArg);
        Assert.IsType<string>(service.LastArg);
        Assert.Equal("123", service.LastArg);
    }

    [Fact]
    public void TestProcessMessageRuntimeException()
    {
        var testService = new TestErrorService();
        MethodInfo method = typeof(TestErrorService).GetMethod(nameof(TestErrorService.Error));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        var ex = Assert.Throws<MessageHandlingException>(() => processor.ProcessMessage(Message.Create("foo")));
        Assert.IsType<InvalidOperationException>(ex.InnerException);
    }

    [Fact]
    public void TestProcessMessageCheckedException()
    {
        var testService = new TestErrorService();
        MethodInfo method = typeof(TestErrorService).GetMethod(nameof(TestErrorService.Checked));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        var ex = Assert.Throws<MessageHandlingException>(() => processor.ProcessMessage(Message.Create("foo")));
        Assert.IsType<CheckedException>(ex.InnerException);
    }

    [Fact]
    public void MessageAndHeaderWithAnnotatedMethod()
    {
        var testService = new AnnotatedTestService();
        MethodInfo method = typeof(AnnotatedTestService).GetMethod(nameof(AnnotatedTestService.MessageAndHeader));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        IMessage request = MessageBuilder.WithPayload("foo").SetHeader("number", 42).Build();
        object result = processor.ProcessMessage(request);
        Assert.NotNull(result);
        Assert.Equal("foo-42", result);
    }

    [Fact]
    public void MultipleHeadersWithAnnotatedMethod()
    {
        var testService = new AnnotatedTestService();
        MethodInfo method = typeof(AnnotatedTestService).GetMethod(nameof(AnnotatedTestService.TwoHeaders));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        IMessage request = MessageBuilder.WithPayload("foo").SetHeader("number", 42).SetHeader("prop", "bar").Build();
        object result = processor.ProcessMessage(request);
        Assert.NotNull(result);
        Assert.Equal("bar-42", result);
    }

    [Fact]
    public void OptionalAndRequiredWithAnnotatedMethod()
    {
        var testService = new AnnotatedTestService();
        MethodInfo method = typeof(AnnotatedTestService).GetMethod(nameof(AnnotatedTestService.OptionalAndRequiredHeader));
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, testService, method);
        IMessage message = MessageBuilder.WithPayload("foo").SetHeader("num", 42).Build();
        object result = processor.ProcessMessage(message);
        Assert.Equal("null42", result);
        message = MessageBuilder.WithPayload("foo").SetHeader("prop", "bar").SetHeader("num", 42).Build();
        result = processor.ProcessMessage(message);
        Assert.Equal("bar42", result);
        message = MessageBuilder.WithPayload("foo").SetHeader("prop", "bar").Build();
        var ex = Assert.Throws<MessageHandlingException>(() => processor.ProcessMessage(message));
        Assert.Contains("num", ex.InnerException.Message);
    }

    [Fact]
    public void TestPrivateMethod()
    {
        var service = new Foo();
        IApplicationContext context = GetDefaultContext();
        var processor = new MethodInvokingMessageProcessor<object>(context, service, typeof(ServiceActivatorAttribute));
        Assert.Equal("FOO", processor.ProcessMessage(Message.Create("foo")));
        Assert.Equal("BAR", processor.ProcessMessage(Message.Create("bar")));
    }

    private IApplicationContext GetDefaultContext()
    {
        var serviceCollection = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();

        var context = new GenericApplicationContext(serviceCollection.BuildServiceProvider(), configBuilder.Build())
        {
            ServiceExpressionResolver = new StandardServiceExpressionResolver()
        };

        return context;
    }

    private async Task<ILifecycleProcessor> Start(ServiceProvider container)
    {
        var saProcessor = container.GetRequiredService<ServiceActivatorAttributeProcessor>();
        saProcessor.Initialize();

        var lifeCycleProcessor = container.GetRequiredService<ILifecycleProcessor>();
        await lifeCycleProcessor.Start();
        return lifeCycleProcessor;
    }

    private IServiceCollection GetDefaultContainer()
    {
        var serviceCollection = new ServiceCollection();
        IConfigurationRoot config = new ConfigurationBuilder().Build();
        serviceCollection.AddSingleton<IConfiguration>(config);
        serviceCollection.AddLogging();

        serviceCollection.AddGenericApplicationContext((_, context) =>
        {
            context.ServiceExpressionResolver = new StandardServiceExpressionResolver();
        });

        serviceCollection.AddIntegrationServices();
        serviceCollection.AddQueueChannel("out");

        return serviceCollection;
    }

    public class Foo
    {
        [ServiceActivator]
#pragma warning disable IDE0051 // Remove unused private members
        private string Service(string payload)
#pragma warning restore IDE0051 // Remove unused private members
        {
            return payload.ToUpper();
        }
    }

    public class TestErrorService
    {
        public string Error(string input)
        {
            throw new InvalidOperationException("Expected test exception");
        }

        public string Checked(string input)
        {
            throw new CheckedException("Expected test exception");
        }
    }

    public class CheckedException : Exception
    {
        public CheckedException(string message)
            : base(message)
        {
        }
    }

    public class OverloadedMethodService
    {
        public volatile object LastArg;

        public void Foo(bool b)
        {
            LastArg = b;
        }

        [ServiceActivator]
        public string Foo(string s)
        {
            LastArg = s;
            return s;
        }
    }

    public class AnnotatedTestService
    {
        public string MessageOnly(IMessage message)
        {
            return (string)message.Payload;
        }

        public string MessageAndHeader(IMessage message, [Header("number")] int num)
        {
            return $"{message.Payload}-{num}";
        }

        public string TwoHeaders([Header] string prop, [Header("number")] int num)
        {
            return $"{prop}-{num}";
        }

        public int OptionalHeader([Header(Required = false)] int num)
        {
            return num;
        }

        public int RequiredHeader([Header("num")] int num)
        {
            return num;
        }

        public string OptionalAndRequiredHeader([Header(Required = false)] string prop, [Header("num")] int num)
        {
            return (prop ?? "null") + num;
        }

        public string OptionalAndRequiredDottedHeader([Header(Name = "dot1.foo", Required = false)] string prop, [Header(Name = "dot2.baz")] int num,
            [Header("'dotted.literal'")] string dotted)
        {
            return prop + num + dotted;
        }

        // public Properties propertiesMethod(Properties properties)
        // {
        //    return properties;
        // }
        public IDictionary MapMethod(IDictionary map)
        {
            return map;
        }

        public int IntegerMethod(int i)
        {
            return i;
        }
    }

    public class TestService
    {
        public string AcceptPayloadAndReturnObject(string s)
        {
            return $"{s}-1";
        }

        public IMessage AcceptPayloadAndReturnMessage(string s)
        {
            return Message.Create($"{s}-2");
        }

        public string AcceptMessageAndReturnObject(IMessage m)
        {
            return $"{m.Payload}-3";
        }

        public IMessage AcceptMessageAndReturnMessage(IMessage m)
        {
            return Message.Create($"{m.Payload}-4");
        }

        public IMessage AcceptMessageSubclassAndReturnMessage(IMessage<string> m)
        {
            return Message.Create($"{m.Payload}-5");
        }

        public IMessage<string> AcceptMessageSubclassAndReturnMessageSubclass(IMessage<string> m)
        {
            return Message.Create($"{m.Payload}-6");
        }

        public string AcceptPayloadAndHeaderAndReturnObject(string s, [Header("number")] int n)
        {
            return $"{s}-{n}";
        }

        public void TestVoidReturningMethods(string s)
        {
            // do nothing
        }

        public int TestVoidReturningMethods(int i)
        {
            return i;
        }
    }

    public class A1
    {
        [ServiceActivator(InputChannel = "in", OutputChannel = "out")]
        public IMessage MyMethod(IMessage<string> msg)
        {
            return MessageBuilder.FromMessage(msg).SetHeader("A1", "A1").Build();
        }
    }

    public class B1 : A1
    {
    }

    public class C1 : B1
    {
    }

    public class A2
    {
        [ServiceActivator(InputChannel = "in", OutputChannel = "out")]
        public virtual IMessage MyMethod(IMessage<string> msg)
        {
            return MessageBuilder.FromMessage(msg).SetHeader("A2", "A2").Build();
        }
    }

    public class B2 : A2
    {
        [ServiceActivator(InputChannel = "in", OutputChannel = "out")]
        public override IMessage MyMethod(IMessage<string> msg)
        {
            return MessageBuilder.FromMessage(msg).SetHeader("B2", "B2").Build();
        }
    }

    public class C2 : B2
    {
        [ServiceActivator(InputChannel = "in", OutputChannel = "out")]
        public override IMessage MyMethod(IMessage<string> msg)
        {
            return MessageBuilder.FromMessage(msg).SetHeader("C2", "C2").Build();
        }
    }

    public class A3
    {
        [ServiceActivator(InputChannel = "in", OutputChannel = "out")]
        public virtual IMessage MyMethod(IMessage<string> msg)
        {
            return MessageBuilder.FromMessage(msg).SetHeader("A3", "A3").Build();
        }
    }

    public class B3 : A3
    {
    }

    public class C3 : B3
    {
        [ServiceActivator(InputChannel = "in", OutputChannel = "out")]
        public override IMessage MyMethod(IMessage<string> msg)
        {
            return MessageBuilder.FromMessage(msg).SetHeader("C3", "C3").Build();
        }
    }

    public class MyConfiguration
    {
        [ServiceActivator(InputChannel = "foo", OutputChannel = "out")]
        public string Foo(Person person)
        {
            return person.ToString();
        }
    }

    public class TestHandlerMethodArgumentResolver : IHandlerMethodArgumentResolver
    {
        public object ResolveArgument(ParameterInfo parameter, IMessage message)
        {
            string[] names = ((string)message.Payload).Split(" ");
            return new Person(names[0], names[1]);
        }

        public bool SupportsParameter(ParameterInfo parameter)
        {
            return true;
        }
    }

    public class Person
    {
        public string Name { get; }

        public Person(string firstName, string lastName)
        {
            Name = $"{firstName} {lastName}";
        }

        public override string ToString()
        {
            return $"Person: {Name}";
        }
    }
}
