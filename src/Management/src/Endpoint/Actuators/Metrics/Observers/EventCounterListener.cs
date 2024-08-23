// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Management.Diagnostics;

namespace Steeltoe.Management.Endpoint.Actuators.Metrics.Observers;

internal sealed class EventCounterListener : EventListener
{
    private const string EventSourceName = "System.Runtime";
    private const string EventName = "EventCounters";
    private readonly IOptionsMonitor<MetricsObserverOptions> _optionsMonitor;
    private readonly ILogger<EventCounterListener> _logger;
    private readonly bool _isInitialized;

    private readonly Dictionary<string, string?> _refreshInterval = new();

    private readonly ConcurrentDictionary<string, ObservableGauge<double>> _doubleMeasureMetrics = new();
    private readonly ConcurrentDictionary<string, ObservableGauge<long>> _longMeasureMetrics = new();

    private readonly ConcurrentDictionary<string, double> _lastDoubleValue = new();
    private readonly ConcurrentDictionary<string, long> _lastLongValue = new();

    private readonly ConcurrentBag<EventSource> _eventSources = [];

    public EventCounterListener(IOptionsMonitor<MetricsObserverOptions> optionsMonitor, ILogger<EventCounterListener> logger)
    {
        ArgumentNullException.ThrowIfNull(optionsMonitor);
        ArgumentNullException.ThrowIfNull(logger);

        _optionsMonitor = optionsMonitor;
        _logger = logger;
        MetricsObserverOptions observerOptions = optionsMonitor.CurrentValue;

        if (observerOptions.EventCounterEvents)
        {
            _isInitialized = true;

            _refreshInterval = new Dictionary<string, string?>
            {
                { "EventCounterIntervalSec", observerOptions.EventCounterIntervalSec?.ToString(CultureInfo.InvariantCulture) ?? "1" }
            };

            ProcessPreInitEventSources();
        }
    }

    /// <summary>
    /// Processes a new EventSource event.
    /// </summary>
    /// <param name="eventData">
    /// Event to process.
    /// </param>
    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        if (!_isInitialized)
        {
            return;
        }

        try
        {
            if (string.Equals(eventData.EventName, EventName, StringComparison.OrdinalIgnoreCase) && eventData.Payload != null)
            {
                foreach (IDictionary<string, object?>? payload in eventData.Payload.Cast<IDictionary<string, object?>?>())
                {
                    if (payload != null)
                    {
                        ExtractAndRecordMetric(eventData.EventSource.Name, payload);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to write event {Id}", eventData.EventId);
        }
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        ArgumentNullException.ThrowIfNull(eventSource);

        if (EventSourceName.Equals(eventSource.Name, StringComparison.OrdinalIgnoreCase))
        {
            if (!_isInitialized)
            {
                _eventSources.Add(eventSource);
            }
            else
            {
                SafeEnableEvents(eventSource);
            }
        }
    }

    private void ProcessPreInitEventSources()
    {
        foreach (EventSource eventSource in _eventSources)
        {
            SafeEnableEvents(eventSource);
        }
    }

    private void SafeEnableEvents(EventSource eventSource)
    {
        try
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Should not call enable events before initialization");
            }

            if (!_optionsMonitor.CurrentValue.EventCounterEvents)
            {
                return;
            }

            EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All, _refreshInterval);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to enable events for {Source}", eventSource.Guid);
        }
    }

    private void ExtractAndRecordMetric(string eventSourceName, IDictionary<string, object?> eventPayload)
    {
        string metricName = string.Empty;
        double? doubleValue = null;
        long? longValue = null;
        string counterName = string.Empty;
        var labelSet = new List<KeyValuePair<string, object?>>();
        string? counterDisplayUnit = null;
        string? counterDisplayName = null;

        foreach (KeyValuePair<string, object?> payload in eventPayload)
        {
            string key = payload.Key;

            switch (key)
            {
                case var _ when key.Equals("Name", StringComparison.OrdinalIgnoreCase):
                    counterName = payload.Value?.ToString() ?? string.Empty;
                    IList<string> includedMetrics = _optionsMonitor.CurrentValue.IncludedMetrics;
                    IList<string> excludedMetrics = _optionsMonitor.CurrentValue.ExcludedMetrics;

                    if ((includedMetrics.Any() && !includedMetrics.Contains(counterName)) || excludedMetrics.Contains(counterName))
                    {
                        return;
                    }

                    break;
                case var _ when key.Equals("DisplayName", StringComparison.OrdinalIgnoreCase):
                    counterDisplayName = payload.Value?.ToString();
                    labelSet.Add(KeyValuePair.Create("DisplayName", (object?)counterDisplayName));
                    break;
                case var _ when key.Equals("DisplayUnits", StringComparison.OrdinalIgnoreCase):
                    counterDisplayUnit = payload.Value?.ToString();
                    labelSet.Add(KeyValuePair.Create("DisplayUnits", (object?)counterDisplayUnit));
                    break;
                case var _ when key.Equals("Mean", StringComparison.OrdinalIgnoreCase):
                    doubleValue = Convert.ToDouble(payload.Value, CultureInfo.InvariantCulture);
                    break;
                case var _ when key.Equals("Increment", StringComparison.OrdinalIgnoreCase):
                    longValue = Convert.ToInt64(payload.Value, CultureInfo.InvariantCulture);
                    break;
                case var _ when key.Equals("IntervalSec", StringComparison.OrdinalIgnoreCase):
                    doubleValue = Convert.ToDouble(payload.Value, CultureInfo.InvariantCulture);
                    break;
                case var _ when key.Equals("Count", StringComparison.OrdinalIgnoreCase):
                    longValue = Convert.ToInt64(payload.Value, CultureInfo.InvariantCulture);
                    break;
                case var _ when key.Equals("Metadata", StringComparison.OrdinalIgnoreCase):
                    string? metadata = payload.Value?.ToString();

                    if (!string.IsNullOrEmpty(metadata))
                    {
                        string[] keyValuePairStrings = metadata.Split(',');

                        foreach (string keyValuePairString in keyValuePairStrings)
                        {
                            string[] keyValuePair = keyValuePairString.Split(':');
                            labelSet.Add(KeyValuePair.Create(keyValuePair[0], (object?)keyValuePair[1]));
                        }
                    }

                    break;
            }

            metricName = $"{eventSourceName}.{counterName}";
        }

        if (doubleValue.HasValue)
        {
            _lastDoubleValue[metricName] = doubleValue.Value;

            _doubleMeasureMetrics.GetOrAdd(metricName,
                name => SteeltoeMetrics.Meter.CreateObservableGauge($"{name}", () => ObserveDouble(name, labelSet), counterDisplayUnit, counterDisplayName));
        }
        else if (longValue.HasValue)
        {
            _lastLongValue[metricName] = longValue.Value;

            _longMeasureMetrics.GetOrAdd(metricName,
                name => SteeltoeMetrics.Meter.CreateObservableGauge($"{name}", () => ObserveLong(name, labelSet), counterDisplayUnit, counterDisplayName));
        }
    }

    private Measurement<double> ObserveDouble(string name, List<KeyValuePair<string, object?>> labelSet)
    {
        return new Measurement<double>(_lastDoubleValue[name], labelSet);
    }

    private Measurement<long> ObserveLong(string name, List<KeyValuePair<string, object?>> labelSet)
    {
        return new Measurement<long>(_lastLongValue[name], labelSet);
    }
}
