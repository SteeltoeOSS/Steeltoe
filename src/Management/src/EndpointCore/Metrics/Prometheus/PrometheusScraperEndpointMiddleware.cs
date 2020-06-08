// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Metrics
{
    public class PrometheusScraperEndpointMiddleware : EndpointMiddleware<string>
    {
        private readonly RequestDelegate _next;

        public PrometheusScraperEndpointMiddleware(RequestDelegate next, PrometheusScraperEndpoint endpoint, IEnumerable<IManagementOptions> mgmtOptions, ILogger<PrometheusScraperEndpointMiddleware> logger = null)
            : base(endpoint, mgmtOptions, null, false, logger)
        {
            _next = next;
        }

        public Task Invoke(HttpContext context)
        {
            if (RequestVerbAndPathMatch(context.Request.Method, context.Request.Path.Value))
            {
                return HandleMetricsRequestAsync(context);
            }

            return _next(context);
        }

        public override string HandleRequest()
        {
            var result = _endpoint.Invoke();
            return result;
        }

        protected internal Task HandleMetricsRequestAsync(HttpContext context)
        {
            HttpRequest request = context.Request;
            HttpResponse response = context.Response;

            _logger?.LogDebug("Incoming path: {0}", request.Path.Value);

            // GET /metrics/{metricName}?tag=key:value&tag=key:value
            var serialInfo = HandleRequest();

            if (serialInfo == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                return Task.CompletedTask;
            }

            response.StatusCode = (int)HttpStatusCode.OK;
            response.ContentType = "text/plain; version=0.0.4;";
            return context.Response.WriteAsync(serialInfo);
        }
    }
}
