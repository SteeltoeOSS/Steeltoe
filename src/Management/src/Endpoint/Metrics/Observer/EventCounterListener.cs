// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;
using Steeltoe.Management.Diagnostics;
using Steeltoe.Management.MetricCollectors;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

public class EventCounterListener : EventListener
{
    private const string EventSourceName = "System.Runtime";
    private const string EventName = "EventCounters";
    private readonly IOptionsMonitor<MetricsObserverOptions> _options;
    private readonly ILogger<EventCounterListener> _logger;
    private readonly bool _isInitialized;

    private readonly Dictionary<string, string> _refreshInterval = new()
    {
        { "EventCounterIntervalSec", "1" }
    };
    

    private readonly ConcurrentDictionary<string, ObservableGauge<double>> _doubleMeasureMetrics = new();
    private readonly ConcurrentDictionary<string, ObservableGauge<long>> _longMeasureMetrics = new();

    private readonly ConcurrentDictionary<string, double> _lastDoubleValue = new();
    private readonly ConcurrentDictionary<string, long> _lastLongValue = new();

    private readonly ConcurrentBag<EventSource> _eventSources = new();

    public EventCounterListener(IOptionsMonitor<MetricsObserverOptions> options, ILogger<EventCounterListener> logger = null)
    {
        ArgumentGuard.NotNull(options);

        _options = options;
        _logger = logger;
        if (options.CurrentValue.EventCounterEvents)
        {
            _isInitialized = true;
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
        ArgumentGuard.NotNull(eventData);

        if (!_isInitialized)
        {
            return;
        }

        try
        {
            if (eventData.EventName.Equals(EventName, StringComparison.OrdinalIgnoreCase))
            {
                foreach (IDictionary<string, object> payload in eventData.Payload)
                {
                    ExtractAndRecordMetric(eventData.EventSource.Name, payload);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);
        }
    }
    
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        ArgumentGuard.NotNull(eventSource);
        
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
            if (_options?.CurrentValue?.EventCounterEvents != true)
            {
                return;
            }

            EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All, _refreshInterval);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to enable events: {ex.Message}", ex);
        }
    }

    private void ExtractAndRecordMetric(string eventSourceName, IDictionary<string, object> eventPayload)
    {
        string metricName = string.Empty;
        double? doubleValue = null;
        long? longValue = null;
        string counterName = string.Empty;
        var labelSet = new List<KeyValuePair<string, object>>();
        string counterDisplayUnit = null;
        string counterDisplayName = null;

        foreach (KeyValuePair<string, object> payload in eventPayload)
        {
            string key = payload.Key;

            switch (key)
            {
                case var _ when key.Equals("Name", StringComparison.OrdinalIgnoreCase):
                    counterName = payload.Value.ToString();
                    var includedMetrics = _options.CurrentValue.IncludedMetrics;
                    var excludedMetrics = _options.CurrentValue.ExcludedMetrics;
                    if ((includedMetrics.Any() && !includedMetrics.Contains(counterName)) || excludedMetrics.Contains(counterName))
                    {
                        return;
                    }

                    break;
                case var _ when key.Equals("DisplayName", StringComparison.OrdinalIgnoreCase):
                    counterDisplayName = payload.Value.ToString();
                    labelSet.Add(KeyValuePair.Create("DisplayName", (object)counterDisplayName));
                    break;
                case var _ when key.Equals("DisplayUnits", StringComparison.OrdinalIgnoreCase):
                    counterDisplayUnit = payload.Value.ToString();
                    labelSet.Add(KeyValuePair.Create("DisplayUnits", (object)counterDisplayUnit));
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
                    string metadata = payload.Value.ToString();

                    if (!string.IsNullOrEmpty(metadata))
                    {
                        string[] keyValuePairStrings = metadata.Split(',');

                        foreach (string keyValuePairString in keyValuePairStrings)
                        {
                            string[] keyValuePair = keyValuePairString.Split(':');
                            labelSet.Add(KeyValuePair.Create(keyValuePair[0], (object)keyValuePair[1]));
                        }
                    }

                    break;
            }

            metricName = $"{eventSourceName}.{counterName}";
        }

        if (doubleValue.HasValue)
        {
            _lastDoubleValue[metricName] = doubleValue.Value;

            _doubleMeasureMetrics.GetOrAddEx(metricName,
                name => SteeltoeMetrics.Meter.CreateObservableGauge($"{name}", () => ObserveDouble(name, labelSet), counterDisplayUnit, counterDisplayName));
        }
        else if (longValue.HasValue)
        {
            _lastLongValue[metricName] = longValue.Value;

            _longMeasureMetrics.GetOrAddEx(metricName,
                name => SteeltoeMetrics.Meter.CreateObservableGauge($"{name}", () => ObserveLong(name, labelSet), counterDisplayUnit, counterDisplayName));
        }
    }

    private Measurement<double> ObserveDouble(string name, List<KeyValuePair<string, object>> labelSet)
    {
        return new Measurement<double>(_lastDoubleValue[name], labelSet);
    }

    private Measurement<long> ObserveLong(string name, List<KeyValuePair<string, object>> labelSet)
    {
        return new Measurement<long>(_lastLongValue[name], labelSet);
    }
}
