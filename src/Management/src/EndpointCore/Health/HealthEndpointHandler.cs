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
    public class HealthEndpointHandler : EndpointHandler<HealthCheckResult, ISecurityContext>
    {
        private readonly RequestDelegate _next;

        public HealthEndpointHandler(RequestDelegate next, IManagementOptions mgmtOptions, ILogger<InfoEndpointMiddleware> logger = null)
            : base(mgmtOptions: mgmtOptions, logger: logger)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context, HealthEndpointCore endpoint)
        {
            _endpoint = endpoint;
            await HandleHealthRequestAsync(context).ConfigureAwait(false);
        }

        protected internal async Task HandleHealthRequestAsync(HttpContext context)
        {
            var serialInfo = DoRequest(context);
            _logger?.LogDebug("Returning: {0}", serialInfo);

            context.HandleContentNegotiation(_logger);
            await context.Response.WriteAsync(serialInfo).ConfigureAwait(false);
        }

        protected internal string DoRequest(HttpContext context)
        {
            var result = _endpoint.Invoke(new CoreSecurityContext(context));
            context.Response.StatusCode = ((HealthEndpoint)_endpoint).GetStatusCode(result);
            return Serialize(result);
        }
    }
}
