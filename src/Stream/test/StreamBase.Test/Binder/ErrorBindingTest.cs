﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Moq;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Attributes;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.Extensions;
using Steeltoe.Stream.TestBinder;
using System;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Steeltoe.Stream.Binder
{
    public class ErrorBindingTest : AbstractTest
    {
        [Fact]
        public async Task TestErrorChannelNotBoundByDefault()
        {
            var searchDirectories = GetSearchDirectories("MockBinder");
            var provider = CreateStreamsContainerWithIProcessorBinding(searchDirectories, "spring:cloud:stream:defaultBinder=mock")
                .BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var factory = provider.GetService<IBinderFactory>();
            Assert.NotNull(factory);
            var binder = factory.GetBinder(null);
            Assert.NotNull(binder);
            var mock = Mock.Get(binder);
            mock.Verify(b => b.BindConsumer("input", null, It.IsAny<IMessageChannel>(), It.IsAny<ConsumerOptions>()));
            mock.Verify(b => b.BindProducer("output", It.IsAny<IMessageChannel>(), It.IsAny<ProducerOptions>()));
        }

        [Fact]
        public async Task TestConfigurationWithDefaultErrorHandler()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            var container = CreateStreamsContainerWithIProcessorBinding(searchDirectories, "spring:cloud:stream:bindings:input:consumer:maxAttempts=1");
            container.AddStreamListeners<ErrorConfigurationDefault>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            SendAndValidate_ErrorConfigurationDefault(provider);
        }

        [Fact]
        public async Task TestConfigurationWithCustomErrorHandler()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            var container = CreateStreamsContainerWithIProcessorBinding(searchDirectories, "spring:cloud:stream:bindings:input:consumer:maxAttempts=1");
            container.AddStreamListeners<ErrorConfigurationWithCustomErrorHandler>();
            var provider = container.BuildServiceProvider();

            await provider.GetRequiredService<ILifecycleProcessor>().OnRefresh(); // Only starts Autostart

            var streamProcessor = provider.GetRequiredService<StreamListenerAttributeProcessor>();
            streamProcessor.AfterSingletonsInstantiated();

            SendAndValidate_ErrorConfigurationWithCustomErrorHandler(provider);
        }

        private void SendAndValidate_ErrorConfigurationWithCustomErrorHandler(IServiceProvider provider)
        {
            var source = provider.GetService<InputDestination>();
            source.Send(Message.Create<byte[]>(Encoding.UTF8.GetBytes("Hello1")));
            source.Send(Message.Create<byte[]>(Encoding.UTF8.GetBytes("Hello2")));
            source.Send(Message.Create<byte[]>(Encoding.UTF8.GetBytes("Hello3")));

            var errorConfig = provider.GetService<ErrorConfigurationWithCustomErrorHandler>();
            Assert.NotNull(errorConfig);
            Assert.Equal(6, errorConfig.Counter);
        }

        private void SendAndValidate_ErrorConfigurationDefault(IServiceProvider provider)
        {
            var source = provider.GetService<InputDestination>();
            source.Send(Message.Create<byte[]>(Encoding.UTF8.GetBytes("Hello1")));
            source.Send(Message.Create<byte[]>(Encoding.UTF8.GetBytes("Hello2")));
            source.Send(Message.Create<byte[]>(Encoding.UTF8.GetBytes("Hello3")));

            var errorConfig = provider.GetService<ErrorConfigurationDefault>();
            Assert.NotNull(errorConfig);
            Assert.Equal(3, errorConfig.Counter);
        }

        public class ErrorConfigurationDefault
        {
            public ErrorConfigurationDefault()
            {
            }

            public int Counter;

            [StreamListener("input")]
            public void Handle(object value)
            {
                Counter++;
                throw new Exception("BOOM!");
            }
        }

        public class ErrorConfigurationWithCustomErrorHandler
        {
            public ErrorConfigurationWithCustomErrorHandler()
            {
            }

            public int Counter;

            [StreamListener("input")]
            public void Handle(object value)
            {
                Counter++;
                throw new Exception("BOOM!");
            }

            [StreamListener("input.anonymous.errors")]
            public void Error(IMessage message)
            {
                Counter++;
            }
        }
    }
}
