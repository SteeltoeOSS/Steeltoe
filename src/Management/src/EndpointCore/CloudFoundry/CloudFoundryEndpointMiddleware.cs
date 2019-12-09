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
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.EndpointCore.ContentNegotiation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.CloudFoundry
{
    /// <summary>
    /// CloudFoundry endpoint provides hypermedia: a page is added with links to all the endpoints that are enabled.
    /// When deployed to CloudFoundry this endpoint is used for apps manager integration when <see cref="CloudFoundrySecurityMiddleware"/> is added.
    /// </summary>
    public class CloudFoundryEndpointMiddleware : EndpointMiddleware<Links, string>
    {
        private readonly ICloudFoundryOptions _options;
        private readonly RequestDelegate _next;

        public CloudFoundryEndpointMiddleware(RequestDelegate next, CloudFoundryEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<CloudFoundryEndpointMiddleware> logger = null)
            : base(endpoint, mgmtOptions?.OfType<CloudFoundryManagementOptions>(), logger: logger)
        {
            _next = next;
            _options = endpoint.Options as ICloudFoundryOptions;
        }

        public async Task Invoke(HttpContext context)
        {
            _logger?.LogDebug("Invoke({0} {1})", context.Request.Method, context.Request.Path.Value);

            if (RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                await HandleCloudFoundryRequestAsync(context).ConfigureAwait(false);
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
        }

        protected internal async Task HandleCloudFoundryRequestAsync(HttpContext context)
        {
            var serialInfo = HandleRequest(GetRequestUri(context.Request));
            _logger?.LogDebug("Returning: {0}", serialInfo);

            context.HandleContentNegotiation(_logger);
            await context.Response.WriteAsync(serialInfo).ConfigureAwait(false);
        }

        protected internal string GetRequestUri(HttpRequest request)
        {
            var scheme = request.Scheme;

            if (request.Headers.TryGetValue("X-Forwarded-Proto", out StringValues headerScheme))
            {
                scheme = headerScheme.ToString();
            }

            return $"{scheme}://{request.Host}{request.PathBase}{request.Path}";
        }
    }
}
