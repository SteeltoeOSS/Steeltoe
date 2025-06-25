// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Steeltoe.Management.Endpoint.Actuators.HttpExchanges.Diagnostics;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges;

internal sealed class DiagnosticObserverHttpExchangeRecorder : DiagnosticObserver, IHttpExchangeRecorder
{
    private const string ObserverName = "HttpExchangesDiagnosticObserver";
    private const string ListenerName = "Microsoft.AspNetCore";
    private const string StopEventName = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

    private readonly TimeProvider _timeProvider;
    private volatile Action<HttpExchange>? _handler;

    public DiagnosticObserverHttpExchangeRecorder(TimeProvider timeProvider, ILoggerFactory loggerFactory)
        : base(ObserverName, ListenerName, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        _timeProvider = timeProvider;
    }

    public void HandleRecording(Action<HttpExchange> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _handler = handler;
    }

    public override void ProcessEvent(string eventName, object? value)
    {
        if (eventName != StopEventName || value == null || _handler == null)
        {
            return;
        }

        Activity? activity = Activity.Current;

        if (activity == null)
        {
            return;
        }

        if (value is HttpContext httpContext)
        {
            NotifyHttpExchange(activity, httpContext);
        }
    }

    private void NotifyHttpExchange(Activity activity, HttpContext context)
    {
        HttpExchange exchange = GetHttpExchange(context, activity.Duration);
        _handler?.Invoke(exchange);
    }

    private HttpExchange GetHttpExchange(HttpContext context, TimeSpan duration)
    {
        var requestUri = new Uri(context.Request.GetEncodedUrl());
        Dictionary<string, StringValues> requestHeaders = GetHeaders(context.Request.Headers);

        var request = new HttpExchangeRequest(context.Request.Method, requestUri, requestHeaders, context.Connection.RemoteIpAddress?.ToString());

        Dictionary<string, StringValues> responseHeaders = GetHeaders(context.Response.Headers);

        var response = new HttpExchangeResponse(context.Response.StatusCode, responseHeaders);

        HttpExchangePrincipal? principal = GetUserPrincipal(context);

        string? sessionId = GetSessionId(context);

        HttpExchangeSession? session = sessionId == null ? null : new HttpExchangeSession(sessionId);

        return new HttpExchange(request, response, _timeProvider.GetUtcNow().UtcDateTime, principal, session, duration);
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

    private static HttpExchangePrincipal? GetUserPrincipal(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? username = context.User.Identity?.Name;

        return username == null ? null : new HttpExchangePrincipal(username);
    }

    private static string? GetSessionId(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var sessionFeature = context.Features.Get<ISessionFeature>();

        // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
        return sessionFeature?.Session?.Id;
    }
}
