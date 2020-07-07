// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Stream.Binding
{
    public class BindingServiceTest : AbstractTest
    {
        [Fact]
        public void TestDefaultGroup()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var provider = CreateStreamsContainer(searchDirectories, "spring.cloud.stream.bindings.input.destination=foo")
                .BuildServiceProvider();

            var factory = provider.GetService<IBinderFactory>();
            var binder = factory.GetBinder("mock");

            IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
            var mockBinder = Mock.Get<IBinder>(binder);

            var service = provider.GetService<BindingService>();
            var bindings = service.BindConsumer(inputChannel, "input");

            Assert.Single(bindings);
            var binding = bindings.First();
            var mockBinding = Mock.Get(binding);

            service.UnbindConsumers("input");

            mockBinder.Verify(b => b.BindConsumer("foo", null, inputChannel, It.IsAny<ConsumerOptions>()));
            mockBinding.Verify(b => b.Unbind());
        }

        [Fact]
        public void TestMultipleConsumerBindings()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var provider = CreateStreamsContainer(
                searchDirectories,
                "spring.cloud.stream.bindings.input.destination=foo,bar")
                .BuildServiceProvider();

            var factory = provider.GetService<IBinderFactory>();
            var binder = factory.GetBinder("mock");

            IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
            var mockBinder = Mock.Get<IBinder>(binder);

            var service = provider.GetService<BindingService>();
            var bindings = service.BindConsumer(inputChannel, "input");

            var bindingsAsList = bindings.ToList();

            Assert.Equal(2, bindingsAsList.Count);
            var binding1 = bindingsAsList[0];
            var binding2 = bindingsAsList[1];
            var mockBinding1 = Mock.Get(binding1);
            var mockBinding2 = Mock.Get(binding2);

            service.UnbindConsumers("input");

            mockBinder.Verify(b => b.BindConsumer("foo", null, inputChannel, It.IsAny<ConsumerOptions>()));
            mockBinder.Verify(b => b.BindConsumer("bar", null, inputChannel, It.IsAny<ConsumerOptions>()));

            mockBinding1.Verify(b => b.Unbind());
            mockBinding2.Verify(b => b.Unbind());
        }

        [Fact]
        public void TestConsumerBindingWhenMultiplexingIsEnabled()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var provider = CreateStreamsContainer(
                searchDirectories,
                "spring.cloud.stream.bindings.input.destination=foo,bar",
                "spring.cloud.stream.bindings.input.consumer.multiplex=true")
                .BuildServiceProvider();

            var factory = provider.GetService<IBinderFactory>();
            var binder = factory.GetBinder("mock");

            IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
            var mockBinder = Mock.Get<IBinder>(binder);

            var service = provider.GetService<BindingService>();
            var bindings = service.BindConsumer(inputChannel, "input");

            Assert.Single(bindings);
            var binding = bindings.First();
            var mockBinding = Mock.Get(binding);

            service.UnbindConsumers("input");

            mockBinder.Verify(b => b.BindConsumer("foo,bar", null, inputChannel, It.IsAny<ConsumerOptions>()));
            mockBinding.Verify(b => b.Unbind());
        }

        [Fact]
        public void TestExplicitGroup()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var provider = CreateStreamsContainer(
                searchDirectories,
                "spring.cloud.stream.bindings.input.destination=foo",
                "spring.cloud.stream.bindings.input.group=fooGroup")
                .BuildServiceProvider();

            var factory = provider.GetService<IBinderFactory>();
            var binder = factory.GetBinder("mock");

            IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
            var mockBinder = Mock.Get<IBinder>(binder);

            var service = provider.GetService<BindingService>();
            var bindings = service.BindConsumer(inputChannel, "input");

            Assert.Single(bindings);
            var binding = bindings.First();
            var mockBinding = Mock.Get(binding);

            service.UnbindConsumers("input");

            mockBinder.Verify(b => b.BindConsumer("foo", "fooGroup", inputChannel, It.IsAny<ConsumerOptions>()));
            mockBinding.Verify(b => b.Unbind());
        }

        [Fact]
        public void TestProducerPropertiesValidation()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var provider = CreateStreamsContainer(
                searchDirectories,
                "spring.cloud.stream.bindings.output.destination=foo",
                "spring.cloud.stream.bindings.output.producer.partitionCount=0")
                .BuildServiceProvider();

            var factory = provider.GetService<IBinderFactory>();
            var binder = factory.GetBinder("mock");

            IMessageChannel outputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
            var mockBinder = Mock.Get<IBinder>(binder);

            var service = provider.GetService<BindingService>();
            Assert.Throws<InvalidOperationException>(() => service.BindProducer(outputChannel, "output"));
        }

        [Fact]
        public async Task TestDefaultPropertyBehavior()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var container = CreateStreamsContainerWithBinding(
                searchDirectories,
                typeof(IFooBinding),
                "spring.cloud.stream.default.contentType=text/plain",
                "spring.cloud.stream.bindings.input1.contentType=application/json",
                "spring.cloud.stream.default.group=foo",
                "spring.cloud.stream.bindings.input2.group=bar",
                "spring.cloud.stream.default.consumer.concurrency=5",
                "spring.cloud.stream.bindings.input2.consumer.concurrency=1",
                "spring.cloud.stream.bindings.input1.consumer.partitioned=true",
                "spring.cloud.stream.default.producer.partitionCount=10",
                "spring.cloud.stream.bindings.output2.producer.partitionCount=1",
                "spring.cloud.stream.bindings.inputXyz.contentType=application/json",
                "spring.cloud.stream.bindings.inputFooBar.contentType=application/avro",
                "spring.cloud.stream.bindings.input_snake_case.contentType=application/avro");

            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var bindServiceOptions = provider.GetService<IOptionsMonitor<BindingServiceOptions>>();
            var bindings = bindServiceOptions.CurrentValue.Bindings;

            Assert.Equal("application/json", bindings["input1"].ContentType.ToString());
            Assert.Equal("text/plain", bindings["input2"].ContentType.ToString());
            Assert.Equal("foo", bindings["input1"].Group);
            Assert.Equal("bar", bindings["input2"].Group);
            Assert.Equal(5, bindings["input1"].Consumer.Concurrency);
            Assert.Equal(1, bindings["input2"].Consumer.Concurrency);
            Assert.True(bindings["input1"].Consumer.Partitioned);
            Assert.False(bindings["input2"].Consumer.Partitioned);
            Assert.Equal(10, bindings["output1"].Producer.PartitionCount);
            Assert.Equal(1, bindings["output2"].Producer.PartitionCount);
            Assert.Equal("application/json", bindings["inputXyz"].ContentType.ToString());
            Assert.Equal("application/avro", bindings["inputFooBar"].ContentType.ToString());
            Assert.Equal("text/plain", bindings["inputFooBarBuzz"].ContentType.ToString());
            Assert.Equal("application/avro", bindings["input_snake_case"].ContentType.ToString());
        }

        [Fact]
        public void TestConsumerPropertiesValidation()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var provider = CreateStreamsContainer(
                searchDirectories,
                "spring.cloud.stream.bindings.input.destination=foo",
                "spring.cloud.stream.bindings.input.consumer.concurrency=0")
                .BuildServiceProvider();

            var factory = provider.GetService<IBinderFactory>();
            var binder = factory.GetBinder("mock");

            IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());

            var service = provider.GetService<BindingService>();
            Assert.Throws<InvalidOperationException>(() => service.BindConsumer(inputChannel, "input"));
        }

        [Fact]
        public void TestUnknownBinderOnBindingFailure()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var provider = CreateStreamsContainer(
                searchDirectories,
                "spring.cloud.stream.bindings.input.destination=fooInput",
                "spring.cloud.stream.bindings.input.binder=mock",
                "spring.cloud.stream.bindings.output.destination=fooOutput",
                "spring.cloud.stream.bindings.output.binder=mockError")
                .BuildServiceProvider();

            IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
            IMessageChannel outputChannel = new DirectChannel(provider.GetService<IApplicationContext>());

            var service = provider.GetService<BindingService>();
            var binding1 = service.BindConsumer(inputChannel, "input");

            Assert.Throws<InvalidOperationException>(() => service.BindProducer(outputChannel, "output"));
        }

        [Fact]
        public void TestUnrecognizedBinderAllowedIfNotUsed()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var mockBinder = "Steeltoe.Stream.MockBinder.Startup" + "," + "Steeltoe.Stream.MockBinder";
            var mockAssembly = searchDirectories[0] + Path.DirectorySeparatorChar + "Steeltoe.Stream.MockBinder.dll";
            var provider = CreateStreamsContainer(
                searchDirectories,
                "spring.cloud.stream.bindings.input.destination=fooInput",
                "spring.cloud.stream.bindings.output.destination=fooOutput",
                "spring.cloud.stream.defaultBinder=mock1",
                "spring.cloud.stream.binders.mock1.configureclass=" + mockBinder,
                "spring.cloud.stream.binders.mock1.configureassembly=" + mockAssembly,
                "spring.cloud.stream.binders.kafka1.configureclass=kafka")
                .BuildServiceProvider();

            IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
            IMessageChannel outputChannel = new DirectChannel(provider.GetService<IApplicationContext>());

            var service = provider.GetService<BindingService>();
            _ = service.BindConsumer(inputChannel, "input");
            _ = service.BindProducer(outputChannel, "output");
        }

        [Fact]
        public void TestResolveBindableType()
        {
            var bindableType = GenericsUtils.GetParameterType(typeof(FooBinder), typeof(IBinder<>), 0);
            Assert.Same(typeof(SomeBindableType), bindableType);
        }

        [Fact]
        public void TestLateBindingConsumer()
        {
            var searchDirectories = GetSearchDirectories("StubBinder1");
            var provider = CreateStreamsContainer(
                searchDirectories,
                "spring.cloud.stream.bindings.input.destination=foo",
                "spring.cloud.stream.bindingretryinterval=1")
                .BuildServiceProvider();

            var service = provider.GetService<BindingService>();

            var factory = provider.GetService<IBinderFactory>();
            var binder = factory.GetBinder("binder1");
            var prop = binder.GetType().GetProperty("BindConsumerFunc");
            var fail = new CountdownEvent(2);
            var mockBinding = new Mock<IBinding>();

            Func<string, string, object, IConsumerOptions, IBinding> func = (name, group, target, options) =>
            {
                fail.Signal();
                if (fail.CurrentCount == 1)
                {
                    throw new Exception("fail");
                }

                return mockBinding.Object;
            };
            prop.GetSetMethod().Invoke(binder, new object[] { func });

            IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
            var bindings = service.BindConsumer(inputChannel, "input");
            Assert.True(fail.IsSet);
            Assert.Single(bindings);
            Assert.Same(mockBinding.Object, bindings.Single());
            service.UnbindConsumers("input");
            mockBinding.Verify((b) => b.Unbind());
        }

        [Fact]
        public void TestLateBindingProducer()
        {
            var searchDirectories = GetSearchDirectories("StubBinder1");
            var provider = CreateStreamsContainer(
                searchDirectories,
                "spring.cloud.stream.bindings.output.destination=foo",
                "spring.cloud.stream.bindingretryinterval=1")
                .BuildServiceProvider();

            var service = provider.GetService<BindingService>();

            var factory = provider.GetService<IBinderFactory>();
            var binder = factory.GetBinder("binder1");

            var fail = new CountdownEvent(2);
            var mockBinding = new Mock<IBinding>();
            var prop = binder.GetType().GetProperty("BindProducerFunc");

            Func<string, object, IProducerOptions, IBinding> func = (name, target, options) =>
           {
               fail.Signal();
               if (fail.CurrentCount == 1)
               {
                   throw new Exception("fail");
               }

               return mockBinding.Object;
           };
            prop.GetSetMethod().Invoke(binder, new object[] { func });

            IMessageChannel inputChannel = new DirectChannel(provider.GetService<IApplicationContext>());
            var binding = service.BindProducer(inputChannel, "output");

            Assert.True(fail.IsSet);
            Assert.NotNull(binding);
            Assert.Same(mockBinding.Object, binding);
            service.UnbindProducers("output");
            mockBinding.Verify((b) => b.Unbind());
        }

        [Fact]
        public async Task TestBindingAutostartup()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            var provider = CreateStreamsContainerWithISinkBinding(
                searchDirectories,
                "spring.cloud.stream.bindings.input.consumer.autostartup=false")
                .BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var service = provider.GetService<BindingService>();
            var bindings = service._consumerBindings;
            bindings.TryGetValue("input", out var inputBindings);
            Assert.Single(inputBindings);
            var binding = inputBindings[0];
            Assert.False(binding.IsRunning);
        }

        private class FooBinder : IBinder<SomeBindableType>
        {
            public string Name => "foobinder";

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

        private class SomeBindableType
        {
        }
    }
}
