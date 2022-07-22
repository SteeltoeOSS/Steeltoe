// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Core;
using System;

namespace Steeltoe.Integration.Support.Channel;

public class DefaultMessageChannelResolver : IDestinationResolver<IMessageChannel>
{
    private readonly IApplicationContext _context;
    private readonly IHeaderChannelRegistry _registry;

    public DefaultMessageChannelResolver(IApplicationContext context, IHeaderChannelRegistry registry = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _registry = registry;
    }

    public virtual IMessageChannel ResolveDestination(string name)
    {
        var result = _context.GetService<IMessageChannel>(name);
        if (result != null)
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