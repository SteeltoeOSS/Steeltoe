// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
