// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Binding;
using Steeltoe.Stream.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;
using IntChannel = Steeltoe.Integration.Channel;

namespace Steeltoe.Stream.Binder;

public class BinderAwareChannelResolverTest : AbstractTest
{
    private readonly IServiceProvider _serviceProvider;
    private readonly BinderAwareChannelResolver _resolver;
    private readonly IBinder<IMessageChannel> _binder;
    private readonly SubscribableChannelBindingTargetFactory _bindingTargetFactory;
    private readonly IOptions<BindingServiceOptions> _options;
    private readonly IApplicationContext _context;

    public BinderAwareChannelResolverTest()
    {
        var searchDirectories = GetSearchDirectories("TestBinder");
        var container = CreateStreamsContainer(searchDirectories, "spring:cloud:stream:defaultBinder=testbinder");
        container.AddSingleton<IChannelInterceptor, ImmutableMessageChannelInterceptor>();
        _serviceProvider = container.BuildServiceProvider();

        _binder = _serviceProvider.GetServices<IBinder>().OfType<IBinder<IMessageChannel>>().Single();
        _bindingTargetFactory = _serviceProvider.GetServices<IBindingTargetFactory>().OfType<SubscribableChannelBindingTargetFactory>().Single();
        _resolver = _serviceProvider.GetService<IDestinationResolver<IMessageChannel>>() as BinderAwareChannelResolver;
        _options = _serviceProvider.GetService<IOptions<BindingServiceOptions>>();
        _context = _serviceProvider.GetService<IApplicationContext>();

        Assert.NotNull(_binder);
        Assert.NotNull(_bindingTargetFactory);
        Assert.NotNull(_resolver);
        Assert.NotNull(_options);
    }

    [Fact]
    public void ResolveChannel()
    {
        var bindables = _serviceProvider.GetServices<IBindable>();
        Assert.Single(bindables);
        var bindable = bindables.Single();
        Assert.Empty(bindable.Inputs);
        Assert.Empty(bindable.Outputs);
        var registered = _resolver.ResolveDestination("foo");

        var interceptors = ((IntChannel.AbstractMessageChannel)registered).ChannelInterceptors;
        Assert.Equal(2, interceptors.Count);
        Assert.IsType<ImmutableMessageChannelInterceptor>(interceptors[1]);

        Assert.Empty(bindable.Inputs);
        Assert.Single(bindable.Outputs);

        var testChannel = new DirectChannel(_context, "INPUT");
        var latch = new CountdownEvent(1);
        IList<IMessage> received = new List<IMessage>();
        testChannel.Subscribe(new LatchedMessageHandler
        {
            Latch = latch,
            Received = received
        });

        _binder.BindConsumer("foo", null, testChannel, GetConsumerOptions("testbinding"));
        Assert.Empty(received);
        registered.Send(MessageBuilder.WithPayload("hello").Build());
        latch.Wait(1000);

        Assert.Single(received);
        var payload = received[0].Payload as byte[];
        Assert.Equal("hello", Encoding.UTF8.GetString(payload));
    }

    [Fact]
    public void ResolveNonRegisteredChannel()
    {
        var other = _resolver.ResolveDestination("other");
        var registry = _serviceProvider.GetService<IApplicationContext>();
        var bean = registry.GetService<IMessageChannel>("other");
        Assert.Same(bean, other);

        // this.context.close();
    }

    private sealed class LatchedMessageHandler : IMessageHandler
    {
        public CountdownEvent Latch { get; set; }

        public IList<IMessage> Received { get; set; }

        public LatchedMessageHandler()
        {
            ServiceName = $"{GetType().Name}@{GetHashCode()}";
        }

        public string ServiceName { get; set; }

        public void HandleMessage(IMessage message)
        {
            Received.Add(message);
            Latch.Signal();
        }
    }
}
