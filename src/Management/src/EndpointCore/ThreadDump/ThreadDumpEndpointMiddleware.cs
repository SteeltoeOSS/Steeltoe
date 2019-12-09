﻿// Copyright 2017 the original author or authors.
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
using Steeltoe.Management.EndpointCore.ContentNegotiation;
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
