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
using System.Text.RegularExpressions;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

public class HttpClientCoreObserver : MetricsObserver
{
    internal const string DiagnosticName = "HttpHandlerDiagnosticListener";
    internal const string DefaultObserverName = "HttpClientCoreObserver";

    internal const string StopEvent = "System.Net.Http.HttpRequestOut.Stop";
    internal const string ExceptionEvent = "System.Net.Http.Exception";

    private readonly string _statusTagKey = "status";
    private readonly string _uriTagKey = "uri";
    private readonly string _methodTagKey = "method";
    private readonly string _clientTagKey = "clientName";
    private readonly Histogram<double> _clientTimeMeasure;
    private readonly Histogram<double> _clientCountMeasure;

    public HttpClientCoreObserver(IMetricsObserverOptions options, ILogger<HttpClientCoreObserver> logger, IViewRegistry viewRegistry)
        : base(DefaultObserverName, DiagnosticName, options, logger)
    {
        if (viewRegistry == null)
        {
            throw new ArgumentNullException(nameof(viewRegistry));
        }

        SetPathMatcher(new Regex(options.EgressIgnorePattern));
        _clientTimeMeasure = OpenTelemetryMetrics.Meter.CreateHistogram<double>("http.client.request.time");
        _clientCountMeasure = OpenTelemetryMetrics.Meter.CreateHistogram<double>("http.client.request.count");

        viewRegistry.AddView(
            "http.client.request.time",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new[] { 0.0, 1.0, 5.0, 10.0, 100.0 },
                TagKeys = new[] { _statusTagKey, _uriTagKey, _methodTagKey, _clientTagKey },
            });
        viewRegistry.AddView(
            "http.client.request.count",
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

        var request = DiagnosticHelpers.GetProperty<HttpRequestMessage>(value, "Request");
        if (request == null)
        {
            return;
        }

        if (eventName == StopEvent)
        {
            Logger?.LogTrace("HandleStopEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

            var response = DiagnosticHelpers.GetProperty<HttpResponseMessage>(value, "Response");
            var requestStatus = DiagnosticHelpers.GetProperty<TaskStatus>(value, "RequestTaskStatus");
            HandleStopEvent(current, request, response, requestStatus);

            Logger?.LogTrace("HandleStopEvent finished {thread}", Thread.CurrentThread.ManagedThreadId);
        }
        else if (eventName == ExceptionEvent)
        {
            Logger?.LogTrace("HandleExceptionEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

            HandleExceptionEvent(current, request);

            Logger?.LogTrace("HandleExceptionEvent finished {thread}", Thread.CurrentThread.ManagedThreadId);
        }
    }

    protected internal void HandleExceptionEvent(Activity current, HttpRequestMessage request)
    {
        HandleStopEvent(current, request, null, TaskStatus.Faulted);
    }

    protected internal void HandleStopEvent(Activity current, HttpRequestMessage request, HttpResponseMessage response, TaskStatus taskStatus)
    {
        if (ShouldIgnoreRequest(request.RequestUri.AbsolutePath))
        {
            Logger?.LogDebug("HandleStopEvent: Ignoring path: {path}", SecurityUtilities.SanitizeInput(request.RequestUri.AbsolutePath));
            return;
        }

        if (current.Duration.TotalMilliseconds > 0)
        {
            var labels = GetLabels(request, response, taskStatus);
            _clientTimeMeasure.Record(current.Duration.TotalMilliseconds, labels.AsReadonlySpan());
            _clientCountMeasure.Record(1, labels.AsReadonlySpan());
        }
    }

    protected internal IEnumerable<KeyValuePair<string, object>> GetLabels(HttpRequestMessage request, HttpResponseMessage response, TaskStatus taskStatus)
    {
        var uri = request.RequestUri.GetComponents(UriComponents.PathAndQuery, UriFormat.SafeUnescaped);
        var statusCode = GetStatusCode(response, taskStatus);
        var clientName = request.RequestUri.GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped);

        return new Dictionary<string, object>
        {
            { _uriTagKey, uri },
            { _statusTagKey, statusCode },
            { _clientTagKey, clientName },
            { _methodTagKey, request.Method.ToString() }
        };
    }

    protected internal string GetStatusCode(HttpResponseMessage response, TaskStatus taskStatus)
    {
        if (response != null)
        {
            var val = (int)response.StatusCode;
            return val.ToString();
        }

        if (taskStatus == TaskStatus.Faulted)
        {
            return "CLIENT_FAULT";
        }

        if (taskStatus == TaskStatus.Canceled)
        {
            return "CLIENT_CANCELED";
        }

        return "CLIENT_ERROR";
    }
}
