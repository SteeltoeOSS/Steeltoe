// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.Contexts;
using System;
using System.Collections.Concurrent;

namespace Steeltoe.Messaging.Core
{
    public sealed class DefaultDestinationRegistry : IDestinationRegistry
    {
        private readonly ConcurrentDictionary<string, object> _destinations = new ConcurrentDictionary<string, object>();

        public IApplicationContext ApplicationContext { get; }

        public DefaultDestinationRegistry(IApplicationContext context)
        {
            ApplicationContext = context;
        }

        public object Deregister(string name)
        {
            _destinations.TryRemove(name, out var destination);

            if (destination is IDisposable)
            {
                ((IDisposable)destination).Dispose();
            }

            return destination;
        }

        public void Register(string name, object destination)
        {
            _ = _destinations.AddOrUpdate(name, destination, (k, v) => destination);
        }

        public object Lookup(string name)
        {
            if (_destinations.TryGetValue(name, out var destination))
            {
                return destination;
            }

            return LookupFromServices(name);
        }

        public bool Contains(string name)
        {
            return Lookup(name) != null;
        }

        public void Dispose()
        {
            var destinations = _destinations.Values;
            _destinations.Clear();
            foreach (var dest in destinations)
            {
                if (dest is IDisposable)
                {
                    ((IDisposable)dest).Dispose();
                }
            }
        }

        private IMessageChannel LookupFromServices(string name)
        {
            var messageChannels = ApplicationContext.GetServices<IMessageChannel>();
            foreach (var chan in messageChannels)
            {
                if (chan.ServiceName == name)
                {
                    return chan;
                }
            }

            return null;
        }
    }
}
