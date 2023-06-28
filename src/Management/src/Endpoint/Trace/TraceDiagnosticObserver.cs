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

internal class TraceDiagnosticObserver : DiagnosticObserver, IHttpTraceRepository
{
    private const string DiagnosticName = "Microsoft.AspNetCore";
    private const string DefaultObserverName = "TraceDiagnosticObserver";
    private const string StopEvent = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

    private static readonly DateTime BaseTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly IOptionsMonitor<TraceEndpointOptions> _options;
    private readonly ILogger<TraceDiagnosticObserver> _logger;
    internal ConcurrentQueue<TraceResult> Queue { get; } = new();

    public TraceDiagnosticObserver(IOptionsMonitor<TraceEndpointOptions> options, ILoggerFactory loggerFactory)
        : base(DefaultObserverName, DiagnosticName, loggerFactory)
    {
        ArgumentGuard.NotNull(options);

        _options = options;
        _logger = loggerFactory.CreateLogger<TraceDiagnosticObserver>();
    }

    public virtual HttpTraceResult GetTraces()
    {
        return new HttpTracesV1(Queue.ToList());
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
            RecordHttpTrace(current, context);
        }
    }

    protected virtual void RecordHttpTrace(Activity current, HttpContext context)
    {
        TraceResult trace = MakeTrace(context, current.Duration);
        Queue.Enqueue(trace);

        if (Queue.Count > _options.CurrentValue.Capacity && !Queue.TryDequeue(out _))
        {
            _logger.LogDebug("Stop - Dequeue failed");
        }
    }

    internal TraceResult MakeTrace(HttpContext context, TimeSpan duration)
    {
        ArgumentGuard.NotNull(context);
        HttpRequest request = context.Request;
        HttpResponse response = context.Response;
        TraceEndpointOptions options = _options.CurrentValue;

        var details = new Dictionary<string, object>
        {
            { "method", request.Method },
            { "path", GetPathInfo(request) }
        };

        var headers = new Dictionary<string, object>();
        details.Add("headers", headers);

        if (options.AddRequestHeaders)
        {
            headers.Add("request", GetHeaders(request.Headers));
        }

        if (options.AddResponseHeaders)
        {
            headers.Add("response", GetHeaders(response.StatusCode, response.Headers));
        }

        if (options.AddPathInfo)
        {
            details.Add("pathInfo", GetPathInfo(request));
        }

        if (options.AddUserPrincipal)
        {
            details.Add("userPrincipal", GetUserPrincipal(context));
        }

        if (options.AddParameters)
        {
            details.Add("parameters", GetRequestParameters(request));
        }

        if (options.AddQueryString)
        {
            details.Add("query", request.QueryString.Value);
        }

        if (options.AddAuthType)
        {
            details.Add("authType", GetAuthType(request));
        }

        if (options.AddRemoteAddress)
        {
            details.Add("remoteAddress", GetRemoteAddress(context));
        }

        if (options.AddSessionId)
        {
            details.Add("sessionId", GetSessionId(context));
        }

        if (options.AddTimeTaken)
        {
            details.Add("timeTaken", GetTimeTaken(duration));
        }

        return new TraceResult(GetJavaTime(DateTime.Now.Ticks), details);
    }

    internal long GetJavaTime(long ticks)
    {
        long javaTicks = ticks - BaseTime.Ticks;
        return javaTicks / 10000;
    }

    internal string GetSessionId(HttpContext context)
    {
        var sessionFeature = context.Features.Get<ISessionFeature>();
        return sessionFeature == null ? null : context.Session.Id;
    }

    internal string GetTimeTaken(TimeSpan duration)
    {
        long timeInMilliseconds = (long)duration.TotalMilliseconds;
        return timeInMilliseconds.ToString(CultureInfo.InvariantCulture);
    }

    internal string GetAuthType(HttpRequest request)
    {
        return string.Empty;
    }

    internal Dictionary<string, string[]> GetRequestParameters(HttpRequest request)
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

    internal string GetRequestUri(HttpRequest request)
    {
        return $"{request.Scheme}://{request.Host.Value}{request.Path.Value}";
    }

    internal string GetPathInfo(HttpRequest request)
    {
        return request.Path.Value;
    }

    internal string GetUserPrincipal(HttpContext context)
    {
        return context?.User?.Identity?.Name;
    }

    internal string GetRemoteAddress(HttpContext context)
    {
        return context?.Connection?.RemoteIpAddress?.ToString();
    }

    internal Dictionary<string, object> GetHeaders(int status, IHeaderDictionary headers)
    {
        Dictionary<string, object> result = GetHeaders(headers);
        result.Add("status", status.ToString(CultureInfo.InvariantCulture));
        return result;
    }

    internal Dictionary<string, object> GetHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, object>();

        foreach (KeyValuePair<string, StringValues> h in headers)
        {
            // Add filtering
#pragma warning disable S4040 // Strings should be normalized to uppercase
            result.Add(h.Key.ToLowerInvariant(), GetHeaderValue(h.Value));
#pragma warning restore S4040 // Strings should be normalized to uppercase
        }

        return result;
    }

    internal object GetHeaderValue(StringValues values)
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

    internal HttpContext GetHttpContextPropertyValue(object obj)
    {
        return DiagnosticHelpers.GetProperty<HttpContext>(obj, "HttpContext");
    }

}
