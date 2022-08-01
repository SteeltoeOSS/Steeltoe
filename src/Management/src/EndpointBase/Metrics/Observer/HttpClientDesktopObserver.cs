// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using Steeltoe.Common;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net;
using System.Text.RegularExpressions;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

public class HttpClientDesktopObserver : MetricsObserver
{
    internal const string DiagnosticName = "System.Net.Http.Desktop";
    internal const string DefaultObserverName = "HttpClientDesktopObserver";

    internal const string StopEvent = "System.Net.Http.Desktop.HttpRequestOut.Stop";
    internal const string StopExEvent = "System.Net.Http.Desktop.HttpRequestOut.Ex.Stop";

    private readonly string _statusTagKey = "status";
    private readonly string _uriTagKey = "uri";
    private readonly string _methodTagKey = "method";
    private readonly string _clientTagKey = "clientName";
    private readonly Histogram<double> _clientTimeMeasure;
    private readonly Histogram<double> _clientCountMeasure;

    public HttpClientDesktopObserver(IMetricsObserverOptions options, ILogger<HttpClientDesktopObserver> logger, IViewRegistry viewRegistry)
        : base(DefaultObserverName, DiagnosticName, options, logger)
    {
        if (viewRegistry == null)
        {
            throw new ArgumentNullException(nameof(viewRegistry));
        }

        SetPathMatcher(new Regex(options.EgressIgnorePattern));

        _clientTimeMeasure = OpenTelemetryMetrics.Meter.CreateHistogram<double>("http.desktop.client.request.time");
        _clientCountMeasure = OpenTelemetryMetrics.Meter.CreateHistogram<double>("http.desktop.client.request.count");

        viewRegistry.AddView(
            "http.desktop.client.request.time",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new[] { 0.0, 1.0, 5.0, 10.0, 100.0 },
                TagKeys = new[] { _statusTagKey, _uriTagKey, _methodTagKey, _clientTagKey },
            });
        viewRegistry.AddView(
            "http.desktop.client.request.count",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new[] { 0.0, 1.0, 5.0, 10.0, 100.0 },
                TagKeys = new[] { _statusTagKey, _uriTagKey, _methodTagKey, _clientTagKey },
            });
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

        var request = DiagnosticHelpers.GetProperty<HttpWebRequest>(value, "Request");
        if (request == null)
        {
            return;
        }

        if (eventName == StopEvent)
        {
            Logger?.LogTrace("HandleStopEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

            var response = DiagnosticHelpers.GetProperty<HttpWebResponse>(value, "Response");
            if (response != null)
            {
                HandleStopEvent(current, request, response.StatusCode);
            }

            Logger?.LogTrace("HandleStopEvent finished {thread}", Thread.CurrentThread.ManagedThreadId);
        }
        else if (eventName == StopExEvent)
        {
            Logger?.LogTrace("HandleStopEventEx start {thread}", Thread.CurrentThread.ManagedThreadId);

            var statusCode = DiagnosticHelpers.GetProperty<HttpStatusCode>(value, "StatusCode");

            HandleStopEvent(current, request, statusCode);

            Logger?.LogTrace("HandleStopEventEx finished {thread}", Thread.CurrentThread.ManagedThreadId);
        }
    }

    protected internal void HandleStopEvent(Activity current, HttpWebRequest request, HttpStatusCode statusCode)
    {
        if (ShouldIgnoreRequest(request.RequestUri.AbsolutePath))
        {
            Logger?.LogDebug("HandleStopEvent: Ignoring path: {path}", SecurityUtilities.SanitizeInput(request.RequestUri.AbsolutePath));
            return;
        }

        if (current.Duration.TotalMilliseconds > 0)
        {
            var labels = GetLabels(request, statusCode);
            _clientTimeMeasure.Record(current.Duration.TotalMilliseconds, labels.AsReadonlySpan());
            _clientCountMeasure.Record(1, labels.AsReadonlySpan());
        }
    }

    protected internal IEnumerable<KeyValuePair<string, object>> GetLabels(HttpWebRequest request, HttpStatusCode statusCode)
    {
        var uri = request.RequestUri.GetComponents(UriComponents.PathAndQuery, UriFormat.SafeUnescaped);
        var status = ((int)statusCode).ToString();
        var clientName = request.RequestUri.GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped);

        return new Dictionary<string, object>
        {
            { _uriTagKey, uri },
            { _statusTagKey, status },
            { _clientTagKey, clientName },
            { _methodTagKey, request.Method }
        };
    }
}
