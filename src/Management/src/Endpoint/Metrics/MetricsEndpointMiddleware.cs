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

internal sealed class MetricsEndpointMiddleware : EndpointMiddleware<MetricsRequest?, MetricsResponse?>
{
    private readonly ILogger<MetricsEndpointMiddleware> _logger;

    public MetricsEndpointMiddleware(IMetricsEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor,
        ILoggerFactory loggerFactory)
        : base(endpointHandler, managementOptionsMonitor, loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<MetricsEndpointMiddleware>();
    }

    protected override async Task<MetricsResponse?> InvokeEndpointHandlerAsync(HttpContext context, CancellationToken cancellationToken)
    {
        MetricsRequest? metricsRequest = GetMetricsRequest(context);
        return await EndpointHandler.InvokeAsync(metricsRequest, cancellationToken);
    }

    private MetricsRequest? GetMetricsRequest(HttpContext context)
    {
        HttpRequest request = context.Request;
        _logger.LogDebug("Handling metrics for path: {Path}", request.Path.Value);

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
        string? baseRequestPath = ManagementOptionsMonitor.CurrentValue.GetBaseRequestPath(request);
        string path = $"{baseRequestPath}/{EndpointHandler.Options.Id}".Replace("//", "/", StringComparison.Ordinal);

        return GetMetricName(request, path);
    }

    private string GetMetricName(HttpRequest request, string path)
    {
        if (request.Path.StartsWithSegments(path, out PathString remaining) && remaining.HasValue)
        {
            return remaining.Value!.TrimStart('/');
        }

        return string.Empty;
    }

    internal IList<KeyValuePair<string, string>> ParseTags(IQueryCollection query)
    {
        var results = new List<KeyValuePair<string, string>>();

        foreach (KeyValuePair<string, StringValues> parameter in query)
        {
            if (parameter.Key.Equals("tag", StringComparison.OrdinalIgnoreCase))
            {
                foreach (string? value in parameter.Value)
                {
                    KeyValuePair<string, string>? pair = ParseTag(value);

                    if (pair != null && !results.Contains(pair.Value))
                    {
                        results.Add(pair.Value);
                    }
                }
            }
        }

        return results;
    }

    internal KeyValuePair<string, string>? ParseTag(string? tag)
    {
        if (tag != null)
        {
            string[] segments = tag.Split(new[]
            {
                ':'
            }, 2);

            if (segments.Length == 2)
            {
                return new KeyValuePair<string, string>(segments[0], segments[1]);
            }
        }

        return null;
    }

    protected override async Task WriteResponseAsync(MetricsResponse? result, HttpContext context, CancellationToken cancellationToken)
    {
        MetricsRequest? metricsRequest = GetMetricsRequest(context);

        if (metricsRequest != null && result == null)
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
