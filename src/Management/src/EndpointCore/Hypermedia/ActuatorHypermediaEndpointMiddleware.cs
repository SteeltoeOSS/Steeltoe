// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.EndpointCore.ContentNegotiation;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Hypermedia
{
    public class ActuatorHypermediaEndpointMiddleware : EndpointMiddleware<Links, string>
    {
        private readonly RequestDelegate _next;

        public ActuatorHypermediaEndpointMiddleware(RequestDelegate next, ActuatorEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<ActuatorHypermediaEndpointMiddleware> logger = null)
            : base(endpoint, mgmtOptions.OfType<ActuatorManagementOptions>(), logger: logger)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            _logger?.LogDebug("Invoke({0} {1})", context.Request.Method, context.Request.Path.Value);

            if (RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                return HandleCloudFoundryRequestAsync(context);
            }

            return _next(context);
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
            string scheme = request.Scheme;

            if (request.Headers.TryGetValue("X-Forwarded-Proto", out StringValues headerScheme))
            {
                scheme = headerScheme.ToString();
            }

            return $"{scheme}://{request.Host}{request.PathBase}{request.Path}";
        }
    }
}
