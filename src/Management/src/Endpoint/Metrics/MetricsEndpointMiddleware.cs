// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.ContentNegotiation;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Metrics;

internal class MetricsEndpointMiddleware : EndpointMiddleware<IMetricsResponse, MetricsRequest>
{
    public MetricsEndpointMiddleware(IMetricsEndpoint endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILogger<MetricsEndpointMiddleware> logger)
        : base(endpoint, managementOptions, logger)
    {
    }

    public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (Endpoint.Options.ShouldInvoke(ManagementOptions, context, Logger))
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

    internal async Task HandleMetricsRequestAsync(HttpContext context)
    {
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        Logger.LogDebug("Incoming path: {path}", request.Path.Value);

        string metricName = GetMetricName(request);

        if (!string.IsNullOrEmpty(metricName))
        {
            // GET /metrics/{metricName}?tag=key:value&tag=key:value
            IList<KeyValuePair<string, string>> tags = ParseTags(request.Query);
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
            Logger.LogDebug("Returning: {info}", serialInfo);

            context.HandleContentNegotiation(Logger);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.WriteAsync(serialInfo);
        }
    }

    internal string GetMetricName(HttpRequest request)
    {
        ManagementEndpointOptions mgmtOptions = ManagementOptions.GetFromContextPath(request.Path);

        if (mgmtOptions == null)
        {
            return GetMetricName(request, Endpoint.Options.Path);
        }

        string path = $"{mgmtOptions.Path}/{Endpoint.Options.Id}".Replace("//", "/", StringComparison.Ordinal);
        string metricName = GetMetricName(request, path);

        return metricName;
    }

    protected internal IList<KeyValuePair<string, string>> ParseTags(IQueryCollection query)
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

    internal KeyValuePair<string, string>? ParseTag(string kvp)
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
