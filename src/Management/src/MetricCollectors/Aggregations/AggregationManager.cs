// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;

namespace Steeltoe.Management.MetricCollectors.Aggregations;

internal sealed class AggregationManager
{
    public const double MinCollectionTimeSecs = 0.1;
    private static readonly QuantileAggregation DefaultHistogramConfig = new(0.50, 0.95, 0.99);

    // these fields are modified after construction and accessed on multiple threads, use lock(_lockObject) to ensure the data
    // is synchronized
    private readonly List<Predicate<Instrument>> _instrumentConfigFuncs = new();
    private readonly ConcurrentDictionary<Instrument, InstrumentState> _instrumentStates = new();
    private readonly MeterListener _listener;
    private readonly object _lockObject = new();

    private readonly int _maxTimeSeries;
    private readonly int _maxHistograms;
    private readonly Action<Instrument, LabeledAggregationStatistics> _collectMeasurement;
    private readonly Action<Instrument> _beginInstrumentMeasurements;
    private readonly Action<Instrument> _endInstrumentMeasurements;
    private readonly Action<Instrument> _instrumentPublished;
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
        _maxTimeSeries = maxTimeSeries;
        _maxHistograms = maxHistograms;
        _collectMeasurement = collectMeasurement;
        _beginInstrumentMeasurements = beginInstrumentMeasurements;
        _endInstrumentMeasurements = endInstrumentMeasurements;
        _instrumentPublished = instrumentPublished;
        _initialInstrumentEnumerationComplete = initialInstrumentEnumerationComplete;
        _timeSeriesLimitReached = timeSeriesLimitReached;
        _histogramLimitReached = histogramLimitReached;
        _observableInstrumentCallbackError = observableInstrumentCallbackError;

        _listener = new MeterListener
        {
            InstrumentPublished = (instrument, listener) =>
            {
                _instrumentPublished(instrument);
                InstrumentState? state = GetInstrumentState(instrument);

                if (state != null)
                {
                    _beginInstrumentMeasurements(instrument);
                    listener.EnableMeasurementEvents(instrument, state);
                }
            },
            MeasurementsCompleted = (instrument, cookie) =>
            {
                _endInstrumentMeasurements(instrument);
                RemoveInstrumentState(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<double>((i, m, l, c) => ((InstrumentState)c!).Update(m, l));
        _listener.SetMeasurementEventCallback<float>((i, m, l, c) => ((InstrumentState)c!).Update(m, l));
        _listener.SetMeasurementEventCallback<long>((i, m, l, c) => ((InstrumentState)c!).Update(m, l));
        _listener.SetMeasurementEventCallback<int>((i, m, l, c) => ((InstrumentState)c!).Update(m, l));
        _listener.SetMeasurementEventCallback<short>((i, m, l, c) => ((InstrumentState)c!).Update(m, l));
        _listener.SetMeasurementEventCallback<byte>((i, m, l, c) => ((InstrumentState)c!).Update(m, l));
        _listener.SetMeasurementEventCallback<decimal>((i, m, l, c) => ((InstrumentState)c!).Update((double)m, l));
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
            _instrumentConfigFuncs.Add(instrumentFilter);
        }
    }

    public void Start()
    {
        _listener.Start();
        _initialInstrumentEnumerationComplete();
    }

#pragma warning disable S2953 // Methods named "Dispose" should implement "IDisposable.Dispose"
    public void Dispose()
#pragma warning restore S2953 // Methods named "Dispose" should implement "IDisposable.Dispose"
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
            // protect _instrumentConfigFuncs list 
            lock (_lockObject)
            {
                foreach (Predicate<Instrument> filter in _instrumentConfigFuncs)
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
    internal InstrumentState? BuildInstrumentState(Instrument instrument)
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
        Type? genericDefType = null;
        genericDefType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;

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
        catch (Exception ex)
        {
            _observableInstrumentCallbackError(ex);
        }

        foreach (KeyValuePair<Instrument, InstrumentState> kv in _instrumentStates)
        {
            kv.Value.Collect(kv.Key, labeledAggStats =>
            {
                _collectMeasurement(kv.Key, labeledAggStats);
            });
        }
    }
}
