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

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Refresh
{
    public class RefreshEndpointMiddleware : EndpointMiddleware<IList<string>>
    {
        private RequestDelegate _next;

        public RefreshEndpointMiddleware(RequestDelegate next, RefreshEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<RefreshEndpointMiddleware> logger = null)
          : base(endpoint, mgmtOptions, logger: logger)
        {
            _next = next;
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public RefreshEndpointMiddleware(RequestDelegate next, RefreshEndpoint endpoint, ILogger<RefreshEndpointMiddleware> logger = null)
            : base(endpoint, logger: logger)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                await HandleRefreshRequestAsync(context).ConfigureAwait(false);
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
        }

        protected internal async Task HandleRefreshRequestAsync(HttpContext context)
        {
            var serialInfo = HandleRequest();
            _logger?.LogDebug("Returning: {0}", serialInfo);
            context.Response.Headers.Add("Content-Type", "application/vnd.spring-boot.actuator.v2+json");
            await context.Response.WriteAsync(serialInfo).ConfigureAwait(false);
        }
    }
}
