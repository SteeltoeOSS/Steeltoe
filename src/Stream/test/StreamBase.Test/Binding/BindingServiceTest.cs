// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using Steeltoe.Stream.Binder;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Util;
using Xunit;

namespace Steeltoe.Stream.Binding;

public class BindingServiceTest : AbstractTest
{
    [Fact]
    public void TestDefaultGroup()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");
        ServiceProvider provider = CreateStreamsContainer(searchDirectories, "spring.cloud.stream.bindings.input.destination=foo").BuildServiceProvider();

        var factory = provider.GetService<IBinderFactory>();
        IBinder binder = factory.GetBinder("mock");

        IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
        Mock<IBinder> mockBinder = Mock.Get(binder);

        var service = provider.GetService<BindingService>();
        ICollection<IBinding> bindings = service.BindConsumer(inputChannel, "input");

        Assert.Single(bindings);
        IBinding binding = bindings.First();
        Mock<IBinding> mockBinding = Mock.Get(binding);

        service.UnbindConsumers("input");

        mockBinder.Verify(b => b.BindConsumer("foo", null, inputChannel, It.IsAny<ConsumerOptions>()));
        mockBinding.Verify(b => b.UnbindAsync());
    }

    [Fact]
    public void TestMultipleConsumerBindings()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");
        ServiceProvider provider = CreateStreamsContainer(searchDirectories, "spring.cloud.stream.bindings.input.destination=foo,bar").BuildServiceProvider();

        var factory = provider.GetService<IBinderFactory>();
        IBinder binder = factory.GetBinder("mock");

        IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
        Mock<IBinder> mockBinder = Mock.Get(binder);

        var service = provider.GetService<BindingService>();
        ICollection<IBinding> bindings = service.BindConsumer(inputChannel, "input");

        List<IBinding> bindingsAsList = bindings.ToList();

        Assert.Equal(2, bindingsAsList.Count);
        IBinding binding1 = bindingsAsList[0];
        IBinding binding2 = bindingsAsList[1];
        Mock<IBinding> mockBinding1 = Mock.Get(binding1);
        Mock<IBinding> mockBinding2 = Mock.Get(binding2);

        service.UnbindConsumers("input");

        mockBinder.Verify(b => b.BindConsumer("foo", null, inputChannel, It.IsAny<ConsumerOptions>()));
        mockBinder.Verify(b => b.BindConsumer("bar", null, inputChannel, It.IsAny<ConsumerOptions>()));

        mockBinding1.Verify(b => b.UnbindAsync());
        mockBinding2.Verify(b => b.UnbindAsync());
    }

    [Fact]
    public void TestConsumerBindingWhenMultiplexingIsEnabled()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        ServiceProvider provider = CreateStreamsContainer(searchDirectories, "spring.cloud.stream.bindings.input.destination=foo,bar",
            "spring.cloud.stream.bindings.input.consumer.multiplex=true").BuildServiceProvider();

        var factory = provider.GetService<IBinderFactory>();
        IBinder binder = factory.GetBinder("mock");

        IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
        Mock<IBinder> mockBinder = Mock.Get(binder);

        var service = provider.GetService<BindingService>();
        ICollection<IBinding> bindings = service.BindConsumer(inputChannel, "input");

        Assert.Single(bindings);
        IBinding binding = bindings.First();
        Mock<IBinding> mockBinding = Mock.Get(binding);

        service.UnbindConsumers("input");

        mockBinder.Verify(b => b.BindConsumer("foo,bar", null, inputChannel, It.IsAny<ConsumerOptions>()));
        mockBinding.Verify(b => b.UnbindAsync());
    }

    [Fact]
    public void TestExplicitGroup()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        ServiceProvider provider = CreateStreamsContainer(searchDirectories, "spring.cloud.stream.bindings.input.destination=foo",
            "spring.cloud.stream.bindings.input.group=fooGroup").BuildServiceProvider();

        var factory = provider.GetService<IBinderFactory>();
        IBinder binder = factory.GetBinder("mock");

        IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
        Mock<IBinder> mockBinder = Mock.Get(binder);

        var service = provider.GetService<BindingService>();
        ICollection<IBinding> bindings = service.BindConsumer(inputChannel, "input");

        Assert.Single(bindings);
        IBinding binding = bindings.First();
        Mock<IBinding> mockBinding = Mock.Get(binding);

        service.UnbindConsumers("input");

        mockBinder.Verify(b => b.BindConsumer("foo", "fooGroup", inputChannel, It.IsAny<ConsumerOptions>()));
        mockBinding.Verify(b => b.UnbindAsync());
    }

    [Fact]
    public void TestProducerPropertiesValidation()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        ServiceProvider provider = CreateStreamsContainer(searchDirectories, "spring.cloud.stream.bindings.output.destination=foo",
            "spring.cloud.stream.bindings.output.producer.partitionCount=0").BuildServiceProvider();

        IMessageChannel outputChannel = new DirectChannel(provider.GetService<IApplicationContext>());

        var service = provider.GetService<BindingService>();
        Assert.Throws<InvalidOperationException>(() => service.BindProducer(outputChannel, "output"));
    }

    [Fact]
    public async Task TestDefaultPropertyBehavior()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        ServiceCollection container = CreateStreamsContainerWithBinding(searchDirectories, typeof(IFooBinding),
            "spring.cloud.stream.default.contentType=text/plain", "spring.cloud.stream.bindings.input1.contentType=application/json",
            "spring.cloud.stream.default.group=foo", "spring.cloud.stream.bindings.input2.group=bar", "spring.cloud.stream.default.consumer.concurrency=5",
            "spring.cloud.stream.bindings.input2.consumer.concurrency=1", "spring.cloud.stream.bindings.input1.consumer.partitioned=true",
            "spring.cloud.stream.default.producer.partitionCount=10", "spring.cloud.stream.bindings.output2.producer.partitionCount=1",
            "spring.cloud.stream.bindings.inputXyz.contentType=application/json", "spring.cloud.stream.bindings.inputFooBar.contentType=application/avro",
            "spring.cloud.stream.bindings.input_snake_case.contentType=application/avro");

        ServiceProvider provider = container.BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var bindServiceOptions = provider.GetService<IOptionsMonitor<BindingServiceOptions>>();
        Dictionary<string, BindingOptions> bindings = bindServiceOptions.CurrentValue.Bindings;

        Assert.Equal("application/json", bindings["input1"].ContentType);
        Assert.Equal("text/plain", bindings["input2"].ContentType);
        Assert.Equal("foo", bindings["input1"].Group);
        Assert.Equal("bar", bindings["input2"].Group);
        Assert.Equal(5, bindings["input1"].Consumer.Concurrency);
        Assert.Equal(1, bindings["input2"].Consumer.Concurrency);
        Assert.True(bindings["input1"].Consumer.Partitioned);
        Assert.False(bindings["input2"].Consumer.Partitioned);
        Assert.Equal(10, bindings["output1"].Producer.PartitionCount);
        Assert.Equal(1, bindings["output2"].Producer.PartitionCount);
        Assert.Equal("application/json", bindings["inputXyz"].ContentType);
        Assert.Equal("application/avro", bindings["inputFooBar"].ContentType);
        Assert.Equal("text/plain", bindings["inputFooBarBuzz"].ContentType);
        Assert.Equal("application/avro", bindings["input_snake_case"].ContentType);
    }

    [Fact]
    public void TestConsumerPropertiesValidation()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        ServiceProvider provider = CreateStreamsContainer(searchDirectories, "spring.cloud.stream.bindings.input.destination=foo",
            "spring.cloud.stream.bindings.input.consumer.concurrency=0").BuildServiceProvider();

        IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());

        var service = provider.GetService<BindingService>();
        Assert.Throws<InvalidOperationException>(() => service.BindConsumer(inputChannel, "input"));
    }

    [Fact]
    public void TestUnknownBinderOnBindingFailure()
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");

        ServiceProvider provider = CreateStreamsContainer(searchDirectories, "spring.cloud.stream.bindings.input.destination=fooInput",
            "spring.cloud.stream.bindings.input.binder=mock", "spring.cloud.stream.bindings.output.destination=fooOutput",
            "spring.cloud.stream.bindings.output.binder=mockError").BuildServiceProvider();

        IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
        IMessageChannel outputChannel = new DirectChannel(provider.GetService<IApplicationContext>());

        var service = provider.GetService<BindingService>();
        service.BindConsumer(inputChannel, "input");

        Assert.Throws<InvalidOperationException>(() => service.BindProducer(outputChannel, "output"));
    }

    // TODO: Assert on the expected test outcome and remove suppression. Beyond not crashing, this test ensures nothing about the system under test.
    [Fact]
