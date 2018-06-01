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
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Common.Discovery
{
    public class DiscoveryHttpClientHandlerBase : HttpClientHandler
    {
        protected static Random _random = new Random();
        protected IDiscoveryClient _client;
        protected ILogger _logger;

        public DiscoveryHttpClientHandlerBase(IDiscoveryClient client, ILogger logger = null)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _logger = logger;
        }

        public virtual Uri LookupService(Uri current)
        {
            _logger?.LogDebug("LookupService({0})", current.ToString());
            if (!current.IsDefaultPort)
            {
                return current;
            }

            var instances = _client.GetInstances(current.Host);
            if (instances.Count > 0)
            {
                var index = _random.Next(instances.Count);
                var result = instances[index].Uri;
                current = new Uri(result, current.PathAndQuery);
                _logger?.LogDebug("Resolved {url} to {service}", current, result);
            }
            else
            {
                _logger?.LogWarning("Attempted to resolve service for {url} but found 0 instances", current.Host);
            }

            _logger?.LogDebug("LookupService() returning {0} ", current.ToString());
            return current;
        }

        internal async Task<HttpResponseMessage> SharedSendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var current = request.RequestUri;
            try
            {
                request.RequestUri = LookupService(current);
                return await base.SendAsync(request, cancellationToken);
            }
            catch (Exception e)
            {
                _logger?.LogDebug(e, "Exception during SendAsync()");
                throw;
            }
            finally
            {
                request.RequestUri = current;
            }
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await SharedSendAsync(request, cancellationToken);
        }
    }
}
