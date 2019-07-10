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

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Discovery;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Common.LoadBalancer
{
    public class RandomLoadBalancer : ILoadBalancer
    {
        private static readonly Random _random = new Random();
        private readonly IServiceInstanceProvider _serviceInstanceProvider;
        private readonly IDistributedCache _distributedCache;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomLoadBalancer"/> class.
        /// Returns random service instances, with option caching of service lookups
        /// </summary>
        /// <param name="serviceInstanceProvider">Provider of service instance information</param>
        /// <param name="distributedCache">For caching service instance data</param>
        /// <param name="logger">For logging</param>
        public RandomLoadBalancer(IServiceInstanceProvider serviceInstanceProvider, IDistributedCache distributedCache = null, ILogger logger = null)
        {
            _serviceInstanceProvider = serviceInstanceProvider ?? throw new ArgumentNullException(nameof(serviceInstanceProvider));
            _distributedCache = distributedCache;
            _logger = logger;
        }

        public virtual async Task<Uri> ResolveServiceInstanceAsync(Uri request)
        {
            _logger?.LogTrace("ResolveServiceInstance {serviceInstance}", request.Host);
            var availableServiceInstances = await _serviceInstanceProvider.GetInstancesWithCacheAsync(request.Host, _distributedCache).ConfigureAwait(false);
            if (availableServiceInstances.Count > 0)
            {
                // load balancer instance selection predictability is not likely to be a security concern
                var resolvedUri = availableServiceInstances[_random.Next(availableServiceInstances.Count)].Uri;
                _logger?.LogDebug("Resolved {url} to {service}", request.Host, resolvedUri.Host);
                return new Uri(resolvedUri, request.PathAndQuery);
            }
            else
            {
                _logger?.LogWarning("Attempted to resolve service for {url} but found 0 instances", request.Host);
                return request;
            }
        }

        public virtual Task UpdateStatsAsync(Uri originalUri, Uri resolvedUri, TimeSpan responseTime, Exception exception)
        {
            return Task.CompletedTask;
        }
    }
}
