// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Integration;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Dispatcher;
using Steeltoe.Integration.Handler;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.TestBinder;
using System;
using Xunit;

namespace Steeltoe.Stream.Binder
{
    public class AbstractMessageChannelBinderTest : AbstractTest
    {
        private readonly IServiceProvider serviceProvider;

        public AbstractMessageChannelBinderTest()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            serviceProvider = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring:cloud:stream:defaultBinder=testbinder")
                .BuildServiceProvider();
        }

        [Fact]
        public void TestEndpointLifecycle()
        {
            var binder = serviceProvider.GetService<IBinder>() as TestChannelBinder;
            Assert.NotNull(binder);

            var consumerProperties = new ConsumerOptions()
            {
                MaxAttempts = 1
            };
            consumerProperties.PostProcess();

            // IBinding<IMessageChannel> consumerBinding = await binder.BindConsumer("foo", "fooGroup",  new DirectChannel(serviceProvider),  consumerProperties);
            var consumerBinding = binder.BindConsumer("foo", "fooGroup", new DirectChannel(serviceProvider), consumerProperties);

            var defaultBinding = consumerBinding as DefaultBinding<IMessageChannel>;
            Assert.NotNull(defaultBinding);

            // lifecycle
            var messageProducer = defaultBinding.Endpoint as TestChannelBinder.TestMessageProducerSupportEndpoint;
            Assert.NotNull(messageProducer);
            Assert.True(defaultBinding.Endpoint.IsRunning);
            Assert.NotNull(messageProducer.OutputChannel);

            // lifecycle.errorchannel
            Assert.NotNull(messageProducer.ErrorChannel);
            var errorChannel = messageProducer.ErrorChannel as PublishSubscribeChannel;
            Assert.NotNull(errorChannel.Dispatcher);

            // dispatcher.handlers
            Assert.Equal(2, errorChannel.Dispatcher.HandlerCount);
            var dispatcher = errorChannel.Dispatcher as AbstractDispatcher;
            Assert.NotNull(dispatcher);
            var handlers = dispatcher.Handlers;
            Assert.True(handlers[0] is BridgeHandler);
            Assert.True(handlers[1] is ILastSubscriberMessageHandler);

            var registry = serviceProvider.GetRequiredService<IDestinationRegistry>();
            Assert.True(registry.Contains("foo.fooGroup.errors"));
            Assert.True(registry.Contains("foo.fooGroup.errors.recoverer"));
            Assert.True(registry.Contains("foo.fooGroup.errors.handler"));
            Assert.True(registry.Contains("foo.fooGroup.errors.bridge"));

            consumerBinding.Unbind();

            Assert.False(registry.Contains("foo.fooGroup.errors"));
            Assert.False(registry.Contains("foo.fooGroup.errors.recoverer"));
            Assert.False(registry.Contains("foo.fooGroup.errors.handler"));
            Assert.False(registry.Contains("foo.fooGroup.errors.bridge"));

            Assert.False(defaultBinding.Endpoint.IsRunning);

            var producerProps = new ProducerOptions()
            {
                ErrorChannelEnabled = true
            };
            producerProps.PostProcess();

            // IBinding<IMessageChannel> producerBinding = await binder.BindProducer("bar", new DirectChannel(serviceProvider), producerProps);
            var producerBinding = binder.BindProducer("bar", new DirectChannel(serviceProvider), producerProps);
            Assert.True(registry.Contains("bar.errors"));
            Assert.True(registry.Contains("bar.errors.bridge"));

            producerBinding.Unbind();
            Assert.False(registry.Contains("bar.errors"));
            Assert.False(registry.Contains("bar.errors.bridge"));
        }

        [Fact]
        public void TestEndpointBinderHasRecoverer()
        {
            var binder = serviceProvider.GetService<IBinder>() as TestChannelBinder;
            Assert.NotNull(binder);
            var consumerBinding = binder.BindConsumer("foo", "fooGroup", new DirectChannel(serviceProvider), GetConsumerOptions());
            var defaultBinding = consumerBinding as DefaultBinding<IMessageChannel>;
            Assert.NotNull(defaultBinding);

            // lifecycle
            var messageProducer = defaultBinding.Endpoint as TestChannelBinder.TestMessageProducerSupportEndpoint;
            Assert.NotNull(messageProducer);

            // lifecycle.errorchannel
            Assert.Null(messageProducer.ErrorChannel);

            var callback = messageProducer.RecoveryCallback as ErrorMessagePublisher;
            Assert.NotNull(callback);
            var errorChannel = callback.Channel as PublishSubscribeChannel;
            Assert.NotNull(errorChannel);

            Assert.NotNull(errorChannel.Dispatcher);

            // dispatcher.handlers
            Assert.Equal(2, errorChannel.Dispatcher.HandlerCount);
            var dispatcher = errorChannel.Dispatcher as AbstractDispatcher;
            Assert.NotNull(dispatcher);
            var handlers = dispatcher.Handlers;
            Assert.True(handlers[0] is BridgeHandler);
            Assert.True(handlers[1] is ILastSubscriberMessageHandler);

            var registry = serviceProvider.GetRequiredService<IDestinationRegistry>();
            Assert.True(registry.Contains("foo.fooGroup.errors"));
            Assert.True(registry.Contains("foo.fooGroup.errors.recoverer"));
            Assert.True(registry.Contains("foo.fooGroup.errors.handler"));
            Assert.True(registry.Contains("foo.fooGroup.errors.bridge"));

            consumerBinding.Unbind();

            Assert.False(registry.Contains("foo.fooGroup.errors"));
            Assert.False(registry.Contains("foo.fooGroup.errors.recoverer"));
            Assert.False(registry.Contains("foo.fooGroup.errors.handler"));
            Assert.False(registry.Contains("foo.fooGroup.errors.bridge"));
        }
    }
}
