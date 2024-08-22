// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Diagnostics;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics.Observers;

internal sealed class HttpClientCoreObserver : MetricsObserver
{
    private const string StatusTagKey = "status";
    private const string UriTagKey = "uri";
    private const string MethodTagKey = "method";
    private const string ClientTagKey = "clientName";
    private const string DiagnosticName = "HttpHandlerDiagnosticListener";
    private const string DefaultObserverName = "HttpClientCoreObserver";

    private const string StopEventName = "System.Net.Http.HttpRequestOut.Stop";
    private const string ExceptionEvent = "System.Net.Http.Exception";
    private readonly Histogram<double> _clientTimeMeasure;
    private readonly Histogram<double> _clientCountMeasure;
    private readonly ILogger _logger;

    public HttpClientCoreObserver(IOptionsMonitor<MetricsObserverOptions> optionsMonitor, ILoggerFactory loggerFactory)
        : base(DefaultObserverName, DiagnosticName, loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);

        string? egressIgnorePattern = optionsMonitor.CurrentValue.EgressIgnorePattern;

        if (egressIgnorePattern != null)
        {
            SetPathMatcher(new Regex(egressIgnorePattern, RegexOptions.None, TimeSpan.FromSeconds(1)));
        }

        _clientTimeMeasure = SteeltoeMetrics.Meter.CreateHistogram<double>("http.client.request.time");
        _clientCountMeasure = SteeltoeMetrics.Meter.CreateHistogram<double>("http.client.request.count");
        _logger = loggerFactory.CreateLogger<HttpClientCoreObserver>();
    }

    public override void ProcessEvent(string eventName, object? value)
    {
        if (value == null)
        {
            return;
        }

        Activity? current = Activity.Current;

        if (current == null)
        {
            return;
        }

        var request = GetPropertyOrDefault<HttpRequestMessage>(value, "Request");

        if (request == null)
        {
            return;
        }

        if (eventName == StopEventName)
        {
            _logger.LogTrace("HandleStopEvent start {Thread}", Thread.CurrentThread.ManagedThreadId);

            var response = GetPropertyOrDefault<HttpResponseMessage>(value, "Response");
            var requestStatus = GetPropertyOrDefault<TaskStatus>(value, "RequestTaskStatus");
            HandleStopEvent(current, request, response, requestStatus);

            _logger.LogTrace("HandleStopEvent finished {Thread}", Thread.CurrentThread.ManagedThreadId);
        }
        else if (eventName == ExceptionEvent)
        {
            _logger.LogTrace("HandleExceptionEvent start {Thread}", Thread.CurrentThread.ManagedThreadId);

            HandleExceptionEvent(current, request);

            _logger.LogTrace("HandleExceptionEvent finished {Thread}", Thread.CurrentThread.ManagedThreadId);
        }
    }

    private void HandleExceptionEvent(Activity current, HttpRequestMessage request)
    {
        HandleStopEvent(current, request, null, TaskStatus.Faulted);
    }

    private void HandleStopEvent(Activity current, HttpRequestMessage request, HttpResponseMessage? response, TaskStatus taskStatus)
    {
        if (ShouldIgnoreRequest(request.RequestUri?.AbsolutePath))
        {
            _logger.LogDebug("HandleStopEvent: Ignoring path: {Path}", SecurityUtilities.SanitizeInput(request.RequestUri?.AbsolutePath));
            return;
        }

        if (current.Duration.TotalMilliseconds > 0)
        {
            ReadOnlySpan<KeyValuePair<string, object?>> labels = GetLabels(request, response, taskStatus).AsReadonlySpan();
            _clientTimeMeasure.Record(current.Duration.TotalMilliseconds, labels);
            _clientCountMeasure.Record(1, labels);
        }
    }

    private IDictionary<string, object?> GetLabels(HttpRequestMessage request, HttpResponseMessage? response, TaskStatus taskStatus)
    {
        string uri = request.RequestUri!.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped);
        string statusCode = GetStatusCode(response, taskStatus);
        string clientName = request.RequestUri.GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped);

        return new Dictionary<string, object?>
        {
            { UriTagKey, uri },
            { StatusTagKey, statusCode },
            { ClientTagKey, clientName },
            { MethodTagKey, request.Method.ToString() }
        };
    }

    private string GetStatusCode(HttpResponseMessage? response, TaskStatus taskStatus)
    {
        if (response != null)
        {
            int value = (int)response.StatusCode;
            return value.ToString(CultureInfo.InvariantCulture);
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
