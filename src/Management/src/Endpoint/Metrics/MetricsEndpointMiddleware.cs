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
    public MetricsEndpointMiddleware(IMetricsEndpointHandler endpointHandler, IOptionsMonitor<ManagementEndpointOptions> managementOptions,
        ILogger<MetricsEndpointMiddleware> logger)
        : base(endpointHandler, managementOptions, logger)
    {
    }

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
        ManagementEndpointOptions mgmtOptions = ManagementEndpointOptions.GetFromContextPath(request.Path, out _);

        if (mgmtOptions == null)
        {
            return GetMetricName(request, EndpointOptions.Path);
        }

        string path = $"{mgmtOptions.Path}/{EndpointOptions.Id}".Replace("//", "/", StringComparison.Ordinal);
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
        IMetricsResponse response = await EndpointHandler.InvokeAsync(metricsRequest, cancellationToken);

        if (metricsRequest != null && response is MetricsEmptyResponse)
        {
            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }

        return response;
    }
}
