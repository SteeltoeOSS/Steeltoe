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

using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using System;

namespace Steeltoe.Integration.Support.Channel
{
    public class DefaultMessageChannelResolver : IDestinationResolver<IMessageChannel>
    {
        private readonly IDestinationRegistry _destinationRegistry;
        private readonly IHeaderChannelRegistry _registry;

        public DefaultMessageChannelResolver(IDestinationRegistry destinationRegistry, IHeaderChannelRegistry registry = null)
        {
            _destinationRegistry = destinationRegistry ?? throw new ArgumentNullException(nameof(destinationRegistry));
            _registry = registry;
        }

        public virtual IMessageChannel ResolveDestination(string name)
        {
            if (_destinationRegistry.Lookup(name) is IMessageChannel result)
            {
                return result;
            }

            if (_registry != null)
            {
                var channel = _registry.ChannelNameToChannel(name);
                if (channel != null)
                {
                    return channel;
                }
            }

            throw new DestinationResolutionException(
                "failed to look up MessageChannel with name '" + name
                     + "' in the Service Container"
                     + (_registry == null ? " (and there is no IHeaderChannelRegistry present)." : "."));
        }

        object IDestinationResolver.ResolveDestination(string name)
        {
            return ResolveDestination(name);
        }
    }
}
