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
using Steeltoe.Common.Discovery;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Common.Http.Discovery
{
    /// <summary>
    /// A <see cref="DelegatingHandler"/> implementation that performs Service Discovery
    /// </summary>
    public class DiscoveryHttpMessageHandler : DelegatingHandler
    {
        private DiscoveryHttpClientHandlerBase discoveryBase;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryHttpMessageHandler"/> class.
        /// </summary>
        /// <param name="discoveryClient">Service discovery client to use - provided by calling services.AddDiscoveryClient(Configuration)</param>
        /// <param name="logger">ILogger for capturing logs from Discovery operations</param>
        public DiscoveryHttpMessageHandler(IDiscoveryClient discoveryClient, ILogger<DiscoveryHttpClientHandler> logger = null)
        {
            if (discoveryClient == null)
            {
                throw new ArgumentNullException(nameof(discoveryClient));
            }

            discoveryBase = new DiscoveryHttpClientHandlerBase(discoveryClient, logger);
        }

        /// <inheritdoc />
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await discoveryBase.SharedSendAsync(request, cancellationToken);
        }
    }
}
