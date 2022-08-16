// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Steeltoe.Common;
using Steeltoe.Common.Diagnostics;

namespace Steeltoe.Management.Endpoint.Trace;

public class TraceDiagnosticObserver : DiagnosticObserver, ITraceRepository
{
    private const string DiagnosticName = "Microsoft.AspNetCore";
    private const string DefaultObserverName = "TraceDiagnosticObserver";
    private const string StopEvent = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

    private static readonly DateTime BaseTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly ILogger<TraceDiagnosticObserver> _logger;
    private readonly ITraceOptions _options;
    internal ConcurrentQueue<TraceResult> Queue = new();

    public TraceDiagnosticObserver(ITraceOptions options, ILogger<TraceDiagnosticObserver> logger = null)
        : base(DefaultObserverName, DiagnosticName, logger)
    {
        ArgumentGuard.NotNull(options);

        _options = options;
        _logger = logger;
    }

    public IReadOnlyList<TraceResult> GetTraces()
    {
        TraceResult[] traces = Queue.ToArray();
        return new List<TraceResult>(traces);
    }

    public override void ProcessEvent(string eventName, object value)
    {
        if (!StopEvent.Equals(eventName))
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

        GetProperty(value, out HttpContext context);

        if (context != null)
        {
            TraceResult trace = MakeTrace(context, current.Duration);
            Queue.Enqueue(trace);

            if (Queue.Count > _options.Capacity && !Queue.TryDequeue(out _))
            {
                _logger?.LogDebug("Stop - Dequeue failed");
            }
        }
    }

    protected internal TraceResult MakeTrace(HttpContext context, TimeSpan duration)
    {
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;

        var details = new Dictionary<string, object>
        {
            { "method", request.Method },
            { "path", GetPathInfo(request) }
        };

        var headers = new Dictionary<string, object>();
        details.Add("headers", headers);

        if (_options.AddRequestHeaders)
        {
            headers.Add("request", GetHeaders(request.Headers));
        }

        if (_options.AddResponseHeaders)
        {
            headers.Add("response", GetHeaders(response.StatusCode, response.Headers));
        }

        if (_options.AddPathInfo)
        {
            details.Add("pathInfo", GetPathInfo(request));
        }

        if (_options.AddUserPrincipal)
        {
            details.Add("userPrincipal", GetUserPrincipal(context));
        }

        if (_options.AddParameters)
        {
            details.Add("parameters", GetRequestParameters(request));
        }

        if (_options.AddQueryString)
        {
            details.Add("query", request.QueryString.Value);
        }

        if (_options.AddAuthType)
        {
            details.Add("authType", GetAuthType(request)); // TODO
        }

        if (_options.AddRemoteAddress)
        {
            details.Add("remoteAddress", GetRemoteAddress(context));
        }

        if (_options.AddSessionId)
        {
            details.Add("sessionId", GetSessionId(context));
        }

        if (_options.AddTimeTaken)
        {
            details.Add("timeTaken", GetTimeTaken(duration));
        }

        return new TraceResult(GetJavaTime(DateTime.Now.Ticks), details);
    }

    protected internal long GetJavaTime(long ticks)
    {
        long javaTicks = ticks - BaseTime.Ticks;
        return javaTicks / 10000;
    }

    protected internal string GetSessionId(HttpContext context)
    {
        var sessionFeature = context.Features.Get<ISessionFeature>();
        return sessionFeature == null ? null : context.Session.Id;
    }

    protected internal string GetTimeTaken(TimeSpan duration)
    {
        long timeInMilliseconds = (long)duration.TotalMilliseconds;
        return timeInMilliseconds.ToString();
    }

    protected internal string GetAuthType(HttpRequest request)
    {
        return string.Empty;
    }

    protected internal Dictionary<string, string[]> GetRequestParameters(HttpRequest request)
    {
        var parameters = new Dictionary<string, string[]>();
        IQueryCollection query = request.Query;

        foreach (KeyValuePair<string, StringValues> p in query)
        {
            parameters.Add(p.Key, p.Value.ToArray());
        }

        if (request.HasFormContentType && request.Form != null)
        {
            IFormCollection formData = request.Form;

            foreach (KeyValuePair<string, StringValues> p in formData)
            {
                parameters.Add(p.Key, p.Value.ToArray());
            }
        }

        return parameters;
    }

    protected internal string GetRequestUri(HttpRequest request)
    {
        return $"{request.Scheme}://{request.Host.Value}{request.Path.Value}";
    }

    protected internal string GetPathInfo(HttpRequest request)
    {
        return request.Path.Value;
    }

    protected internal string GetUserPrincipal(HttpContext context)
    {
        return context?.User?.Identity?.Name;
    }

    protected internal string GetRemoteAddress(HttpContext context)
    {
        return context?.Connection?.RemoteIpAddress?.ToString();
    }

    protected internal Dictionary<string, object> GetHeaders(int status, IHeaderDictionary headers)
    {
        Dictionary<string, object> result = GetHeaders(headers);
        result.Add("status", status.ToString());
        return result;
    }

    protected internal Dictionary<string, object> GetHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, object>();

        foreach (KeyValuePair<string, StringValues> h in headers)
        {
            // Add filtering
            result.Add(h.Key.ToLowerInvariant(), GetHeaderValue(h.Value));
        }

        return result;
    }

    protected internal object GetHeaderValue(StringValues values)
    {
        var result = new List<string>();

        foreach (string v in values)
        {
            result.Add(v);
        }

        if (result.Count == 1)
        {
            return result[0];
        }

        if (result.Count == 0)
        {
            return string.Empty;
        }

        return result;
    }

    protected internal void GetProperty(object obj, out HttpContext context)
    {
        context = DiagnosticHelpers.GetProperty<HttpContext>(obj, "HttpContext");
    }
}
