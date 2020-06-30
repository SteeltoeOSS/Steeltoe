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
using Steeltoe.Common.Contexts;
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

        public SubscribableChannelBindingTargetFactory(IApplicationContext context, CompositeMessageChannelConfigurer messageChannelConfigurer)
            : base(context)
        {
            _messageChannelConfigurer = messageChannelConfigurer;
            _registry = new Lazy<IDestinationRegistry>(() => (IDestinationRegistry)context.GetService(typeof(IDestinationRegistry)));
        }

        public override ISubscribableChannel CreateInput(string name)
        {
            var chan = new DirectWithAttributesChannel(_context);
            chan.ServiceName = name;
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
            var chan = new DirectWithAttributesChannel(_context);
            chan.ServiceName = name;
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
                var interceptors = _context.GetServices<IChannelInterceptor>();
                foreach (var interceptor in interceptors)
                {
                    aware.AddInterceptor(interceptor);
                }
            }
        }
    }
}
