// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;

namespace Steeltoe.Messaging.Core
{
    public class CachingDestinationResolverProxy<D> : IDestinationResolver<D>
    {
        private readonly ConcurrentDictionary<string, D> _resolvedDestinationCache = new ();

        private IDestinationResolver<D> _targetDestinationResolver;

        public CachingDestinationResolverProxy(IDestinationResolver<D> targetDestinationResolver)
        {
            if (targetDestinationResolver == null)
            {
                throw new ArgumentNullException(nameof(targetDestinationResolver));
            }

            _targetDestinationResolver = targetDestinationResolver;
        }

        public D ResolveDestination(string name)
        {
            _resolvedDestinationCache.TryGetValue(name, out var destination);
            if (destination == null)
            {
                destination = _targetDestinationResolver.ResolveDestination(name);
                _resolvedDestinationCache.TryAdd(name, destination);
            }

            return destination;
        }

        object IDestinationResolver.ResolveDestination(string name)
        {
            var result = ResolveDestination(name);
            return result;
        }
    }
}
