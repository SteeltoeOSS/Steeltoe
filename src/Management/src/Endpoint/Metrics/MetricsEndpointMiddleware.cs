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

internal sealed class MetricsEndpointMiddleware : EndpointMiddleware<MetricsRequest, IMetricsResponse>
{
    public MetricsEndpointMiddleware(IMetricsEndpointHandler endpoint, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        IOptionsMonitor<HttpMiddlewareOptions> endpointOptions, ILogger<MetricsEndpointMiddleware> logger)
        : base(endpoint, managementOptions, endpointOptions, logger)
    {
    }

    //public override Task InvokeAsync(HttpContext context, RequestDelegate next)
    //{
    //    if (EndpointOptions.CurrentValue.ShouldInvoke(ManagementOptions, context, Logger))
    //    {
    //        return HandleMetricsRequestAsync(context);
    //    }

    //    return Task.CompletedTask;
    //}

    //public override async Task<string> HandleRequestAsync(MetricsRequest arg, CancellationToken cancellationToken)
    //{
    //    IMetricsResponse result = await EndpointHandler.InvokeAsync(arg, cancellationToken);
    //    return result == null ? null : Serialize(result);
    //}

    //internal async Task HandleMetricsRequestAsync(HttpContext context)
    //{
    //    HttpRequest request = context.Request;
    //    HttpResponse response = context.Response;

    //    Logger.LogDebug("Incoming path: {path}", request.Path.Value);

    //    string metricName = GetMetricName(request);

    //    if (!string.IsNullOrEmpty(metricName))
    //    {
    //        // GET /metrics/{metricName}?tag=key:value&tag=key:value
    //        IList<KeyValuePair<string, string>> tags = ParseTags(request.Query);
    //        var metricRequest = new MetricsRequest(metricName, tags);
    //        string serialInfo = await HandleRequestAsync(metricRequest, context.RequestAborted);

    //        if (serialInfo != null)
    //        {
    //            response.StatusCode = (int)HttpStatusCode.OK;
    //            await context.Response.WriteAsync(serialInfo);
    //        }
    //        else
    //        {
    //            response.StatusCode = (int)HttpStatusCode.NotFound;
    //        }
    //    }
    //    else
    //    {
    //        // GET /metrics
    //        string serialInfo = await HandleRequestAsync(null, context.RequestAborted);
    //        Logger.LogDebug("Returning: {info}", serialInfo);

    //        context.HandleContentNegotiation(Logger);
    //        context.Response.StatusCode = (int)HttpStatusCode.OK;
    //        await context.Response.WriteAsync(serialInfo);
    //    }
    //}
    private MetricsRequest GetMetricsRequest(HttpContext context)
    {
        HttpRequest request = context.Request;
        Logger.LogDebug("Handling metrics for path: {path}", request.Path.Value);

        string metricName = GetMetricName(request);

        if (!string.IsNullOrEmpty(metricName))
        {
            // GET /metrics/{metricName}?tag=key:value&tag=key:value
            IList<KeyValuePair<string, string>> tags = ParseTags(request.Query);
            return new MetricsRequest(metricName, tags);
        }

        // GET /metrics
        return null;
    }
    internal string GetMetricName(HttpRequest request)
    {
        ManagementEndpointOptions mgmtOptions = ManagementOptions.GetFromContextPath(request.Path);

        if (mgmtOptions == null)
        {
            return GetMetricName(request, EndpointOptions.CurrentValue.Path);
        }

        string path = $"{mgmtOptions.Path}/{EndpointOptions.CurrentValue.Id}".Replace("//", "/", StringComparison.Ordinal);
        string metricName = GetMetricName(request, path);

        return metricName;
    }

    internal IList<KeyValuePair<string, string>> ParseTags(IQueryCollection query)
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

    public override bool ShouldInvoke(HttpContext context)
    {
        throw new NotImplementedException();
    }

    protected override async Task<IMetricsResponse> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var metricsRequest = GetMetricsRequest(context);
        var response = await EndpointHandler.InvokeAsync(metricsRequest, cancellationToken);

        if (metricsRequest != null && response is MetricsEmptyResponse)
        {
            context.Response.StatusCode = (int) HttpStatusCode.NotFound;
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }

        return response;
        
    }
}
