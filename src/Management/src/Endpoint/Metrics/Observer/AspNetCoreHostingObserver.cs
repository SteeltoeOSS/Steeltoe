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
using Steeltoe.Common;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.MetricCollectors.Metrics;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

internal sealed class AspNetCoreHostingObserver : MetricsObserver
{
    private const string DefaultObserverName = "AspNetCoreHostingObserver";
    private const string DiagnosticName = "Microsoft.AspNetCore";
    private const string StatusTagKey = "status";
    private const string ExceptionTagKey = "exception";
    private const string MethodTagKey = "method";
    private const string UriTagKey = "uri";
    private const string StopEvent = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

    private readonly Histogram<double> _responseTime;
    private readonly Histogram<double> _serverCount;
    private readonly ILogger _logger;

    public AspNetCoreHostingObserver(IOptionsMonitor<MetricsObserverOptions> optionsMonitor, ILoggerFactory loggerFactory)
        : base(DefaultObserverName, DiagnosticName, loggerFactory)
    {
        ArgumentGuard.NotNull(optionsMonitor);
        SetPathMatcher(new Regex(optionsMonitor.CurrentValue.IngressIgnorePattern));
        Meter meter = SteeltoeMetrics.Meter;

        _responseTime = meter.CreateHistogram<double>("http.server.requests.seconds", "s", "measures the duration of the inbound request in seconds");
        _serverCount = meter.CreateHistogram<double>("http.server.requests.count", "total", "number of requests");
        _logger = loggerFactory.CreateLogger<AspNetCoreHostingObserver>();
    }

    public override void ProcessEvent(string eventName, object value)
    {
        if (value == null)
        {
            return;
        }

        Activity current = Activity.Current;

        if (current == null)
        {
            return;
        }

        if (eventName == StopEvent)
        {
            _logger.LogTrace("HandleStopEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

            var context = DiagnosticHelpers.GetPropertyOrDefault<HttpContext>(value, "HttpContext");

            if (context != null)
            {
                HandleStopEvent(current, context);
            }

            _logger.LogTrace("HandleStopEvent finish {thread}", Thread.CurrentThread.ManagedThreadId);
        }
    }

    private void HandleStopEvent(Activity current, HttpContext context)
    {
        ArgumentGuard.NotNull(current);
        ArgumentGuard.NotNull(context);

        if (ShouldIgnoreRequest(context.Request.Path))
        {
            _logger.LogDebug("HandleStopEvent: Ignoring path: {path}", context.Request.Path);
            return;
        }

        if (current.Duration.TotalMilliseconds > 0)
        {
            IEnumerable<KeyValuePair<string, object>> labelSets = GetLabelSets(context);

            _serverCount.Record(1, labelSets.AsReadonlySpan());
            _responseTime.Record(current.Duration.TotalSeconds, labelSets.AsReadonlySpan());
        }
    }

    internal IEnumerable<KeyValuePair<string, object>> GetLabelSets(HttpContext context)
    {
        ArgumentGuard.NotNull(context);
        string uri = context.Request.Path.ToString();
        string statusCode = context.Response.StatusCode.ToString(CultureInfo.InvariantCulture);
        string exception = GetException(context);

        return new Dictionary<string, object>
        {
            { UriTagKey, uri },
            { StatusTagKey, statusCode },
            { ExceptionTagKey, exception },
            { MethodTagKey, context.Request.Method }
        };
    }

    internal string GetException(HttpContext arg)
    {
        var exception = arg.Features.Get<IExceptionHandlerFeature>();

        if (exception != null)
        {
            return exception.Error.GetType().Name;
        }

        return "None";
    }
}
