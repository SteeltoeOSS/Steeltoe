// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

public class AspNetCoreHostingObserver : MetricsObserver
{
    internal const string StopEvent = "Microsoft.AspNetCore.Hosting.HttpRequestIn.Stop";

    private const string DefaultObserverName = "AspNetCoreHostingObserver";
    private const string DiagnosticName = "Microsoft.AspNetCore";

    private readonly string _statusTagKey = "status";
    private readonly string _exceptionTagKey = "exception";
    private readonly string _methodTagKey = "method";
    private readonly string _uriTagKey = "uri";
    private readonly Histogram<double> _responseTime;
    private readonly Histogram<double> _serverCount;
    private readonly IViewRegistry _viewRegistry;

    public AspNetCoreHostingObserver(IMetricsObserverOptions options, IViewRegistry viewRegistry, ILogger<AspNetCoreHostingObserver> logger)
        : base(DefaultObserverName, DiagnosticName, options, logger)
    {
        SetPathMatcher(new Regex(options.IngressIgnorePattern));
        var meter = OpenTelemetryMetrics.Meter;

        _viewRegistry = viewRegistry ?? throw new ArgumentNullException(nameof(viewRegistry));
        _responseTime = meter.CreateHistogram<double>("http.server.requests.seconds", "s", "measures the duration of the inbound request in seconds");
        _serverCount = meter.CreateHistogram<double>("http.server.requests.count", "total", "number of requests");

        /*
        //var view = View.Create(
        //        ViewName.Create("http.server.request.time"),
        //        "Total request time",
        //        responseTimeMeasure,
        //        Distribution.Create(BucketBoundaries.Create(new List<double>() { 0.0, 1.0, 5.0, 10.0, 100.0 })),
        //        new List<ITagKey>() { statusTagKey, exceptionTagKey, methodTagKey, uriTagKey });

        //ViewManager.RegisterView(view);

        //view = View.Create(
        //        ViewName.Create("http.server.request.count"),
        //        "Total request counts",
        //        serverCountMeasure,
        //        Sum.Create(),
        //        new List<ITagKey>() { statusTagKey, exceptionTagKey, methodTagKey, uriTagKey });

        //ViewManager.RegisterView(view);
        */
    }

    public override void ProcessEvent(string eventName, object value)
    {
        if (value == null)
        {
            return;
        }

        var current = Activity.Current;
        if (current == null)
        {
            return;
        }

        if (eventName == StopEvent)
        {
            Logger?.LogTrace("HandleStopEvent start{thread}", Thread.CurrentThread.ManagedThreadId);

            var context = DiagnosticHelpers.GetProperty<HttpContext>(value, "HttpContext");

            if (context != null)
            {
                HandleStopEvent(current, context);
            }

            Logger?.LogTrace("HandleStopEvent finish {thread}", Thread.CurrentThread.ManagedThreadId);
        }
    }

    protected internal void HandleStopEvent(Activity current, HttpContext arg)
    {
        if (ShouldIgnoreRequest(arg.Request.Path))
        {
            Logger?.LogDebug("HandleStopEvent: Ignoring path: {path}", arg.Request.Path);
            return;
        }

        if (current.Duration.TotalMilliseconds > 0)
        {
            var labelSets = GetLabelSets(arg);

            _serverCount.Record(1, labelSets.AsReadonlySpan());
            _responseTime.Record(current.Duration.TotalSeconds, labelSets.AsReadonlySpan());
        }
    }

    protected internal IEnumerable<KeyValuePair<string, object>> GetLabelSets(HttpContext arg)
    {
        var uri = arg.Request.Path.ToString();
        var statusCode = arg.Response.StatusCode.ToString();
        var exception = GetException(arg);

        return new Dictionary<string, object>
        {
            { _uriTagKey, uri },
            { _statusTagKey, statusCode },
            { _exceptionTagKey, exception },
            { _methodTagKey, arg.Request.Method }
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
