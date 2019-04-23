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

using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Common.Discovery
{
    /// <summary>
    /// An <see cref="HttpClientHandler"/> implementation that performs Service Discovery
    /// </summary>
    public class DiscoveryHttpClientHandler : HttpClientHandler
    {
        private readonly ILogger _logger;
        private DiscoveryHttpClientHandlerBase _discoveryBase;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryHttpClientHandler"/> class.
        /// </summary>
        /// <param name="discoveryClient">Service discovery client to use - provided by calling services.AddDiscoveryClient(Configuration)</param>
        /// <param name="logger">ILogger for capturing logs from Discovery operations</param>
        public DiscoveryHttpClientHandler(IDiscoveryClient discoveryClient, ILogger logger = null)
        {
            _discoveryBase = new DiscoveryHttpClientHandlerBase(discoveryClient, logger);
            _logger = logger;
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var current = request.RequestUri;
            try
            {
                request.RequestUri = await _discoveryBase.LookupServiceAsync(current);
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
    }
}