#pragma warning disable S2699 // Tests should include assertions
    public void TestUnrecognizedBinderAllowedIfNotUsed()
#pragma warning restore S2699 // Tests should include assertions
    {
        List<string> searchDirectories = GetSearchDirectories("MockBinder");
        const string mockBinder = "Steeltoe.Stream.MockBinder.Startup" + "," + "Steeltoe.Stream.MockBinder";
        string mockAssembly = $"{searchDirectories[0]}{Path.DirectorySeparatorChar}Steeltoe.Stream.MockBinder.dll";

        ServiceProvider provider = CreateStreamsContainer(searchDirectories, "spring.cloud.stream.bindings.input.destination=fooInput",
            "spring.cloud.stream.bindings.output.destination=fooOutput", "spring.cloud.stream.defaultBinder=mock1",
            $"spring.cloud.stream.binders.mock1.configureclass={mockBinder}", $"spring.cloud.stream.binders.mock1.configureassembly={mockAssembly}",
            "spring.cloud.stream.binders.kafka1.configureclass=kafka").BuildServiceProvider();

        IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
        IMessageChannel outputChannel = new DirectChannel(provider.GetService<IApplicationContext>());

        var service = provider.GetService<BindingService>();
        _ = service.BindConsumer(inputChannel, "input");
        _ = service.BindProducer(outputChannel, "output");
    }

    [Fact]
    public void TestResolveBindableType()
    {
        Type bindableType = GenericsUtils.GetParameterType(typeof(FooBinder), typeof(IBinder<>), 0);
        Assert.Same(typeof(SomeBindableType), bindableType);
    }

    [Fact]
    public void TestLateBindingConsumer()
    {
        List<string> searchDirectories = GetSearchDirectories("StubBinder1");

        ServiceProvider provider = CreateStreamsContainer(
            searchDirectories, "spring.cloud.stream.bindings.input.destination=foo", "spring.cloud.stream.bindingretryinterval=1").BuildServiceProvider();

        var service = provider.GetService<BindingService>();

        var factory = provider.GetService<IBinderFactory>();
        IBinder binder = factory.GetBinder("binder1");
        PropertyInfo prop = binder.GetType().GetProperty("BindConsumerFunc");
        var fail = new CountdownEvent(2);
        var mockBinding = new Mock<IBinding>();

        Func<string, string, object, IConsumerOptions, IBinding> func = (_, _, _, _) =>
        {
            fail.Signal();

            if (fail.CurrentCount == 1)
            {
                throw new Exception("fail");
            }

            return mockBinding.Object;
        };

        prop.GetSetMethod().Invoke(binder, new object[]
        {
            func
        });

        IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
        ICollection<IBinding> bindings = service.BindConsumer(inputChannel, "input");
        Assert.True(fail.IsSet);
        Assert.Single(bindings);
        Assert.Same(mockBinding.Object, bindings.Single());
        service.UnbindConsumers("input");
        mockBinding.Verify(b => b.UnbindAsync());
    }

    [Fact]
    public void TestLateBindingProducer()
    {
        List<string> searchDirectories = GetSearchDirectories("StubBinder1");

        ServiceProvider provider = CreateStreamsContainer(
            searchDirectories, "spring.cloud.stream.bindings.output.destination=foo", "spring.cloud.stream.bindingretryinterval=1").BuildServiceProvider();

        var service = provider.GetService<BindingService>();

        var factory = provider.GetService<IBinderFactory>();
        IBinder binder = factory.GetBinder("binder1");

        var fail = new CountdownEvent(2);
        var mockBinding = new Mock<IBinding>();
        PropertyInfo prop = binder.GetType().GetProperty("BindProducerFunc");

        Func<string, object, IProducerOptions, IBinding> func = (_, _, _) =>
        {
            fail.Signal();

            if (fail.CurrentCount == 1)
            {
                throw new Exception("fail");
            }

            return mockBinding.Object;
        };

        prop.GetSetMethod().Invoke(binder, new object[]
        {
            func
        });

        IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
        IBinding binding = service.BindProducer(inputChannel, "output");

        Assert.True(fail.IsSet);
        Assert.NotNull(binding);
        Assert.Same(mockBinding.Object, binding);
        service.UnbindProducers("output");
        mockBinding.Verify(b => b.UnbindAsync());
    }

    [Fact]
    public async Task TestBindingAutoStartup()
    {
        List<string> searchDirectories = GetSearchDirectories("TestBinder");

        ServiceProvider provider = CreateStreamsContainerWithISinkBinding(searchDirectories, "spring.cloud.stream.bindings.input.consumer.autostartup=false")
            .BuildServiceProvider();

        await provider.GetRequiredService<ILifecycleProcessor>().OnRefreshAsync(); // Only starts Autostart

        var service = provider.GetService<BindingService>();
        IDictionary<string, List<IBinding>> bindings = service.ConsumerBindings;
        bindings.TryGetValue("input", out List<IBinding> inputBindings);
        Assert.Single(inputBindings);
        IBinding binding = inputBindings[0];
        Assert.False(binding.IsRunning);
    }

    private sealed class FooBinder : IBinder<SomeBindableType>
    {
        public string ServiceName { get; set; } = "foobinder";

        public Type TargetType => typeof(SomeBindableType);

        public IBinding BindConsumer(string name, string group, SomeBindableType inboundTarget, IConsumerOptions consumerOptions)
        {
            throw new InvalidOperationException();
        }

        public IBinding BindConsumer(string name, string group, object inboundTarget, IConsumerOptions consumerOptions)
        {
            throw new InvalidOperationException();
        }

        public IBinding BindProducer(string name, SomeBindableType outboundTarget, IProducerOptions producerOptions)
        {
            throw new InvalidOperationException();
        }

        public IBinding BindProducer(string name, object outboundTarget, IProducerOptions producerOptions)
        {
            throw new InvalidOperationException();
        }

        public void Dispose()
        {
        }
    }

    private sealed class SomeBindableType
    {
    }
}
