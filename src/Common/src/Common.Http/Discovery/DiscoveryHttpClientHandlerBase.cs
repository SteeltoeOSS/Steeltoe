﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.LoadBalancer;
using Steeltoe.Discovery;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Common.Discovery
{
    public class DiscoveryHttpClientHandlerBase
    {
        protected IDiscoveryClient _client;
        protected ILoadBalancer _loadBalancer;
        protected ILogger _logger;

        public DiscoveryHttpClientHandlerBase(IDiscoveryClient client, ILogger logger = null, ILoadBalancer loadBalancer = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _loadBalancer = loadBalancer ?? new RandomLoadBalancer(client);
            _logger = logger;
        }

        public virtual Uri LookupService(Uri current)
        {
            _logger?.LogDebug("LookupService({0})", current.ToString());
            if (!current.IsDefaultPort)
            {
                return current;
            }

            return _loadBalancer.ResolveServiceInstanceAsync(current).GetAwaiter().GetResult();
        }

        public virtual async Task<Uri> LookupServiceAsync(Uri current)
        {
            _logger?.LogDebug("LookupService({0})", current.ToString());
            if (!current.IsDefaultPort)
            {
                return current;
            }

            return await _loadBalancer.ResolveServiceInstanceAsync(current).ConfigureAwait(false);
        }
    }
}
