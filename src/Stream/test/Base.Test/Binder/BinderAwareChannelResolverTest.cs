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

namespace Steeltoe.Stream.Binder
{
    public class BinderAwareChannelResolverTest : AbstractTest
    {
        private readonly IServiceProvider serviceProvider;
        private readonly BinderAwareChannelResolver resolver;
        private readonly IBinder<IMessageChannel> binder;
        private readonly SubscribableChannelBindingTargetFactory bindingTargetFactory;
        private readonly IOptions<BindingServiceOptions> options;
        private readonly IApplicationContext context;

        public BinderAwareChannelResolverTest()
        {
            var searchDirectories = GetSearchDirectories("TestBinder");
            var container = CreateStreamsContainer(searchDirectories, "spring:cloud:stream:defaultBinder=testbinder");
            container.AddSingleton<IChannelInterceptor, ImmutableMessageChannelInterceptor>();
            serviceProvider = container.BuildServiceProvider();

            binder = serviceProvider.GetServices<IBinder>().OfType<IBinder<IMessageChannel>>().Single();
            bindingTargetFactory = serviceProvider.GetServices<IBindingTargetFactory>().OfType<SubscribableChannelBindingTargetFactory>().Single();
            resolver = serviceProvider.GetService<IDestinationResolver<IMessageChannel>>() as BinderAwareChannelResolver;
            options = serviceProvider.GetService<IOptions<BindingServiceOptions>>();
            context = serviceProvider.GetService<IApplicationContext>();

            Assert.NotNull(binder);
            Assert.NotNull(bindingTargetFactory);
            Assert.NotNull(resolver);
            Assert.NotNull(options);
        }

        [Fact]
        public void ResolveChannel()
        {
            var bindables = serviceProvider.GetServices<IBindable>();
            Assert.Single(bindables);
            var bindable = bindables.Single();
            Assert.Empty(bindable.Inputs);
            Assert.Empty(bindable.Outputs);
            var registered = resolver.ResolveDestination("foo");

            var interceptors = ((IntChannel.AbstractMessageChannel)registered).ChannelInterceptors;
            Assert.Equal(2, interceptors.Count);
            Assert.IsType<ImmutableMessageChannelInterceptor>(interceptors[1]);

            Assert.Empty(bindable.Inputs);
            Assert.Single(bindable.Outputs);

            var testChannel = new DirectChannel(context, "INPUT");
            var latch = new CountdownEvent(1);
            IList<IMessage> received = new List<IMessage>();
            testChannel.Subscribe(new LatchedMessageHandler()
            {
                Latch = latch,
                Received = received
            });

            binder.BindConsumer("foo", null, testChannel, GetConsumerOptions("testbinding"));
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
            var other = resolver.ResolveDestination("other");
            var registry = serviceProvider.GetService<IApplicationContext>();
            var bean = registry.GetService<IMessageChannel>("other");
            Assert.Same(bean, other);

            // this.context.close();
        }

        private class LatchedMessageHandler : IMessageHandler
        {
            public CountdownEvent Latch { get; set; }

            public IList<IMessage> Received { get; set; }

            public LatchedMessageHandler()
            {
                ServiceName = GetType().Name + "@" + GetHashCode();
            }

            public virtual string ServiceName { get; set; }

            public void HandleMessage(IMessage message)
            {
                Received.Add(message);
                Latch.Signal();
            }
        }
    }
}
