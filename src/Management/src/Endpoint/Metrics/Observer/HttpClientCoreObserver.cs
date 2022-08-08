// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using Steeltoe.Common;
using Steeltoe.Common.Diagnostics;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Metrics;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

public class HttpClientCoreObserver : MetricsObserver
{
    private const string StatusTagKey = "status";
    private const string UriTagKey = "uri";
    private const string MethodTagKey = "method";
    private const string ClientTagKey = "clientName";
    internal const string DiagnosticName = "HttpHandlerDiagnosticListener";
    internal const string DefaultObserverName = "HttpClientCoreObserver";

    internal const string StopEvent = "System.Net.Http.HttpRequestOut.Stop";
    internal const string ExceptionEvent = "System.Net.Http.Exception";
    private readonly Histogram<double> _clientTimeMeasure;
    private readonly Histogram<double> _clientCountMeasure;

    public HttpClientCoreObserver(IMetricsObserverOptions options, ILogger<HttpClientCoreObserver> logger, IViewRegistry viewRegistry)
        : base(DefaultObserverName, DiagnosticName, options, logger)
    {
        ArgumentGuard.NotNull(viewRegistry);

        SetPathMatcher(new Regex(options.EgressIgnorePattern));
        _clientTimeMeasure = OpenTelemetryMetrics.Meter.CreateHistogram<double>("http.client.request.time");
        _clientCountMeasure = OpenTelemetryMetrics.Meter.CreateHistogram<double>("http.client.request.count");

        viewRegistry.AddView("http.client.request.time", new ExplicitBucketHistogramConfiguration
        {
            Boundaries = new[]
            {
                0.0,
                1.0,
                5.0,
                10.0,
                100.0
            },
            TagKeys = new[]
            {
                StatusTagKey,
                UriTagKey,
                MethodTagKey,
                ClientTagKey
            }
        });

        viewRegistry.AddView("http.client.request.count", new ExplicitBucketHistogramConfiguration
        {
            Boundaries = new[]
            {
                0.0,
                1.0,
                5.0,
                10.0,
                100.0
            },
            TagKeys = new[]
            {
                StatusTagKey,
                UriTagKey,
                MethodTagKey,
                ClientTagKey
            }
        });
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
            IEnumerable<KeyValuePair<string, object>> labels = GetLabels(request, response, taskStatus);
            _clientTimeMeasure.Record(current.Duration.TotalMilliseconds, labels.AsReadonlySpan());
            _clientCountMeasure.Record(1, labels.AsReadonlySpan());
        }
    }

    protected internal IEnumerable<KeyValuePair<string, object>> GetLabels(HttpRequestMessage request, HttpResponseMessage response, TaskStatus taskStatus)
    {
        string uri = request.RequestUri.GetComponents(UriComponents.PathAndQuery, UriFormat.SafeUnescaped);
        string statusCode = GetStatusCode(response, taskStatus);
        string clientName = request.RequestUri.GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped);

        return new Dictionary<string, object>
        {
            { UriTagKey, uri },
            { StatusTagKey, statusCode },
            { ClientTagKey, clientName },
            { MethodTagKey, request.Method.ToString() }
        };
    }

    protected internal string GetStatusCode(HttpResponseMessage response, TaskStatus taskStatus)
    {
        if (response != null)
        {
            int val = (int)response.StatusCode;
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
