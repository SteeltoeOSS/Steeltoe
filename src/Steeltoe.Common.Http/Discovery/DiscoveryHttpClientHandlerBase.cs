// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.LoadBalancer;
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

            return Task.Run(async () => await _loadBalancer.ResolveServiceInstanceAsync(current)).Result;
        }

        public virtual async Task<Uri> LookupServiceAsync(Uri current)
        {
            _logger?.LogDebug("LookupService({0})", current.ToString());
            if (!current.IsDefaultPort)
            {
                return current;
            }

            return await _loadBalancer.ResolveServiceInstanceAsync(current);
        }
    }
}
