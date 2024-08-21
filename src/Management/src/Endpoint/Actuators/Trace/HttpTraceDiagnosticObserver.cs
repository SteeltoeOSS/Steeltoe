// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Steeltoe.Management.Endpoint.Actuators.Trace;

internal sealed class HttpTraceDiagnosticObserver : TraceDiagnosticObserver
{
    private readonly IOptionsMonitor<TraceEndpointOptions> _optionsMonitor;
    private readonly ILogger<HttpTraceDiagnosticObserver> _logger;
    private readonly ConcurrentQueue<HttpTrace> _queue = new();

    public HttpTraceDiagnosticObserver(IOptionsMonitor<TraceEndpointOptions> optionsMonitor, ILoggerFactory loggerFactory)
        : base(optionsMonitor, loggerFactory)
    {
        _optionsMonitor = optionsMonitor;
        _logger = loggerFactory.CreateLogger<HttpTraceDiagnosticObserver>();
    }

    public override HttpTraceResult GetTraces()
    {
        return new HttpTraceResultV2(_queue.ToList());
    }

    protected override void RecordHttpTrace(Activity current, HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(context);

        HttpTrace trace = MakeTraceV2(context, current.Duration);
        _queue.Enqueue(trace);

        if (_queue.Count > _optionsMonitor.CurrentValue.Capacity && !_queue.TryDequeue(out _))
        {
            _logger.LogDebug("Stop - Dequeue failed");
        }
    }

    private HttpTrace MakeTraceV2(HttpContext context, TimeSpan duration)
    {
        string? remoteAddress = GetRemoteAddress(context);
        string requestUri = GetRequestUri(context.Request);
        Dictionary<string, IList<string?>> requestHeaders = GetHeaders(context.Request.Headers);

        var request = new Request(context.Request.Method, requestUri, requestHeaders, remoteAddress);

        Dictionary<string, IList<string?>> responseHeaders = GetHeaders(context.Response.Headers);
        var response = new Response(context.Response.StatusCode, responseHeaders);

        string? userName = GetUserPrincipal(context);
        Principal? principal = userName == null ? null : new Principal(userName);

        string? sessionId = GetSessionId(context);
        Session? session = sessionId == null ? null : new Session(sessionId);

        long timestamp = GetJavaTime(DateTime.UtcNow.Ticks);

        return new HttpTrace(request, response, timestamp, principal, session, duration.Milliseconds);
    }

    private Dictionary<string, IList<string?>> GetHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, IList<string?>>();

        foreach (KeyValuePair<string, StringValues> pair in headers)
        {
            // Add filtering
#pragma warning disable S4040 // Strings should be normalized to uppercase
            result.Add(pair.Key.ToLowerInvariant(), pair.Value.ToArray());
#pragma warning restore S4040 // Strings should be normalized to uppercase
        }

        return result;
    }
}
