// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.ThreadDump
{
    public class ThreadDumpEndpointMiddleware_v2 : EndpointMiddleware<ThreadDumpResult>
    {
        private readonly RequestDelegate _next;

        public ThreadDumpEndpointMiddleware_v2(RequestDelegate next, ThreadDumpEndpoint_v2 endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<ThreadDumpEndpointMiddleware_v2> logger = null)
           : base(endpoint, mgmtOptions, logger: logger)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            if (RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                return HandleThreadDumpRequestAsync(context);
            }

            return _next(context);
        }

        protected internal Task HandleThreadDumpRequestAsync(HttpContext context)
        {
            var serialInfo = HandleRequest();
            _logger?.LogDebug("Returning: {0}", serialInfo);
            context.Response.Headers.Add("Content-Type", "application/vnd.spring-boot.actuator.v2+json");
            return context.Response.WriteAsync(serialInfo);
        }
    }
}
