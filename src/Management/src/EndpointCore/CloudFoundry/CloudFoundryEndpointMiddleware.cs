// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Hypermedia;
using Steeltoe.Management.Endpoint.Middleware;
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

        public CloudFoundryEndpointMiddleware(RequestDelegate next, CloudFoundryEndpoint endpoint, IManagementOptions mgmtOptions, ILogger<CloudFoundryEndpointMiddleware> logger = null)
            : base(endpoint, mgmtOptions, logger: logger)
        {
            _next = next;
            _options = endpoint.Options as ICloudFoundryOptions;
        }

        public Task Invoke(HttpContext context)
        {
            _logger?.LogDebug("Invoke({0} {1})", context.Request.Method, context.Request.Path.Value);

            if (_endpoint.ShouldInvoke(_mgmtOptions, _logger))
            {
                return HandleCloudFoundryRequestAsync(context);
            }

            return Task.CompletedTask;
        }

        protected internal Task HandleCloudFoundryRequestAsync(HttpContext context)
        {
            var serialInfo = HandleRequest(GetRequestUri(context.Request));
            _logger?.LogDebug("Returning: {0}", serialInfo);
            context.HandleContentNegotiation(_logger);
            return context.Response.WriteAsync(serialInfo);
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
