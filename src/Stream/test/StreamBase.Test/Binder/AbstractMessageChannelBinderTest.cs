// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Dispatcher;
using Steeltoe.Integration.Handler;
using Steeltoe.Messaging;
using Steeltoe.Stream.Config;
using Steeltoe.Stream.TestBinder;
using System;
using Xunit;

namespace Steeltoe.Stream.Binder;

public class AbstractMessageChannelBinderTest : AbstractTest
{
    private readonly IServiceProvider _serviceProvider;

    public AbstractMessageChannelBinderTest()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        _serviceProvider = CreateStreamsContainerWithDefaultBindings(searchDirectories, "spring:cloud:stream:defaultBinder=testbinder")
            .BuildServiceProvider();
    }

    [Fact]
    public void TestEndpointLifecycle()
    {
        var binder = _serviceProvider.GetService<IBinder>() as TestChannelBinder;
        Assert.NotNull(binder);

        var consumerProperties = new ConsumerOptions("testbinding")
        {
            MaxAttempts = 1
        };
        consumerProperties.PostProcess("testbinding");

        // IBinding<IMessageChannel> consumerBinding = await binder.BindConsumer("foo", "fooGroup",  new DirectChannel(serviceProvider),  consumerProperties);
        var consumerBinding = binder.BindConsumer("foo", "fooGroup", new DirectChannel(_serviceProvider.GetService<IApplicationContext>()), consumerProperties);

        var defaultBinding = consumerBinding as DefaultBinding<IMessageChannel>;
        Assert.NotNull(defaultBinding);

        // lifecycle
        var messageProducer = defaultBinding.Endpoint as TestChannelBinder.TestMessageProducerSupportEndpoint;
        Assert.NotNull(messageProducer);
        Assert.True(defaultBinding.Endpoint.IsRunning);
        Assert.NotNull(messageProducer.OutputChannel);

        // lifecycle.ErrorChannel
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

        var registry = _serviceProvider.GetRequiredService<IApplicationContext>();
        Assert.True(registry.ContainsService<IMessageChannel>("foo.fooGroup.errors"));
        Assert.True(registry.ContainsService<ErrorMessageSendingRecoverer>("foo.fooGroup.errors.recoverer"));
        Assert.True(registry.ContainsService<IMessageHandler>("foo.fooGroup.errors.handler"));
        Assert.True(registry.ContainsService<IMessageHandler>("foo.fooGroup.errors.bridge"));

        consumerBinding.Unbind();

        Assert.False(registry.ContainsService<IMessageChannel>("foo.fooGroup.errors"));
        Assert.False(registry.ContainsService<ErrorMessageSendingRecoverer>("foo.fooGroup.errors.recoverer"));
        Assert.False(registry.ContainsService<IMessageHandler>("foo.fooGroup.errors.handler"));
        Assert.False(registry.ContainsService<IMessageHandler>("foo.fooGroup.errors.bridge"));

        Assert.False(defaultBinding.Endpoint.IsRunning);

        var producerProps = new ProducerOptions("testbinding")
        {
            ErrorChannelEnabled = true
        };
        producerProps.PostProcess("testbinding");

        // IBinding<IMessageChannel> producerBinding = await binder.BindProducer("bar", new DirectChannel(serviceProvider), producerProps);
        var producerBinding = binder.BindProducer("bar", new DirectChannel(_serviceProvider.GetService<IApplicationContext>()), producerProps);
        Assert.True(registry.ContainsService<IMessageChannel>("bar.errors"));
        Assert.True(registry.ContainsService<IMessageHandler>("bar.errors.bridge"));

        producerBinding.Unbind();
        Assert.False(registry.ContainsService<IMessageChannel>("bar.errors"));
        Assert.False(registry.ContainsService<IMessageHandler>("bar.errors.bridge"));
    }

    [Fact]
    public void TestEndpointBinderHasRecoverer()
    {
        var binder = _serviceProvider.GetService<IBinder>() as TestChannelBinder;
        Assert.NotNull(binder);
        var consumerBinding = binder.BindConsumer("foo", "fooGroup", new DirectChannel(_serviceProvider.GetService<IApplicationContext>()), GetConsumerOptions("testbinding"));
        var defaultBinding = consumerBinding as DefaultBinding<IMessageChannel>;
        Assert.NotNull(defaultBinding);

        // lifecycle
        var messageProducer = defaultBinding.Endpoint as TestChannelBinder.TestMessageProducerSupportEndpoint;
        Assert.NotNull(messageProducer);

        // lifecycle.ErrorChannel
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

        var registry = _serviceProvider.GetRequiredService<IApplicationContext>();
        Assert.True(registry.ContainsService<IMessageChannel>("foo.fooGroup.errors"));
        Assert.True(registry.ContainsService<ErrorMessageSendingRecoverer>("foo.fooGroup.errors.recoverer"));
        Assert.True(registry.ContainsService<IMessageHandler>("foo.fooGroup.errors.handler"));
        Assert.True(registry.ContainsService<IMessageHandler>("foo.fooGroup.errors.bridge"));

        consumerBinding.Unbind();

        Assert.False(registry.ContainsService<IMessageChannel>("foo.fooGroup.errors"));
        Assert.False(registry.ContainsService<ErrorMessageSendingRecoverer>("foo.fooGroup.errors.recoverer"));
        Assert.False(registry.ContainsService<IMessageHandler>("foo.fooGroup.errors.handler"));
        Assert.False(registry.ContainsService<IMessageHandler>("foo.fooGroup.errors.bridge"));
    }
}
