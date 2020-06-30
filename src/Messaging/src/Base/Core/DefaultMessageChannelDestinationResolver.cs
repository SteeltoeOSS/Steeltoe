// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.Core
{
    public class DefaultMessageChannelDestinationResolver : IDestinationResolver<IMessageChannel>
    {
        public IDestinationRegistry Registry { get; }

        public DefaultMessageChannelDestinationResolver(IDestinationRegistry registry)
        {
            if (registry == null)
            {
                throw new ArgumentNullException(nameof(registry));
            }

            Registry = registry;
        }

        public virtual IMessageChannel ResolveDestination(string name)
        {
            if (Registry.Lookup(name) is IMessageChannel result)
            {
                return result;
            }

            return null;
        }

        object IDestinationResolver.ResolveDestination(string name)
        {
            var result = ResolveDestination(name);
            return result;
        }
    }
}
