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
using Microsoft.Extensions.Logging;
using Steeltoe.Common.LoadBalancer;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Common.Http.LoadBalancer
{
    /// <summary>
    /// Same as <see cref="LoadBalancerHttpClientHandler"/> except is a <see cref="DelegatingHandler"/>, for use with HttpClientFactory
    /// </summary>
    public class LoadBalancerDelegatingHandler : DelegatingHandler
    {
        private readonly ILoadBalancer _loadBalancer;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoadBalancerDelegatingHandler"/> class. <para />
        /// For use with <see cref="IHttpClientBuilder"/>
        /// </summary>
        /// <param name="loadBalancer">Load balancer to use</param>
        /// <param name="logger">For logging</param>
        public LoadBalancerDelegatingHandler(ILoadBalancer loadBalancer, ILogger logger = null)
        {
            _loadBalancer = loadBalancer ?? throw new ArgumentNullException(nameof(loadBalancer));
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // record the original request
            var originalUri = request.RequestUri;
            Uri resolvedUri = null;
            DateTime startTime = default(DateTime);
            DateTime endTime = default(DateTime);

            try
            {
                // look up a service instance and update the request
                resolvedUri = await _loadBalancer.ResolveServiceInstanceAsync(request.RequestUri);
                request.RequestUri = resolvedUri;

                // allow other handlers to operate and the request to continue
                startTime = DateTime.UtcNow;
                var response = await base.SendAsync(request, cancellationToken);
                endTime = DateTime.UtcNow;

                // track stats
                await _loadBalancer.UpdateStatsAsync(originalUri, resolvedUri, endTime - startTime, null);
                return response;
            }
            catch (Exception exception)
            {
                if (endTime == default(DateTime))
                {
                    endTime = DateTime.UtcNow;
                }

                _logger?.LogDebug(exception, "Exception during SendAsync()");
                if (resolvedUri != null)
                {
                    await _loadBalancer.UpdateStatsAsync(originalUri, resolvedUri, endTime - startTime, exception);
                }
                else
                {
                    _logger?.LogWarning("resolvedUri was null. This might be an issue with your service discovery provider.");
                }

                throw;
            }
            finally
            {
                request.RequestUri = originalUri;
            }
        }
    }
}
