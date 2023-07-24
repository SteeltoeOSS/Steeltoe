// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;

namespace Steeltoe.Management.Endpoint.Trace;

internal sealed class HttpTraceDiagnosticObserver : TraceDiagnosticObserver
{
    private readonly IOptionsMonitor<TraceEndpointOptions> _options;
    private readonly ILogger<HttpTraceDiagnosticObserver> _logger;
    private readonly ConcurrentQueue<HttpTrace> _queue = new();

    public HttpTraceDiagnosticObserver(IOptionsMonitor<TraceEndpointOptions> options, ILoggerFactory loggerFactory)
        : base(options, loggerFactory)
    {
        ArgumentGuard.NotNull(options);

        _options = options;
        _logger = loggerFactory.CreateLogger<HttpTraceDiagnosticObserver>();
    }

    public override HttpTraceResult GetTraces()
    {
        return new HttpTraceResultV2(_queue.ToList());
    }

    protected override void RecordHttpTrace(Activity current, HttpContext context)
    {
        HttpTrace trace = MakeTraceV2(context, current.Duration);
        _queue.Enqueue(trace);

        if (_queue.Count > _options.CurrentValue.Capacity && !_queue.TryDequeue(out _))
        {
            _logger.LogDebug("Stop - Dequeue failed");
        }
    }

    private HttpTrace MakeTraceV2(HttpContext context, TimeSpan duration)
    {
        HttpRequest req = context.Request;
        HttpResponse res = context.Response;

        var request = new Request(req.Method, GetRequestUri(req), GetHeaders(req.Headers), GetRemoteAddress(context));
        var response = new Response(res.StatusCode, GetHeaders(res.Headers));
        var principal = new Principal(GetUserPrincipal(context));
        var session = new Session(GetSessionId(context));
        return new HttpTrace(request, response, GetJavaTime(DateTime.Now.Ticks), principal, session, duration.Milliseconds);
    }

    private Dictionary<string, IList<string>> GetHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, IList<string>>();

        foreach (KeyValuePair<string, StringValues> h in headers)
        {
            // Add filtering
#pragma warning disable S4040 // Strings should be normalized to uppercase
            result.Add(h.Key.ToLowerInvariant(), h.Value.ToArray());
#pragma warning restore S4040 // Strings should be normalized to uppercase
        }

        return result;
    }
}
