// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Security;
using Steeltoe.Management.EndpointCore;
using Steeltoe.Management.EndpointCore.ContentNegotiation;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Health
{
    public class HealthEndpointMiddleware : EndpointMiddleware<HealthCheckResult, ISecurityContext>
    {
        private readonly RequestDelegate _next;

        public HealthEndpointMiddleware(RequestDelegate next, IEnumerable<IManagementOptions> mgmtOptions, ILogger<InfoEndpointMiddleware> logger = null)
            : base(mgmtOptions: mgmtOptions, logger: logger)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context, HealthEndpointCore endpoint)
        {
            _endpoint = endpoint;

            if (RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                return HandleHealthRequestAsync(context);
            }

            return _next(context);
        }

        protected internal Task HandleHealthRequestAsync(HttpContext context)
        {
            var serialInfo = DoRequest(context);
            _logger?.LogDebug("Returning: {0}", serialInfo);

            context.HandleContentNegotiation(_logger);
            return context.Response.WriteAsync(serialInfo);
        }

        protected internal string DoRequest(HttpContext context)
        {
            var result = _endpoint.Invoke(new CoreSecurityContext(context));
            context.Response.StatusCode = ((HealthEndpoint)_endpoint).GetStatusCode(result);
            return Serialize(result);
        }
    }
}
