// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Diagnostics;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

internal sealed class HttpExchangesDiagnosticObserver : DiagnosticObserver, IHttpExchangesRepository
{
    private const string DiagnosticName = "Microsoft.AspNetCore";
    private const string DefaultObserverName = "HttpExchangesDiagnosticObserver";
    private const string StopEventName = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

    internal const string Redacted = "******";

    private readonly IOptionsMonitor<HttpExchangesEndpointOptions> _optionsMonitor;
    internal ConcurrentQueue<HttpExchange> Queue { get; } = new();

    public HttpExchangesDiagnosticObserver(IOptionsMonitor<HttpExchangesEndpointOptions> optionsMonitor, ILoggerFactory loggerFactory)
        : base(DefaultObserverName, DiagnosticName, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
    }

    public HttpExchangesResult GetHttpExchanges()
    {
        List<HttpExchange> recentExchanges = [];
        HttpExchangesEndpointOptions options = _optionsMonitor.CurrentValue;

        recentExchanges.AddRange(
            from exchange in Queue
            let requestUri = FilterUri(exchange.Request.Uri, options)
            let filteredRequestHeaders = options.IncludeRequestHeaders ? FilterHeaders(exchange.Request.Headers, options.RequestHeaders) : []
            let filteredResponseHeaders = options.IncludeResponseHeaders ? FilterHeaders(exchange.Response.Headers, options.ResponseHeaders) : []
            let principal = options.IncludeUserPrincipal ? exchange.Principal : null
            let remoteAddress = options.IncludeRemoteAddress ? exchange.Request.RemoteAddress : null
            let session = options.IncludeSessionId ? exchange.Session : null
            let request = new HttpExchangeRequest(exchange.Request.Method, requestUri, filteredRequestHeaders, remoteAddress)
            let response = new HttpExchangeResponse(exchange.Response.Status, filteredResponseHeaders)
            select new HttpExchange(request, response, exchange.Timestamp, principal, session, exchange.TimeTaken));

        recentExchanges.Sort((first, second) => options.Reverse ? second.Timestamp.CompareTo(first.Timestamp) : first.Timestamp.CompareTo(second.Timestamp));

        return new HttpExchangesResult(recentExchanges);
    }

    public override void ProcessEvent(string eventName, object? value)
    {
        if (eventName != StopEventName || value == null)
        {
            return;
        }

        Activity? current = Activity.Current;

        if (current == null)
        {
            return;
        }

        if (value is HttpContext httpContext)
        {
            RecordHttpExchange(current, httpContext);
        }
    }

    internal static HttpContext? GetHttpContextPropertyValue(object instance)
    {
        return GetPropertyOrDefault<HttpContext>(instance, "HttpContext");
    }

    private void RecordHttpExchange(Activity current, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(context);

        HttpExchange exchange = GetHttpExchange(context, current.Duration);
        Queue.Enqueue(exchange);

        if (Queue.Count > _optionsMonitor.CurrentValue.Capacity)
        {
            Queue.TryDequeue(out _);
        }
    }

    private static Uri FilterUri(Uri requestUri, HttpExchangesEndpointOptions options)
    {
        var filteredUri = new UriBuilder(requestUri);

        if (!options.IncludePathInfo)
        {
            filteredUri.Path = string.Empty;
        }

        if (!options.IncludeQueryString)
        {
            filteredUri.Query = string.Empty;
        }

        return filteredUri.Uri;
    }

    private HttpExchange GetHttpExchange(HttpContext context, TimeSpan duration)
    {
        HttpExchangesEndpointOptions options = _optionsMonitor.CurrentValue;

        var requestUri = new Uri(context.Request.GetEncodedUrl());
        Dictionary<string, StringValues> requestHeaders = GetHeaders(context.Request.Headers);

        var request = new HttpExchangeRequest(context.Request.Method, requestUri, requestHeaders, context.Connection.RemoteIpAddress?.ToString());

        Dictionary<string, StringValues> responseHeaders = GetHeaders(context.Response.Headers);

        var response = new HttpExchangeResponse(context.Response.StatusCode, responseHeaders);

        HttpExchangePrincipal? principal = GetUserPrincipal(context);

        string? sessionId = null;

        if (options.IncludeSessionId)
        {
            sessionId = GetSessionId(context);
        }

        HttpExchangeSession? session = sessionId == null ? null : new HttpExchangeSession(sessionId);

        return new HttpExchange(request, response, DateTime.UtcNow, principal, session, duration);
    }

    private static Dictionary<string, StringValues> GetHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

        foreach ((string key, StringValues value) in headers)
        {
            result.Add(key, value);
        }

        return result;
    }

    private static Dictionary<string, StringValues> FilterHeaders(IDictionary<string, StringValues> headers, HashSet<string> allowedHeaders)
    {
        var filteredHeaders = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

        foreach ((string key, StringValues value) in headers)
        {
            filteredHeaders[key] = HeaderShouldBeRedacted(key, allowedHeaders) ? Redacted : value;
        }

        return filteredHeaders;
    }

    private static bool HeaderShouldBeRedacted(string currentHeader, HashSet<string> allowedHeaders)
    {
        return !allowedHeaders.Contains(currentHeader);
    }

    internal static HttpExchangePrincipal? GetUserPrincipal(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? username = context.User.Identity?.Name;

        return username == null ? null : new HttpExchangePrincipal(username);
    }

    internal static string? GetSessionId(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sessionFeature = context.Features.Get<ISessionFeature>();

        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        return sessionFeature?.Session?.Id;
    }
}
