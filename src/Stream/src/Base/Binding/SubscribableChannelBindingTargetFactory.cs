// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using Steeltoe.Messaging.Support;
using Steeltoe.Stream.Messaging;
using System;

namespace Steeltoe.Stream.Binding
{
    public class SubscribableChannelBindingTargetFactory : AbstractBindingTargetFactory<ISubscribableChannel>
    {
        private readonly IMessageChannelConfigurer _messageChannelConfigurer;
        private readonly Lazy<IDestinationRegistry> _registry;

        public SubscribableChannelBindingTargetFactory(IServiceProvider serviceProvider, CompositeMessageChannelConfigurer messageChannelConfigurer)
            : base(serviceProvider)
        {
            _messageChannelConfigurer = messageChannelConfigurer;
            _registry = new Lazy<IDestinationRegistry>(() => (IDestinationRegistry)serviceProvider.GetService(typeof(IDestinationRegistry)));
        }

        public override ISubscribableChannel CreateInput(string name)
        {
            var chan = new DirectWithAttributesChannel(_serviceProvider);
            chan.Name = name;
            chan.SetAttribute("type", "input");
            _messageChannelConfigurer.ConfigureInputChannel(chan, name);

            AddChannelInterceptors(chan);

            if (_registry.Value != null)
            {
                _registry.Value.Register(name, chan);
            }

            return chan;
        }

        public override ISubscribableChannel CreateOutput(string name)
        {
            var chan = new DirectWithAttributesChannel(_serviceProvider);
            chan.Name = name;
            chan.SetAttribute("type", "output");
            _messageChannelConfigurer.ConfigureOutputChannel(chan, name);

            AddChannelInterceptors(chan);

            if (_registry.Value != null)
            {
                _registry.Value.Register(name, chan);
            }

            return chan;
        }

        private void AddChannelInterceptors(IMessageChannel chan)
        {
            var aware = chan as IChannelInterceptorAware;
            if (aware != null)
            {
                var interceptors = _serviceProvider.GetServices<IChannelInterceptor>();
                foreach (var interceptor in interceptors)
                {
                    aware.AddInterceptor(interceptor);
                }
            }
        }
    }
}
