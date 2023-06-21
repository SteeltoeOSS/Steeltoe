// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.Middleware;
using Steeltoe.Management.Endpoint.Options;

namespace Steeltoe.Management.Endpoint.Metrics;

internal sealed class MetricsEndpointMiddleware : EndpointMiddleware<MetricsRequest, IMetricsResponse>
{
    private readonly ILogger<MetricsEndpointMiddleware> _logger;

    public MetricsEndpointMiddleware(IMetricsEndpointHandler endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILoggerFactory loggerFactory)
        : base(endpointHandler, managementOptions, loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MetricsEndpointMiddleware>();
    }

    private MetricsRequest GetMetricsRequest(HttpContext context)
    {
        HttpRequest request = context.Request;
        _logger.LogDebug("Handling metrics for path: {path}", request.Path.Value);

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
        ManagementEndpointOptions mgmtOptions = ManagementEndpointOptionsMonitor.GetFromContextPath(request.Path, out _);

        if (mgmtOptions == null)
        {
            return GetMetricName(request, EndpointHandler.Options.Path);
        }

        string path = $"{mgmtOptions.Path}/{EndpointHandler.Options.Id}".Replace("//", "/", StringComparison.Ordinal);
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

    protected override async Task<IMetricsResponse> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        MetricsRequest metricsRequest = GetMetricsRequest(context);
        return await EndpointHandler.InvokeAsync(metricsRequest, cancellationToken);
    }

    protected override async Task WriteResponseAsync(IMetricsResponse result, HttpContext context, CancellationToken cancellationToken)
    {
        MetricsRequest metricsRequest = GetMetricsRequest(context);

        if (metricsRequest != null && result is null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }

        await base.WriteResponseAsync(result, context, cancellationToken);
    }
}
