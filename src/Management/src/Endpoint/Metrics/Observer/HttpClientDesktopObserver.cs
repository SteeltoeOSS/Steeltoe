// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Diagnostics;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

internal sealed class HttpClientDesktopObserver : MetricsObserver
{
    private const string StatusTagKey = "status";
    private const string UriTagKey = "uri";
    private const string MethodTagKey = "method";
    private const string ClientTagKey = "clientName";
    private const string DiagnosticName = "System.Net.Http.Desktop";
    private const string DefaultObserverName = "HttpClientDesktopObserver";
    private const string StopEventName = "System.Net.Http.Desktop.HttpRequestOut.Stop";
    private const string StopExEventName = "System.Net.Http.Desktop.HttpRequestOut.Ex.Stop";

    private readonly Histogram<double> _clientTimeMeasure;
    private readonly Histogram<double> _clientCountMeasure;
    private readonly ILogger _logger;

    public HttpClientDesktopObserver(IOptionsMonitor<MetricsObserverOptions> optionsMonitor, ILoggerFactory loggerFactory)
        : base(DefaultObserverName, DiagnosticName, loggerFactory)
    {
        ArgumentGuard.NotNull(optionsMonitor);

        string? egressIgnorePattern = optionsMonitor.CurrentValue.EgressIgnorePattern;

        if (egressIgnorePattern != null)
        {
            SetPathMatcher(new Regex(egressIgnorePattern, RegexOptions.None, TimeSpan.FromSeconds(1)));
        }

        _clientTimeMeasure = SteeltoeMetrics.Meter.CreateHistogram<double>("http.desktop.client.request.time");
        _clientCountMeasure = SteeltoeMetrics.Meter.CreateHistogram<double>("http.desktop.client.request.count");
        _logger = loggerFactory.CreateLogger<HttpClientDesktopObserver>();
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

        var request = GetPropertyOrDefault<HttpWebRequest>(value, "Request");

        if (request == null)
        {
            return;
        }

        if (eventName == StopEventName)
        {
            _logger.LogTrace("HandleStopEvent start {Thread}", Thread.CurrentThread.ManagedThreadId);

            var response = GetPropertyOrDefault<HttpWebResponse>(value, "Response");

            if (response != null)
            {
                HandleStopEvent(current, request, response.StatusCode);
            }

            _logger.LogTrace("HandleStopEvent finished {Thread}", Thread.CurrentThread.ManagedThreadId);
        }
        else if (eventName == StopExEventName)
        {
            _logger.LogTrace("HandleStopEventEx start {Thread}", Thread.CurrentThread.ManagedThreadId);

            var statusCode = GetPropertyOrDefault<HttpStatusCode>(value, "StatusCode");

            HandleStopEvent(current, request, statusCode);

            _logger.LogTrace("HandleStopEventEx finished {Thread}", Thread.CurrentThread.ManagedThreadId);
        }
    }

    private void HandleStopEvent(Activity current, HttpWebRequest request, HttpStatusCode statusCode)
    {
        if (ShouldIgnoreRequest(request.RequestUri.AbsolutePath))
        {
            _logger.LogDebug("HandleStopEvent: Ignoring path: {Path}", SecurityUtilities.SanitizeInput(request.RequestUri.AbsolutePath));
            return;
        }

        if (current.Duration.TotalMilliseconds > 0)
        {
            ReadOnlySpan<KeyValuePair<string, object?>> labels = GetLabels(request, statusCode).AsReadonlySpan();
            _clientTimeMeasure.Record(current.Duration.TotalMilliseconds, labels);
            _clientCountMeasure.Record(1, labels);
        }
    }

    private IDictionary<string, object?> GetLabels(HttpWebRequest request, HttpStatusCode statusCode)
    {
        string uri = request.RequestUri.GetComponents(UriComponents.PathAndQuery, UriFormat.UriEscaped);
        string status = ((int)statusCode).ToString(CultureInfo.InvariantCulture);
        string clientName = request.RequestUri.GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped);

        return new Dictionary<string, object?>
        {
            { UriTagKey, uri },
            { StatusTagKey, status },
            { ClientTagKey, clientName },
            { MethodTagKey, request.Method }
        };
    }
}
