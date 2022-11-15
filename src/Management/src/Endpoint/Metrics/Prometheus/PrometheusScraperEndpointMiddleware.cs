// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Metrics.Prometheus;

public class PrometheusScraperEndpointMiddleware : EndpointMiddleware<string>
{
    public PrometheusScraperEndpointMiddleware(RequestDelegate next, PrometheusScraperEndpoint endpoint, IManagementOptions managementOptions,
        ILogger<PrometheusScraperEndpointMiddleware> logger = null)
        : base(endpoint, managementOptions, logger)
    {
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (Endpoint.ShouldInvoke(managementOptions, logger))
        {
            return HandleMetricsRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    public override string HandleRequest()
    {
        string result = Endpoint.Invoke();
        return result;
    }

    protected internal Task HandleMetricsRequestAsync(HttpContext context)
    {
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        logger?.LogDebug("Incoming path: {path}", request.Path.Value);

        // GET /metrics/{metricName}?tag=key:value&tag=key:value
        string serialInfo = HandleRequest();

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
