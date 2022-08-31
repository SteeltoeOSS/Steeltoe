// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Metrics;

public class MetricsEndpointMiddleware : EndpointMiddleware<IMetricsResponse, MetricsRequest>
{
    private readonly RequestDelegate _next;

    public MetricsEndpointMiddleware(RequestDelegate next, MetricsEndpoint endpoint, IManagementOptions managementOptions,
        ILogger<MetricsEndpointMiddleware> logger = null)
        : base(endpoint, managementOptions, logger)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (Endpoint.ShouldInvoke(managementOptions, logger))
        {
            return HandleMetricsRequestAsync(context);
        }

        return Task.CompletedTask;
    }

    public override string HandleRequest(MetricsRequest arg)
    {
        IMetricsResponse result = Endpoint.Invoke(arg);
        return result == null ? null : Serialize(result);
    }

    protected internal async Task HandleMetricsRequestAsync(HttpContext context)
    {
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        logger?.LogDebug("Incoming path: {path}", request.Path.Value);

        string metricName = GetMetricName(request);

        if (!string.IsNullOrEmpty(metricName))
        {
            // GET /metrics/{metricName}?tag=key:value&tag=key:value
            List<KeyValuePair<string, string>> tags = ParseTags(request.Query);
            var metricRequest = new MetricsRequest(metricName, tags);
            string serialInfo = HandleRequest(metricRequest);

            if (serialInfo != null)
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                await context.Response.WriteAsync(serialInfo);
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
        else
        {
            // GET /metrics
            string serialInfo = HandleRequest(null);
            logger?.LogDebug("Returning: {info}", serialInfo);

            context.HandleContentNegotiation(logger);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync(serialInfo);
        }
    }

    protected internal string GetMetricName(HttpRequest request)
    {
        if (managementOptions == null)
        {
            return GetMetricName(request, Endpoint.Path);
        }

        string path = $"{managementOptions.Path}/{Endpoint.Id}".Replace("//", "/");
        string metricName = GetMetricName(request, path);

        return metricName;
    }

    protected internal List<KeyValuePair<string, string>> ParseTags(IQueryCollection query)
    {
        var results = new List<KeyValuePair<string, string>>();

        if (query == null)
        {
            return results;
        }

        foreach (KeyValuePair<string, StringValues> q in query)
        {
            if (q.Key.Equals("tag", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string kvp in q.Value)
                {
                    KeyValuePair<string, string>? pair = ParseTag(kvp);

                    if (pair != null && !results.Contains(pair.Value))
                    {
                        results.Add(pair.Value);
                    }
                }
            }
        }

        return results;
    }

    protected internal KeyValuePair<string, string>? ParseTag(string kvp)
    {
        string[] str = kvp.Split(new[]
        {
            ':'
        }, 2);

        if (str != null && str.Length == 2)
        {
            return new KeyValuePair<string, string>(str[0], str[1]);
        }

        return null;
    }

    private string GetMetricName(HttpRequest request, string path)
    {
        var epPath = new PathString(path);

        if (request.Path.StartsWithSegments(epPath, out PathString remaining) && remaining.HasValue)
        {
            return remaining.Value.TrimStart('/');
        }

        return null;
    }
}
