// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using Steeltoe.Common;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Metrics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

public class HttpClientCoreObserver : MetricsObserver
{
    internal const string DIAGNOSTIC_NAME = "HttpHandlerDiagnosticListener";
    internal const string OBSERVER_NAME = "HttpClientCoreObserver";

    internal const string STOP_EVENT = "System.Net.Http.HttpRequestOut.Stop";
    internal const string EXCEPTION_EVENT = "System.Net.Http.Exception";

    private readonly string _statusTagKey = "status";
    private readonly string _uriTagKey = "uri";
    private readonly string _methodTagKey = "method";
    private readonly string _clientTagKey = "clientName";
    private Histogram<double> _clientTimeMeasure;
    private Histogram<double> _clientCountMeasure;
    private IViewRegistry _viewRegistry;

    public HttpClientCoreObserver(IMetricsObserverOptions options, ILogger<HttpClientCoreObserver> logger, IViewRegistry viewRegistry)
        : base(OBSERVER_NAME, DIAGNOSTIC_NAME, options, logger)
    {
        _viewRegistry = viewRegistry ?? throw new ArgumentNullException(nameof(viewRegistry));
        SetPathMatcher(new Regex(options.EgressIgnorePattern));
        _clientTimeMeasure = OpenTelemetryMetrics.Meter.CreateHistogram<double>("http.client.request.time");
        _clientCountMeasure = OpenTelemetryMetrics.Meter.CreateHistogram<double>("http.client.request.count");

        _viewRegistry.AddView(
            "http.client.request.time",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new[] { 0.0, 1.0, 5.0, 10.0, 100.0 },
                TagKeys = new[] { _statusTagKey, _uriTagKey, _methodTagKey, _clientTagKey },
            });
        _viewRegistry.AddView(
            "http.client.request.count",
            new ExplicitBucketHistogramConfiguration
            {
                Boundaries = new[] { 0.0, 1.0, 5.0, 10.0, 100.0 },
                TagKeys = new[] { _statusTagKey, _uriTagKey, _methodTagKey, _clientTagKey },
            });
    }

    public override void ProcessEvent(string evnt, object arg)
    {
        if (arg == null)
        {
            return;
        }

        var current = Activity.Current;
        if (current == null)
        {
            return;
        }

        var request = DiagnosticHelpers.GetProperty<HttpRequestMessage>(arg, "Request");
        if (request == null)
        {
            return;
        }

        if (evnt == STOP_EVENT)
        {
            Logger?.LogTrace("HandleStopEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

            var response = DiagnosticHelpers.GetProperty<HttpResponseMessage>(arg, "Response");
            var requestStatus = DiagnosticHelpers.GetProperty<TaskStatus>(arg, "RequestTaskStatus");
            HandleStopEvent(current, request, response, requestStatus);

            Logger?.LogTrace("HandleStopEvent finished {thread}", Thread.CurrentThread.ManagedThreadId);
        }
        else if (evnt == EXCEPTION_EVENT)
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
