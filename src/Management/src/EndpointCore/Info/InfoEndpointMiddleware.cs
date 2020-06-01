// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.EndpointCore.ContentNegotiation;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Info
{
    public class InfoEndpointMiddleware : EndpointMiddleware<Dictionary<string, object>>
    {
        private readonly RequestDelegate _next;

        public InfoEndpointMiddleware(RequestDelegate next, InfoEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<InfoEndpointMiddleware> logger = null)
            : base(endpoint, mgmtOptions, logger: logger)
        {
            _next = next;
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public InfoEndpointMiddleware(RequestDelegate next, InfoEndpoint endpoint, ILogger<InfoEndpointMiddleware> logger = null)
            : base(endpoint, logger: logger)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogDebug("Info middleware Invoke({0})", context.Request.Path.Value);

            if (RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                await HandleInfoRequestAsync(context).ConfigureAwait(false);
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
        }

        protected internal async Task HandleInfoRequestAsync(HttpContext context)
        {
            var serialInfo = HandleRequest();
            _logger?.LogDebug("Returning: {0}", serialInfo);

            context.HandleContentNegotiation(_logger);
            await context.Response.WriteAsync(serialInfo).ConfigureAwait(false);
        }
    }
}
