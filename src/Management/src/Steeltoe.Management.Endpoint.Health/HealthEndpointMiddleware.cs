//
// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.AspNetCore.Http;
using Steeltoe.Management.Endpoint.Middleware;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Health
{

    public class HealthEndpointMiddleware : EndpointMiddleware<Health>
    {
        private RequestDelegate _next;

        public HealthEndpointMiddleware(RequestDelegate next, HealthEndpoint endpoint, ILogger<HealthEndpointMiddleware> logger = null)
            : base(endpoint, logger)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (IsHealthRequest(context))
            {
                await HandleHealthRequestAsync(context);
            }
            else
            {
                await _next(context);
            }
        }

        internal protected async Task HandleHealthRequestAsync(HttpContext context)
        {
   
            var serialInfo = base.HandleRequest();
            logger?.LogDebug("Returning: {0}", serialInfo);
            context.Response.Headers.Add("Content-Type", "application/vnd.spring-boot.actuator.v1+json");
            await context.Response.WriteAsync(serialInfo);
    
        }

        internal protected bool IsHealthRequest(HttpContext context)
        {
            if (!context.Request.Method.Equals("GET")) { return false; }
            PathString path = new PathString(endpoint.Path);
            return context.Request.Path.Equals(path);
        }

    }
}
