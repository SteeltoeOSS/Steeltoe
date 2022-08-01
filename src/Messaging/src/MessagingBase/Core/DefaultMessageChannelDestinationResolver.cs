// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;

namespace Steeltoe.Messaging.Core;

public class DefaultMessageChannelDestinationResolver : IDestinationResolver<IMessageChannel>
{
    public IApplicationContext Context { get; }

    public DefaultMessageChannelDestinationResolver(IApplicationContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public virtual IMessageChannel ResolveDestination(string name)
    {
        return Context.GetService<IMessageChannel>(name);
    }

    object IDestinationResolver.ResolveDestination(string name)
    {
        var result = ResolveDestination(name);
        return result;
    }
}
