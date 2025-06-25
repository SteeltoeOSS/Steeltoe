// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

internal sealed class HttpExchangesRepository
{
    private const string RedactedText = "******";

    private readonly IOptionsMonitor<HttpExchangesEndpointOptions> _optionsMonitor;
    private readonly ILogger<HttpExchangesRepository> _logger;
    private readonly ConcurrentQueue<HttpExchange> _queue = new();

    public HttpExchangesRepository(IHttpExchangeRecorder httpExchangeRecorder, IOptionsMonitor<HttpExchangesEndpointOptions> optionsMonitor,
        ILogger<HttpExchangesRepository> logger)
    {
        ArgumentNullException.ThrowIfNull(httpExchangeRecorder);
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        _optionsMonitor = optionsMonitor;
        _logger = logger;

        httpExchangeRecorder.HandleRecording(OnRecord);
    }

    private void OnRecord(HttpExchange exchange)
    {
        ArgumentNullException.ThrowIfNull(exchange);

        _logger.LogDebug("Incoming exchange for {Url}.", exchange.Request.Uri);
        _queue.Enqueue(exchange);

        if (_queue.Count > _optionsMonitor.CurrentValue.Capacity)
        {
            _queue.TryDequeue(out _);
        }
    }

    public HttpExchangesResult GetHttpExchanges()
    {
        List<HttpExchange> recentExchanges = [];
        HttpExchangesEndpointOptions options = _optionsMonitor.CurrentValue;

        recentExchanges.AddRange(
            from exchange in _queue
            let requestUri = FilterUri(exchange.Request.Uri, options)
            let filteredRequestHeaders = options.IncludeRequestHeaders ? FilterHeaders(exchange.Request.Headers, options.RequestHeaders) : []
            let filteredResponseHeaders = options.IncludeResponseHeaders ? FilterHeaders(exchange.Response.Headers, options.ResponseHeaders) : []
            let principal = options.IncludeUserPrincipal ? exchange.Principal : null
            let remoteAddress = options.IncludeRemoteAddress ? exchange.Request.RemoteAddress : null
            let session = options.IncludeSessionId ? exchange.Session : null
            let request = new HttpExchangeRequest(exchange.Request.Method, requestUri, filteredRequestHeaders, remoteAddress)
            let response = new HttpExchangeResponse(exchange.Response.Status, filteredResponseHeaders)
            let timeTaken = FilterTimeTaken(exchange.TimeTaken, options)
            select new HttpExchange(request, response, exchange.Timestamp, principal, session, timeTaken));

        recentExchanges.Sort((first, second) => options.Reverse ? second.Timestamp.CompareTo(first.Timestamp) : first.Timestamp.CompareTo(second.Timestamp));

        return new HttpExchangesResult(recentExchanges);
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

    private static Dictionary<string, StringValues> FilterHeaders(IDictionary<string, StringValues> headers, HashSet<string> allowedHeaders)
    {
        var filteredHeaders = new Dictionary<string, StringValues>(StringComparer.OrdinalIgnoreCase);

        foreach ((string key, StringValues value) in headers)
        {
            filteredHeaders[key] = HeaderShouldBeRedacted(key, allowedHeaders) ? RedactedText : value;
        }

        return filteredHeaders;
    }

    private static bool HeaderShouldBeRedacted(string currentHeader, HashSet<string> allowedHeaders)
    {
        return !allowedHeaders.Contains(currentHeader);
    }

    private static TimeSpan? FilterTimeTaken(TimeSpan? timeTaken, HttpExchangesEndpointOptions options)
    {
        return !options.IncludeTimeTaken ? null : timeTaken;
    }
}
