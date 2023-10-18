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
    private const string StopEventName = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

    private static readonly DateTime BaseTime = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private readonly IOptionsMonitor<TraceEndpointOptions> _optionsMonitor;
    private readonly ILogger<TraceDiagnosticObserver> _logger;
    internal ConcurrentQueue<TraceResult> Queue { get; } = new();

    public TraceDiagnosticObserver(IOptionsMonitor<TraceEndpointOptions> optionsMonitor, ILoggerFactory loggerFactory)
        : base(DefaultObserverName, DiagnosticName, loggerFactory)
    {
        ArgumentGuard.NotNull(optionsMonitor);

        _optionsMonitor = optionsMonitor;
        _logger = loggerFactory.CreateLogger<TraceDiagnosticObserver>();
    }

    public virtual HttpTraceResult GetTraces()
    {
        return new HttpTraceResultV1(Queue.ToList());
    }

    public override void ProcessEvent(string eventName, object? value)
    {
        if (eventName != StopEventName)
        {
            return;
        }

        Activity? current = Activity.Current;

        if (current == null)
        {
            return;
        }

        if (value == null)
        {
            return;
        }

        HttpContext? context = GetHttpContextPropertyValue(value);

        if (context != null)
        {
            RecordHttpTrace(current, context);
        }
    }

    protected virtual void RecordHttpTrace(Activity current, HttpContext context)
    {
        ArgumentGuard.NotNull(current);
        ArgumentGuard.NotNull(context);

        TraceResult trace = MakeTrace(context, current.Duration);
        Queue.Enqueue(trace);

        if (Queue.Count > _optionsMonitor.CurrentValue.Capacity && !Queue.TryDequeue(out _))
        {
            _logger.LogDebug("Stop - Dequeue failed");
        }
    }

    internal TraceResult MakeTrace(HttpContext context, TimeSpan duration)
    {
        ArgumentGuard.NotNull(context);

        HttpRequest request = context.Request;
        HttpResponse response = context.Response;
        TraceEndpointOptions options = _optionsMonitor.CurrentValue;
        string? pathInfo = GetPathInfo(request);

        var details = new Dictionary<string, object?>
        {
            { "method", request.Method },
            { "path", pathInfo }
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
            details.Add("pathInfo", pathInfo);
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
            details.Add("authType", string.Empty);
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

        long timestamp = GetJavaTime(DateTime.Now.Ticks);
        return new TraceResult(timestamp, details);
    }

    internal long GetJavaTime(long ticks)
    {
        long javaTicks = ticks - BaseTime.Ticks;
        return javaTicks / 10000;
    }

    internal string? GetSessionId(HttpContext context)
    {
        ArgumentGuard.NotNull(context);

        var sessionFeature = context.Features.Get<ISessionFeature>();
        return sessionFeature?.Session.Id;
    }

    internal string GetTimeTaken(TimeSpan duration)
    {
        long timeInMilliseconds = (long)duration.TotalMilliseconds;
        return timeInMilliseconds.ToString(CultureInfo.InvariantCulture);
    }

    internal Dictionary<string, IList<string?>> GetRequestParameters(HttpRequest request)
    {
        ArgumentGuard.NotNull(request);

        var parameters = new Dictionary<string, IList<string?>>();
        IQueryCollection query = request.Query;

        foreach (KeyValuePair<string, StringValues> pair in query)
        {
            parameters.Add(pair.Key, pair.Value.ToArray());
        }

        if (request.HasFormContentType)
        {
            IFormCollection formData = request.Form;

            foreach (KeyValuePair<string, StringValues> pair in formData)
            {
                parameters.Add(pair.Key, pair.Value.ToArray());
            }
        }

        return parameters;
    }

    internal string GetRequestUri(HttpRequest request)
    {
        ArgumentGuard.NotNull(request);

        return $"{request.Scheme}://{request.Host.Value}{request.Path.Value}";
    }

    internal string? GetPathInfo(HttpRequest request)
    {
        ArgumentGuard.NotNull(request);

        return request.Path.Value;
    }

    internal string? GetUserPrincipal(HttpContext context)
    {
        ArgumentGuard.NotNull(context);

        return context.User.Identity?.Name;
    }

    internal string? GetRemoteAddress(HttpContext context)
    {
        ArgumentGuard.NotNull(context);

        return context.Connection.RemoteIpAddress?.ToString();
    }

    internal Dictionary<string, object?> GetHeaders(int status, IHeaderDictionary headers)
    {
        ArgumentGuard.NotNull(headers);

        Dictionary<string, object?> result = GetHeaders(headers);
        result.Add("status", status.ToString(CultureInfo.InvariantCulture));
        return result;
    }

    private Dictionary<string, object?> GetHeaders(IHeaderDictionary headers)
    {
        var result = new Dictionary<string, object?>();

        foreach (KeyValuePair<string, StringValues> pair in headers)
        {
            // Add filtering
#pragma warning disable S4040 // Strings should be normalized to uppercase
            result.Add(pair.Key.ToLowerInvariant(), GetHeaderValue(pair.Value));
#pragma warning restore S4040 // Strings should be normalized to uppercase
        }

        return result;
    }

    private object? GetHeaderValue(StringValues values)
    {
        var result = new List<string?>();

        foreach (string? value in values)
        {
            result.Add(value);
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

    internal HttpContext? GetHttpContextPropertyValue(object instance)
    {
        return GetPropertyOrDefault<HttpContext>(instance, "HttpContext");
    }
}
