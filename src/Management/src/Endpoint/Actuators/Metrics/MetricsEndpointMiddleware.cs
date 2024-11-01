// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.Configuration;
using Steeltoe.Management.Endpoint.Middleware;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics;

internal sealed class MetricsEndpointMiddleware(
    IMetricsEndpointHandler endpointHandler, IOptionsMonitor<ManagementOptions> managementOptionsMonitor, ILoggerFactory loggerFactory)
    : EndpointMiddleware<MetricsRequest?, MetricsResponse?>(endpointHandler, managementOptionsMonitor, loggerFactory)
{
    private readonly ILogger<MetricsEndpointMiddleware> _logger = loggerFactory.CreateLogger<MetricsEndpointMiddleware>();

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
        string path = $"{baseRequestPath}/{EndpointHandler.Options.Path}".Replace("//", "/", StringComparison.Ordinal);

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
        if (query.Count == 0 || !query.Keys.Any(key => key.Equals("tag", StringComparison.OrdinalIgnoreCase)))
        {
            return Array.Empty<KeyValuePair<string, string>>();
        }

        Dictionary<string, HashSet<string>> tagValuesByName = new();

        foreach (KeyValuePair<string, StringValues> parameter in query.Where(parameter => parameter.Key.Equals("tag", StringComparison.OrdinalIgnoreCase)))
        {
            foreach (string? parameterValue in parameter.Value)
            {
                KeyValuePair<string, string>? tag = ParseTag(parameterValue);

                if (tag != null)
                {
                    if (!tagValuesByName.TryGetValue(tag.Value.Key, out HashSet<string>? tagValues))
                    {
                        tagValues = [];
                        tagValuesByName[tag.Value.Key] = tagValues;
                    }

                    tagValues.Add(tag.Value.Value);
                }
            }
        }

        List<KeyValuePair<string, string>> result = [];

        foreach ((string tagName, HashSet<string> tagValues) in tagValuesByName)
        {
            foreach (string tagValue in tagValues)
            {
                result.Add(new KeyValuePair<string, string>(tagName, tagValue));
            }
        }

        return result;
    }

    internal KeyValuePair<string, string>? ParseTag(string? tag)
    {
        if (tag != null)
        {
            string[] segments = tag.Split(':', 2);

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
