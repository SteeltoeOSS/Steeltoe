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
using Steeltoe.Management.Endpoint.Middleware;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class PrometheusScraperEndpointHandler : EndpointHandler<string>
    {
        private readonly RequestDelegate _next;

        public PrometheusScraperEndpointHandler(RequestDelegate next, PrometheusScraperEndpoint endpoint, IManagementOptions mgmtOptions, ILogger<PrometheusScraperEndpointMiddleware> logger = null)
            : base(endpoint, mgmtOptions, logger)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            if (_endpoint.ShouldInvoke(_mgmtOptions))
            {
                await HandleMetricsRequestAsync(context).ConfigureAwait(false);
            }
        }

        public override string HandleRequest()
        {
            var result = _endpoint.Invoke();
            return result;
        }

        protected internal async Task HandleMetricsRequestAsync(HttpContext context)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            _logger?.LogDebug("Incoming path: {0}", request.Path.Value);

            // GET /metrics/{metricName}?tag=key:value&tag=key:value
            var serialInfo = HandleRequest();

            if (serialInfo != null)
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "text/plain; version=0.0.4;";
                await context.Response.WriteAsync(serialInfo).ConfigureAwait(false);
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
    }
}
