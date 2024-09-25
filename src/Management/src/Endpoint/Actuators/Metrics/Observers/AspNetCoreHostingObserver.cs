// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Diagnostics;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics.Observers;

internal sealed class AspNetCoreHostingObserver : MetricsObserver
{
    private const string DefaultObserverName = "AspNetCoreHostingObserver";
    private const string DiagnosticName = "Microsoft.AspNetCore";
    private const string StatusTagKey = "status";
    private const string ExceptionTagKey = "exception";
    private const string MethodTagKey = "method";
    private const string UriTagKey = "uri";
    private const string StopEventName = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

    private readonly Histogram<double> _responseTime;
    private readonly Histogram<double> _serverCount;
    private readonly ILogger _logger;

    public AspNetCoreHostingObserver(IOptionsMonitor<MetricsObserverOptions> optionsMonitor, ILoggerFactory loggerFactory)
        : base(DefaultObserverName, DiagnosticName, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        string? ingressIgnorePattern = optionsMonitor.CurrentValue.IngressIgnorePattern;

        if (ingressIgnorePattern != null)
        {
            SetPathMatcher(new Regex(ingressIgnorePattern, RegexOptions.None, TimeSpan.FromSeconds(1)));
        }

        Meter meter = SteeltoeMetrics.Meter;

        _responseTime = meter.CreateHistogram<double>("http.server.requests.seconds", "s", "measures the duration of the inbound request in seconds");
        _serverCount = meter.CreateHistogram<double>("http.server.requests.count", "total", "number of requests");
        _logger = loggerFactory.CreateLogger<AspNetCoreHostingObserver>();
    }

    public override void ProcessEvent(string eventName, object? value)
    {
        if (value == null || eventName != StopEventName)
        {
            return;
        }

        Activity? current = Activity.Current;

        if (current == null)
        {
            return;
        }

        _logger.LogTrace("HandleStopEvent start {Thread}", System.Environment.CurrentManagedThreadId);

        if (value is HttpContext httpContext)
        {
            HandleStopEvent(current, httpContext);
        }

        _logger.LogTrace("HandleStopEvent finish {Thread}", System.Environment.CurrentManagedThreadId);
    }

    private void HandleStopEvent(Activity current, HttpContext context)
    {
        if (ShouldIgnoreRequest(context.Request.Path))
        {
            _logger.LogDebug("HandleStopEvent: Ignoring path: {Path}", context.Request.Path);
            return;
        }

        if (current.Duration.TotalMilliseconds > 0)
        {
            ReadOnlySpan<KeyValuePair<string, object?>> labels = GetLabelSets(context).AsReadonlySpan();
            _serverCount.Record(1, labels);
            _responseTime.Record(current.Duration.TotalSeconds, labels);
        }
    }

    internal IDictionary<string, object?> GetLabelSets(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string uri = context.Request.Path.ToString();
        string statusCode = context.Response.StatusCode.ToString(CultureInfo.InvariantCulture);
        string exceptionTypeName = GetExceptionTypeName(context);

        return new Dictionary<string, object?>
        {
            { UriTagKey, uri },
            { StatusTagKey, statusCode },
            { ExceptionTagKey, exceptionTypeName },
            { MethodTagKey, context.Request.Method }
        };
    }

    internal static string GetExceptionTypeName(HttpContext context)
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>();

        return exception != null ? exception.Error.GetType().Name : "None";
    }
}
