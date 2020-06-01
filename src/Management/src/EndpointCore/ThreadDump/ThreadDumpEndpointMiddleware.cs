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

namespace Steeltoe.Management.Endpoint.ThreadDump
{
    public class ThreadDumpEndpointMiddleware : EndpointMiddleware<List<ThreadInfo>>
    {
        private readonly RequestDelegate _next;

        public ThreadDumpEndpointMiddleware(RequestDelegate next, ThreadDumpEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<ThreadDumpEndpointMiddleware> logger = null)
           : base(endpoint, mgmtOptions, logger: logger)
        {
            _next = next;
        }

        [Obsolete("Use newer constructor that passes in IManagementOptions instead")]
        public ThreadDumpEndpointMiddleware(RequestDelegate next, ThreadDumpEndpoint endpoint, ILogger<ThreadDumpEndpointMiddleware> logger = null)
            : base(endpoint, logger: logger)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                await HandleThreadDumpRequestAsync(context).ConfigureAwait(false);
            }
            else
            {
                await _next(context).ConfigureAwait(false);
            }
        }

        protected internal async Task HandleThreadDumpRequestAsync(HttpContext context)
        {
            var serialInfo = HandleRequest();
            _logger?.LogDebug("Returning: {0}", serialInfo);

            context.HandleContentNegotiation(_logger);
            await context.Response.WriteAsync(serialInfo).ConfigureAwait(false);
        }
    }
}
