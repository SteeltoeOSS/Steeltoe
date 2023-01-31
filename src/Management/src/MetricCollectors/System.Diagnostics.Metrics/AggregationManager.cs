// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.Metrics;

internal sealed class AggregationManager
{
    public const double MinCollectionTimeSecs = 0.1;
#pragma warning disable SA1311 // Static readonly fields should begin with upper-case letter
    private static readonly QuantileAggregation s_defaultHistogramConfig = new QuantileAggregation(new double[] { 0.50, 0.95, 0.99 });
#pragma warning restore SA1311 // Static readonly fields should begin with upper-case letter

    // these fields are modified after construction and accessed on multiple threads, use lock(this) to ensure the data
    // is synchronized
    private readonly List<Predicate<Instrument>> _instrumentConfigFuncs = new();
    // private TimeSpan _collectionPeriod;

    private readonly ConcurrentDictionary<Instrument, InstrumentState> _instrumentStates = new();
   // private readonly CancellationTokenSource _cts = new();
   // private Thread? _collectThread;
    private readonly MeterListener _listener;
    private int _currentTimeSeries;
    private int _currentHistograms;

    private readonly int _maxTimeSeries;
    private readonly int _maxHistograms;
    private readonly Action<Instrument, LabeledAggregationStatistics> _collectMeasurement;
    // private readonly Action<DateTime, DateTime> _beginCollection;
    // private readonly Action<DateTime, DateTime> _endCollection;
    private readonly Action<Instrument> _beginInstrumentMeasurements;
    private readonly Action<Instrument> _endInstrumentMeasurements;
    private readonly Action<Instrument> _instrumentPublished;
    private readonly Action _initialInstrumentEnumerationComplete;
    // private readonly Action<Exception> _collectionError;
    // private readonly Action _timeSeriesLimitReached;
    // private readonly Action _histogramLimitReached;
    // private readonly Action<Exception> _observableInstrumentCallbackError;

