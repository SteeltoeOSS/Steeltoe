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
using Steeltoe.Management.MetricCollectors;
using Steeltoe.Management.MetricCollectors.Metrics;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

public class AspNetCoreHostingObserver : MetricsObserver
{
    private const string DefaultObserverName = "AspNetCoreHostingObserver";
    private const string DiagnosticName = "Microsoft.AspNetCore";
    private const string StatusTagKey = "status";
    private const string ExceptionTagKey = "exception";
    private const string MethodTagKey = "method";
    private const string UriTagKey = "uri";
    internal const string StopEvent = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

    private readonly Histogram<double> _responseTime;
    private readonly Histogram<double> _serverCount;

    public AspNetCoreHostingObserver(IOptionsMonitor<MetricsObserverOptions> optionsMonitor, ILogger<AspNetCoreHostingObserver> logger)
        : base(DefaultObserverName, DiagnosticName, logger)
    {
        SetPathMatcher(new Regex(optionsMonitor.CurrentValue.IngressIgnorePattern));
        Meter meter = SteeltoeMetrics.Meter;

        _responseTime = meter.CreateHistogram<double>("http.server.requests.seconds", "s", "measures the duration of the inbound request in seconds");
        _serverCount = meter.CreateHistogram<double>("http.server.requests.count", "total", "number of requests");
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
            Logger.LogTrace("HandleStopEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

            var context = DiagnosticHelpers.GetProperty<HttpContext>(value, "HttpContext");

            if (context != null)
            {
                HandleStopEvent(current, context);
            }

            Logger.LogTrace("HandleStopEvent finish {thread}", Thread.CurrentThread.ManagedThreadId);
        }
    }

    protected internal void HandleStopEvent(Activity current, HttpContext arg)
    {
        if (ShouldIgnoreRequest(arg.Request.Path))
        {
            Logger.LogDebug("HandleStopEvent: Ignoring path: {path}", arg.Request.Path);
            return;
        }

        if (current.Duration.TotalMilliseconds > 0)
        {
            IEnumerable<KeyValuePair<string, object>> labelSets = GetLabelSets(arg);

            _serverCount.Record(1, labelSets.AsReadonlySpan());
            _responseTime.Record(current.Duration.TotalSeconds, labelSets.AsReadonlySpan());
        }
    }

    protected internal IEnumerable<KeyValuePair<string, object>> GetLabelSets(HttpContext arg)
    {
        string uri = arg.Request.Path.ToString();
        string statusCode = arg.Response.StatusCode.ToString(CultureInfo.InvariantCulture);
        string exception = GetException(arg);

        return new Dictionary<string, object>
        {
            { UriTagKey, uri },
            { StatusTagKey, statusCode },
            { ExceptionTagKey, exception },
            { MethodTagKey, arg.Request.Method }
        };
    }

    protected internal string GetException(HttpContext arg)
    {
        var exception = arg.Features.Get<IExceptionHandlerFeature>();

        if (exception != null && exception.Error != null)
        {
            return exception.Error.GetType().Name;
        }

        return "None";
    }
}
