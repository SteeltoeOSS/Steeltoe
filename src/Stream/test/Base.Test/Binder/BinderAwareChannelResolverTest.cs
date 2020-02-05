// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

            var testChannel = new DirectChannel(serviceProvider, "INPUT");
            var latch = new CountdownEvent(1);
            IList<IMessage> received = new List<IMessage>();
            testChannel.Subscribe(new LatchedMessageHandler()
            {
                Latch = latch,
                Received = received
            });

            binder.BindConsumer("foo", null, testChannel, GetConsumerOptions());
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
            var registry = serviceProvider.GetService<IDestinationRegistry>();
            var bean = registry.Lookup("other");
            Assert.Same(bean, other);

            // this.context.close();
        }

        private class LatchedMessageHandler : IMessageHandler
        {
            public CountdownEvent Latch { get; set; }

            public IList<IMessage> Received { get; set; }

            public LatchedMessageHandler()
            {
            }

            public void HandleMessage(IMessage message)
            {
                Received.Add(message);
                Latch.Signal();
            }
        }
    }
}
