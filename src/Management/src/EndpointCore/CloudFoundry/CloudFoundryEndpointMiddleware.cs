// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
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
#pragma warning disable CS0618 // Type or member is obsolete
    public class CloudFoundryEndpointMiddleware : EndpointMiddleware<Links, string>
#pragma warning restore CS0618 // Type or member is obsolete
    {
        private readonly ICloudFoundryOptions _options;
        private readonly RequestDelegate _next;

        public CloudFoundryEndpointMiddleware(RequestDelegate next, CloudFoundryEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<CloudFoundryEndpointMiddleware> logger = null)
            : base(endpoint, mgmtOptions?.OfType<CloudFoundryManagementOptions>(), logger: logger)
        {
            _next = next;
            _options = endpoint.Options as ICloudFoundryOptions;
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public CloudFoundryEndpointMiddleware(RequestDelegate next, CloudFoundryEndpoint endpoint, ILogger<CloudFoundryEndpointMiddleware> logger = null)
            : base(endpoint, logger: logger)
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
            string scheme = request.Scheme;

            if (request.Headers.TryGetValue("X-Forwarded-Proto", out StringValues headerScheme))
            {
                scheme = headerScheme.ToString();
            }

            return $"{scheme}://{request.Host}{request.PathBase}{request.Path}";
        }
    }
}
