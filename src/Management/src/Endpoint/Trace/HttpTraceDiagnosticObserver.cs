// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;
using Steeltoe.Management.Diagnostics;

namespace Steeltoe.Management.Endpoint.Trace;

public class HttpTraceDiagnosticObserver : DiagnosticObserver, IHttpTraceRepository
{
    private const string DiagnosticName = "Microsoft.AspNetCore";
    private const string DefaultObserverName = "HttpTraceDiagnosticObserver";
    private const string StopEvent = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

    private static readonly DateTime BaseTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly IOptionsMonitor<TraceEndpointOptions> _options;
    private readonly ILogger<TraceDiagnosticObserver> _logger;
    private readonly ConcurrentQueue<HttpTrace> _queue = new();

    public HttpTraceDiagnosticObserver(IOptionsMonitor<TraceEndpointOptions> options, ILogger<TraceDiagnosticObserver> logger)
        : base(DefaultObserverName, DiagnosticName, logger)
    {
        ArgumentGuard.NotNull(options);

        _options = options;
        _logger = logger;
    }

    public HttpTraceResult GetTraces()
    {
        return new HttpTraceResult(_queue.ToList());
    }

    public override void ProcessEvent(string eventName, object value)
    {
        if (eventName != StopEvent)
        {
            return;
        }

        Activity current = Activity.Current;

        if (current == null)
        {
            return;
        }

        if (value == null)
        {
            return;
        }

        HttpContext context = GetHttpContextPropertyValue(value);

        if (context != null)
        {
            HttpTrace trace = MakeTrace(context, current.Duration);
            _queue.Enqueue(trace);

            if (_queue.Count > _options.CurrentValue.Capacity && !_queue.TryDequeue(out _))
            {
                _logger.LogDebug("Stop - Dequeue failed");
            }
        }
    }

    private HttpTrace MakeTrace(HttpContext context, TimeSpan duration)
    {
        HttpRequest req = context.Request;
        HttpResponse res = context.Response;

        var request = new Request(req.Method, GetRequestUri(req), GetHeaders(req.Headers), GetRemoteAddress(context));
        var response = new Response(res.StatusCode, GetHeaders(res.Headers));
        var principal = new Principal(GetUserPrincipal(context));
        var session = new Session(GetSessionId(context));
        return new HttpTrace(request, response, GetJavaTime(DateTime.Now.Ticks), principal, session, duration.Milliseconds);
    }

    private long GetJavaTime(long ticks)
    {
        long javaTicks = ticks - BaseTime.Ticks;
        return javaTicks / 10000;
    }

    private string GetSessionId(HttpContext context)
    {
        var sessionFeature = context.Features.Get<ISessionFeature>();
        return sessionFeature == null ? null : context.Session.Id;
    }

    protected internal string GetTimeTaken(TimeSpan duration)
    {
        long timeInMilliseconds = (long)duration.TotalMilliseconds;
        return timeInMilliseconds.ToString(CultureInfo.InvariantCulture);
    }

    private string GetRequestUri(HttpRequest request)
    {
        return $"{request.Scheme}://{request.Host.Value}{request.Path.Value}";
    }

    private string GetUserPrincipal(HttpContext context)
    {
        return context?.User?.Identity?.Name;
    }

    private string GetRemoteAddress(HttpContext context)
    {
        return context?.Connection?.RemoteIpAddress?.ToString();
    }

    private Dictionary<string, string[]> GetHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, string[]>();

        foreach (KeyValuePair<string, StringValues> h in headers)
        {
            // Add filtering
#pragma warning disable S4040 // Strings should be normalized to uppercase
            result.Add(h.Key.ToLowerInvariant(), h.Value.ToArray());
#pragma warning restore S4040 // Strings should be normalized to uppercase
        }

        return result;
    }

    private HttpContext GetHttpContextPropertyValue(object obj)
    {
        return DiagnosticHelpers.GetProperty<HttpContext>(obj, "HttpContext");
    }
}
