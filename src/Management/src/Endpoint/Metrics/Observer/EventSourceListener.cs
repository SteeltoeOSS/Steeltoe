// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Management.OpenTelemetry;
using Steeltoe.Management.OpenTelemetry.Metrics;

namespace Steeltoe.Management.Endpoint.Metrics.Observer;

public class EventSourceListener : EventListener
{
    private readonly ILogger<EventSourceListener> _logger;

    protected ConcurrentDictionary<string, Counter<long>> LongCounters { get; set; }

    protected ConcurrentDictionary<string, Counter<double>> DoubleCounters { get; set; }

    internal EventSourceListener(ILogger<EventSourceListener> logger = null)
    {
        _logger = logger;
        LongCounters = new ConcurrentDictionary<string, Counter<long>>();
        DoubleCounters = new ConcurrentDictionary<string, Counter<double>>();
    }

    public override void Dispose()
    {
        try
        {
            base.Dispose();
        }
        catch (Exception)
        {
            // Catch and ignore exceptions
        }
    }

    protected virtual void ExtractAndRecordMetric(string eventSourceName, EventWrittenEventArgs eventData, IDictionary<string, object> labels,
        string[] ignorePayloadNames = null, string[] counterNames = null)
    {
        ReadOnlyCollection<string> payloadNames = eventData.PayloadNames;
        ReadOnlyCollection<object> payload = eventData.Payload;

        List<string> names = payloadNames.Where(name => ignorePayloadNames == null || !ignorePayloadNames.Contains(name)).ToList();

        IDictionary<string, object> currentLabels = GetLabels(payload, names, labels);

        using IEnumerator<object> payloadEnumerator = payload.GetEnumerator();
        using List<string>.Enumerator nameEnumerator = names.GetEnumerator();

        while (nameEnumerator.MoveNext())
        {
            payloadEnumerator.MoveNext();
            string metricName = $"{eventSourceName}.{eventData.EventName}.{nameEnumerator.Current}";
            RecordMetricsWithLabels(metricName, payloadEnumerator.Current, currentLabels);
        }
    }

    protected void SafelyEnableEvents(EventSource eventSource, EventLevel level, EventKeywords matchAnyKeyword)
    {
        try
        {
            EnableEvents(eventSource, level, matchAnyKeyword);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message, ex);
        }
    }

    private IDictionary<string, object> GetLabels(IEnumerable<object> payload, IList<string> names, IDictionary<string, object> labels)
    {
        using IEnumerator<string> nameEnumerator = names.GetEnumerator();
        using IEnumerator<object> payloadEnumerator = payload.GetEnumerator();

        while (nameEnumerator.MoveNext())
        {
            payloadEnumerator.MoveNext();

            switch (payloadEnumerator.Current)
            {
                case string strValue:
                    if (!labels.ContainsKey(nameEnumerator.Current))
                    {
                        labels.Add(nameEnumerator.Current, strValue);
                    }

                    break;
            }
        }

        return labels;
    }

    private void RecordMetricsWithLabels(string metricName, object payloadValue, IDictionary<string, object> labels)
    {
        long? longValue = null;
        double? doubleValue = null;

        switch (payloadValue)
        {
            case string:
                break;
            case short shortValue:
                longValue = shortValue;
                break;
            case int intValue:
                longValue = intValue;
                break;
            case uint unsignedInt:
                longValue = unsignedInt;
                break;
            case long lValue:
                longValue = lValue;
                break;
            case ulong ulValue:
                longValue = (long)ulValue;
                break;
            case double dValue:
                doubleValue = dValue;
                break;
            case bool boolValue:
                longValue = Convert.ToInt64(boolValue);
                break;
            default:
                _logger?.LogDebug($"Unhandled type at {metricName} - {payloadValue.GetType()} - {payloadValue}");
                break;
        }

        if (longValue.HasValue)
        {
            Counter<long> currentMetric = LongCounters.GetOrAddEx(metricName, name => OpenTelemetryMetrics.Meter.CreateCounter<long>(name));
            currentMetric.Add(longValue.Value, labels.AsReadonlySpan());
        }
        else if (doubleValue.HasValue)
        {
            Counter<double> currentMetric = DoubleCounters.GetOrAddEx(metricName, name => OpenTelemetryMetrics.Meter.CreateCounter<double>(name));
            currentMetric.Add(doubleValue.Value, labels.AsReadonlySpan());
        }
    }
}
