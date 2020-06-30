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