    public AggregationManager(
        int maxTimeSeries,
        int maxHistograms,
        Action<Instrument, LabeledAggregationStatistics> collectMeasurement,
        Action<DateTime, DateTime> beginCollection,
        Action<DateTime, DateTime> endCollection,
        Action<Instrument> beginInstrumentMeasurements,
        Action<Instrument> endInstrumentMeasurements,
        Action<Instrument> instrumentPublished,
        Action initialInstrumentEnumerationComplete
       /* //Action<Exception> collectionError,
        //Action timeSeriesLimitReached,
        //Action histogramLimitReached,
        //Action<Exception> observableInstrumentCallbackError */
        )
    {
        _maxTimeSeries = maxTimeSeries;
        _maxHistograms = maxHistograms;
        _collectMeasurement = collectMeasurement;
        // _beginCollection = beginCollection;
        // _endCollection = endCollection;
        _beginInstrumentMeasurements = beginInstrumentMeasurements;
        _endInstrumentMeasurements = endInstrumentMeasurements;
        _instrumentPublished = instrumentPublished;
        _initialInstrumentEnumerationComplete = initialInstrumentEnumerationComplete;
       /* //_collectionError = collectionError;
        //_timeSeriesLimitReached = timeSeriesLimitReached;
        //_histogramLimitReached = histogramLimitReached;
        //_observableInstrumentCallbackError = observableInstrumentCallbackError;
       */
        _listener = new MeterListener()
        {
            InstrumentPublished = (instrument, listener) =>
            {
                _instrumentPublished(instrument);
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
                InstrumentState? state = GetInstrumentState(instrument);
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
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
#pragma warning disable S1905 // Redundant casts should not be used
        _listener.SetMeasurementEventCallback<double>((i, m, l, c) => ((InstrumentState)c!).Update((double)m, l));
#pragma warning restore S1905 // Redundant casts should not be used
        _listener.SetMeasurementEventCallback<float>((i, m, l, c) => ((InstrumentState)c!).Update((double)m, l));
        _listener.SetMeasurementEventCallback<long>((i, m, l, c) => ((InstrumentState)c!).Update((double)m, l));
        _listener.SetMeasurementEventCallback<int>((i, m, l, c) => ((InstrumentState)c!).Update((double)m, l));
        _listener.SetMeasurementEventCallback<short>((i, m, l, c) => ((InstrumentState)c!).Update((double)m, l));
        _listener.SetMeasurementEventCallback<byte>((i, m, l, c) => ((InstrumentState)c!).Update((double)m, l));
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
#pragma warning disable S2551 // Shared resources should not be used for locking
        lock (this)
        {
            _instrumentConfigFuncs.Add(instrumentFilter);
        }
#pragma warning restore S2551 // Shared resources should not be used for locking
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

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    private InstrumentState? GetInstrumentState(Instrument instrument)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        if (!_instrumentStates.TryGetValue(instrument, out InstrumentState? instrumentState))
        {
#pragma warning disable S2551 // Shared resources should not be used for locking
            // protect _instrumentConfigFuncs list
            lock (this) 
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
#pragma warning restore S2551 // Shared resources should not be used for locking
        }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        return instrumentState;
    }

    [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
                    Justification = "MakeGenericType is creating instances over reference types that works fine in AOT.")]
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    internal InstrumentState? BuildInstrumentState(Instrument instrument)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        Func<Aggregator?>? createAggregatorFunc = GetAggregatorFactory(instrument);
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        if (createAggregatorFunc == null)
        {
            return null;
        }
        Type aggregatorType = createAggregatorFunc.GetType().GenericTypeArguments[0];
        Type instrumentStateType = typeof(InstrumentState<>).MakeGenericType(aggregatorType);
        return (InstrumentState)Activator.CreateInstance(instrumentStateType, createAggregatorFunc)!;
    }

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    private Func<Aggregator?>? GetAggregatorFactory(Instrument instrument)
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    {
        Type type = instrument.GetType();
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        Type? genericDefType = null;
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        genericDefType = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
        if (genericDefType == typeof(Counter<>))
        {
            return () =>
            {
#pragma warning disable S2551 // Shared resources should not be used for locking
                lock (this)
                {
                    return CheckTimeSeriesAllowed() ? new RateSumAggregator() : null;
                }
#pragma warning restore S2551 // Shared resources should not be used for locking
            };
        }
        else if (genericDefType == typeof(ObservableCounter<>))
        {
            return () =>
            {
#pragma warning disable S2551 // Shared resources should not be used for locking
                lock (this)
                {
                    return CheckTimeSeriesAllowed() ? new RateAggregator() : null;
                }
#pragma warning restore S2551 // Shared resources should not be used for locking
            };
        }
        else if (genericDefType == typeof(ObservableGauge<>))
        {
            return () =>
            {
#pragma warning disable S2551 // Shared resources should not be used for locking
                lock (this)
                {
                    return CheckTimeSeriesAllowed() ? new LastValue() : null;
                }
#pragma warning restore S2551 // Shared resources should not be used for locking
            };
        }
        else if (genericDefType == typeof(Histogram<>))
        {
            return () =>
            {
#pragma warning disable S2551 // Shared resources should not be used for locking
                lock (this)
                {
                    // checking currentHistograms first because avoiding unexpected increment of TimeSeries count.
                    return (!CheckHistogramAllowed() || !CheckTimeSeriesAllowed()) ?
                        null :
                        new ExponentialHistogramAggregator(s_defaultHistogramConfig);
                }
#pragma warning restore S2551 // Shared resources should not be used for locking
            };
        }
        else
        {
            return null;
        }
    }

    private bool CheckTimeSeriesAllowed()
    {
        if (_currentTimeSeries < _maxTimeSeries)
        {
            _currentTimeSeries++;
            return true;
        }
        else if (_currentTimeSeries == _maxTimeSeries)
        {
            _currentTimeSeries++;
            // _timeSeriesLimitReached(); Handle inline
            // Console.WriteLine("Time series limit reached");
            return false;
        }
        else
        {
            return false;
        }
    }

    private bool CheckHistogramAllowed()
    {
        if (_currentHistograms < _maxHistograms)
        {
            _currentHistograms++;
            return true;
        }
        else if (_currentHistograms == _maxHistograms)
        {
            _currentHistograms++;
            // _histogramLimitReached();

            // Console.WriteLine("Histogram series limit reached");
            return false;
        }
        else
        {
            return false;
        }
    }

    internal void Collect()
    {
        try
        {
            _listener.RecordObservableInstruments();
        }
        catch (Exception)
        {
            // _observableInstrumentCallbackError(e);
            // Console.WriteLine(e);
        }

        foreach (KeyValuePair<Instrument, InstrumentState> kv in _instrumentStates)
        {
            kv.Value.Collect(kv.Key, (LabeledAggregationStatistics labeledAggStats) =>
            {
                _collectMeasurement(kv.Key, labeledAggStats);
            });
        }
    }
}
