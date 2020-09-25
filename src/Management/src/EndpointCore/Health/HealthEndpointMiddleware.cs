// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Info;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Security;
using System.Linq;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Health
{
    public class HealthEndpointMiddleware : EndpointMiddleware<HealthEndpointResponse, ISecurityContext>
    {
        private readonly RequestDelegate _next;

        public HealthEndpointMiddleware(RequestDelegate next, IManagementOptions mgmtOptions, ILogger<InfoEndpointMiddleware> logger = null)
            : base(mgmtOptions: mgmtOptions, logger: logger)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context, HealthEndpointCore endpoint)
        {
            _endpoint = endpoint;
            if (_endpoint.ShouldInvoke(_mgmtOptions))
            {
                return HandleHealthRequestAsync(context);
            }

            return Task.CompletedTask;
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
            if (((HealthEndpointOptions)_mgmtOptions.EndpointOptions.FirstOrDefault(o => o is HealthEndpointOptions)).HttpStatusFromHealth)
            {
                context.Response.StatusCode = ((HealthEndpoint)_endpoint).GetStatusCode(result);
            }

            return Serialize(result);
        }
    }
}
