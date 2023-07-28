// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Steeltoe.Common;

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal sealed class AggregationManager : IDisposable
{
    private static readonly QuantileAggregation DefaultHistogramConfig = new(0.50, 0.95, 0.99);

    // these fields are modified after construction and accessed on multiple threads, use lock(_lockObject) to ensure the data
    // is synchronized
    private readonly List<Predicate<Instrument>> _instrumentConfigPredicates = new();
    private readonly ConcurrentDictionary<Instrument, InstrumentState> _instrumentStates = new();
    private readonly MeterListener _listener;
    private readonly object _lockObject = new();

    private readonly int _maxTimeSeries;
    private readonly int _maxHistograms;
    private readonly Action<Instrument, LabeledAggregationStatistics> _collectMeasurement;
    private readonly Action _initialInstrumentEnumerationComplete;
    private readonly Action _timeSeriesLimitReached;
    private readonly Action _histogramLimitReached;
    private readonly Action<Exception> _observableInstrumentCallbackError;
    private int _currentTimeSeries;
    private int _currentHistograms;

    public AggregationManager(int maxTimeSeries, int maxHistograms, Action<Instrument, LabeledAggregationStatistics> collectMeasurement,
        Action<Instrument> beginInstrumentMeasurements, Action<Instrument> endInstrumentMeasurements, Action<Instrument> instrumentPublished,
        Action initialInstrumentEnumerationComplete, Action timeSeriesLimitReached, Action histogramLimitReached,
        Action<Exception> observableInstrumentCallbackError)
    {
        ArgumentGuard.NotNull(collectMeasurement);
        ArgumentGuard.NotNull(beginInstrumentMeasurements);
        ArgumentGuard.NotNull(endInstrumentMeasurements);
        ArgumentGuard.NotNull(instrumentPublished);
        ArgumentGuard.NotNull(initialInstrumentEnumerationComplete);
        ArgumentGuard.NotNull(timeSeriesLimitReached);
        ArgumentGuard.NotNull(histogramLimitReached);
        ArgumentGuard.NotNull(observableInstrumentCallbackError);

        _maxTimeSeries = maxTimeSeries;
        _maxHistograms = maxHistograms;
        _collectMeasurement = collectMeasurement;
        _initialInstrumentEnumerationComplete = initialInstrumentEnumerationComplete;
        _timeSeriesLimitReached = timeSeriesLimitReached;
        _histogramLimitReached = histogramLimitReached;
        _observableInstrumentCallbackError = observableInstrumentCallbackError;

        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                instrumentPublished(instrument);
                InstrumentState? state = GetInstrumentState(instrument);

                if (state != null)
                {
                    beginInstrumentMeasurements(instrument);
                    listener.EnableMeasurementEvents(instrument, state);
                }
            },
            MeasurementsCompleted = (instrument, _) =>
            {
                endInstrumentMeasurements(instrument);
                RemoveInstrumentState(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<double>((_, m, l, c) => ((InstrumentState)c!).Update(m, l));
        _listener.SetMeasurementEventCallback<float>((_, m, l, c) => ((InstrumentState)c!).Update(m, l));
        _listener.SetMeasurementEventCallback<long>((_, m, l, c) => ((InstrumentState)c!).Update(m, l));
        _listener.SetMeasurementEventCallback<int>((_, m, l, c) => ((InstrumentState)c!).Update(m, l));
        _listener.SetMeasurementEventCallback<short>((_, m, l, c) => ((InstrumentState)c!).Update(m, l));
        _listener.SetMeasurementEventCallback<byte>((_, m, l, c) => ((InstrumentState)c!).Update(m, l));
        _listener.SetMeasurementEventCallback<decimal>((_, m, l, c) => ((InstrumentState)c!).Update((double)m, l));
    }

    public void Include(string meterName)
    {
        Include(i => i.Meter.Name == meterName);
    }

    public void Include(string meterName, string instrumentName)
    {
        Include(i => i.Meter.Name == meterName && i.Name == instrumentName);
    }

    private void Include(Predicate<Instrument> instrumentFilter)
    {
        lock (_lockObject)
        {
            _instrumentConfigPredicates.Add(instrumentFilter);
        }
    }

    public void Start()
    {
        _listener.Start();
        _initialInstrumentEnumerationComplete();
    }

    public void Dispose()
    {
        _listener.Dispose();
    }

    private void RemoveInstrumentState(Instrument instrument)
    {
        _instrumentStates.TryRemove(instrument, out _);
    }

    private InstrumentState? GetInstrumentState(Instrument instrument)
    {
        if (!_instrumentStates.TryGetValue(instrument, out InstrumentState? instrumentState))
        {
            // protect _instrumentConfigPredicates list
            lock (_lockObject)
            {
                foreach (Predicate<Instrument> filter in _instrumentConfigPredicates)
                {
                    if (filter(instrument))
                    {
                        instrumentState = BuildInstrumentState(instrument);

                        if (instrumentState != null)
                        {
                            _instrumentStates.TryAdd(instrument, instrumentState);
                            // I don't think it is possible for the instrument to be removed immediately
                            // and instrumentState = _instrumentStates[instrument] should work, but writing
                            // this defensively.
                            _instrumentStates.TryGetValue(instrument, out instrumentState);
                        }

                        break;
                    }
                }
            }
        }

        return instrumentState;
    }

    [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
        Justification = "MakeGenericType is creating instances over reference types that works fine in AOT.")]
    private InstrumentState? BuildInstrumentState(Instrument instrument)
    {
        Func<Aggregator?>? createAggregatorFunc = GetAggregatorFactory(instrument);

        if (createAggregatorFunc == null)
        {
            return null;
        }

        Type aggregatorType = createAggregatorFunc.GetType().GenericTypeArguments[0];
        Type instrumentStateType = typeof(InstrumentState<>).MakeGenericType(aggregatorType);
        return (InstrumentState)Activator.CreateInstance(instrumentStateType, createAggregatorFunc)!;
    }

    private Func<Aggregator?>? GetAggregatorFactory(Instrument instrument)
    {
        Type type = instrument.GetType();
        Type? genericDefType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;

        if (genericDefType == typeof(Counter<>))
        {
            return () =>
            {
                lock (_lockObject)
                {
                    return CheckTimeSeriesAllowed() ? new RateSumAggregator() : null;
                }
            };
        }

        if (genericDefType == typeof(ObservableCounter<>))
        {
            return () =>
            {
                lock (_lockObject)
                {
                    return CheckTimeSeriesAllowed() ? new RateAggregator() : null;
                }
            };
        }

        if (genericDefType == typeof(ObservableGauge<>))
        {
            return () =>
            {
                lock (_lockObject)
                {
                    return CheckTimeSeriesAllowed() ? new LastValue() : null;
                }
            };
        }

        if (genericDefType == typeof(Histogram<>))
        {
            return () =>
            {
                lock (_lockObject)
                {
                    // checking currentHistograms first because avoiding unexpected increment of TimeSeries count.
                    return !CheckHistogramAllowed() || !CheckTimeSeriesAllowed() ? null : new ExponentialHistogramAggregator(DefaultHistogramConfig);
                }
            };
        }

        return null;
    }

    private bool CheckTimeSeriesAllowed()
    {
        if (_currentTimeSeries < _maxTimeSeries)
        {
            _currentTimeSeries++;
            return true;
        }

        if (_currentTimeSeries == _maxTimeSeries)
        {
            _currentTimeSeries++;
            _timeSeriesLimitReached();
            return false;
        }

        return false;
    }

    private bool CheckHistogramAllowed()
    {
        if (_currentHistograms < _maxHistograms)
        {
            _currentHistograms++;
            return true;
        }

        if (_currentHistograms == _maxHistograms)
        {
            _currentHistograms++;
            _histogramLimitReached();

            return false;
        }

        return false;
    }

    internal void Collect()
    {
        try
        {
            _listener.RecordObservableInstruments();
        }
        catch (Exception exception)
        {
            _observableInstrumentCallbackError(exception);
        }

        foreach (KeyValuePair<Instrument, InstrumentState> kv in _instrumentStates)
        {
            kv.Value.Collect(labeledAggStats =>
            {
                _collectMeasurement(kv.Key, labeledAggStats);
            });
        }
    }
}
