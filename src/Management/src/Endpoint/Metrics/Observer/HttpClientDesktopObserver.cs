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
using Steeltoe.Management.MetricCollectors;
using Steeltoe.Management.MetricCollectors.Metrics;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

public class HttpClientDesktopObserver : MetricsObserver
{
    private const string StatusTagKey = "status";
    private const string UriTagKey = "uri";
    private const string MethodTagKey = "method";
    private const string ClientTagKey = "clientName";
    internal const string DiagnosticName = "System.Net.Http.Desktop";
    internal const string DefaultObserverName = "HttpClientDesktopObserver";

    internal const string StopEvent = "System.Net.Http.Desktop.HttpRequestOut.Stop";
    internal const string StopExEvent = "System.Net.Http.Desktop.HttpRequestOut.Ex.Stop";
    private readonly Histogram<double> _clientTimeMeasure;
    private readonly Histogram<double> _clientCountMeasure;

    public HttpClientDesktopObserver(IOptionsMonitor<MetricsObserverOptions> options, ILogger<HttpClientDesktopObserver> logger)
        : base(DefaultObserverName, DiagnosticName, logger)
    {
        ArgumentGuard.NotNull(options);
        SetPathMatcher(new Regex(options.CurrentValue.EgressIgnorePattern));

        _clientTimeMeasure = SteeltoeMetrics.Meter.CreateHistogram<double>("http.desktop.client.request.time");
        _clientCountMeasure = SteeltoeMetrics.Meter.CreateHistogram<double>("http.desktop.client.request.count");
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

        var request = DiagnosticHelpers.GetProperty<HttpWebRequest>(value, "Request");

        if (request == null)
        {
            return;
        }

        if (eventName == StopEvent)
        {
            Logger.LogTrace("HandleStopEvent start {thread}", Thread.CurrentThread.ManagedThreadId);

            var response = DiagnosticHelpers.GetProperty<HttpWebResponse>(value, "Response");

            if (response != null)
            {
                HandleStopEvent(current, request, response.StatusCode);
            }

            Logger.LogTrace("HandleStopEvent finished {thread}", Thread.CurrentThread.ManagedThreadId);
        }
        else if (eventName == StopExEvent)
        {
            Logger.LogTrace("HandleStopEventEx start {thread}", Thread.CurrentThread.ManagedThreadId);

            var statusCode = DiagnosticHelpers.GetProperty<HttpStatusCode>(value, "StatusCode");

            HandleStopEvent(current, request, statusCode);

            Logger.LogTrace("HandleStopEventEx finished {thread}", Thread.CurrentThread.ManagedThreadId);
        }
    }

    private void HandleStopEvent(Activity current, HttpWebRequest request, HttpStatusCode statusCode)
    {
        if (ShouldIgnoreRequest(request.RequestUri.AbsolutePath))
        {
            Logger.LogDebug("HandleStopEvent: Ignoring path: {path}", SecurityUtilities.SanitizeInput(request.RequestUri.AbsolutePath));
            return;
        }

        if (current.Duration.TotalMilliseconds > 0)
        {
            IEnumerable<KeyValuePair<string, object>> labels = GetLabels(request, statusCode);
            _clientTimeMeasure.Record(current.Duration.TotalMilliseconds, labels.AsReadonlySpan());
            _clientCountMeasure.Record(1, labels.AsReadonlySpan());
        }
    }

    private IEnumerable<KeyValuePair<string, object>> GetLabels(HttpWebRequest request, HttpStatusCode statusCode)
    {
        string uri = request.RequestUri.GetComponents(UriComponents.PathAndQuery, UriFormat.SafeUnescaped);
        string status = ((int)statusCode).ToString(CultureInfo.InvariantCulture);
        string clientName = request.RequestUri.GetComponents(UriComponents.HostAndPort, UriFormat.UriEscaped);

        return new Dictionary<string, object>
        {
            { UriTagKey, uri },
            { StatusTagKey, status },
            { ClientTagKey, clientName },
            { MethodTagKey, request.Method }
        };
    }
}
