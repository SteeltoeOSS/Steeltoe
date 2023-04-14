// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Diagnostics;

public class DiagnosticsManager : IObserver<DiagnosticListener>, IDisposable, IDiagnosticsManager
{
    private static readonly Lazy<DiagnosticsManager> AsSingleton = new(() => new DiagnosticsManager(NullLogger<DiagnosticsManager>.Instance));

    private bool _isDisposed;
    private IDisposable _listenersSubscription;
    private ILogger<DiagnosticsManager> _logger;
    private readonly IList<IDiagnosticObserver> _innerObservers;
    private readonly IList<IRuntimeDiagnosticSource> _innerSources;
    private readonly IList<EventListener> _eventListeners;

    private int _started;

    public static DiagnosticsManager Instance => AsSingleton.Value;

    public IList<IDiagnosticObserver> Observers => _innerObservers;

    //public IList<IRuntimeDiagnosticSource> Sources => InnerSources;

    public DiagnosticsManager(IOptionsMonitor<MetricsObserverOptions> observerOptions, IEnumerable<IRuntimeDiagnosticSource> runtimeSources,
        IEnumerable<IDiagnosticObserver> observers, IEnumerable<EventListener> eventListeners, ILogger<DiagnosticsManager> logger)
    {
        ArgumentGuard.NotNull(observers);
        ArgumentGuard.NotNull(logger);

        _logger = logger;
        var filteredObservers = new List<IDiagnosticObserver>();

        foreach (IDiagnosticObserver observer in observers)
        {
            if (observerOptions.CurrentValue.IncludeObserver(observer.ObserverName))
            {
                filteredObservers.Add(observer);
            }
        }

        _innerObservers = filteredObservers;
        _innerSources = runtimeSources.ToList();
        _eventListeners = eventListeners.ToList();
    }

    internal DiagnosticsManager(ILogger<DiagnosticsManager> logger)
    {
        ArgumentGuard.NotNull(logger);
        _logger = logger;
        _innerObservers = new List<IDiagnosticObserver>();
        _innerSources = new List<IRuntimeDiagnosticSource>();
    }

    public void OnCompleted()
    {
        // for future use
    }

    public void OnError(Exception error)
    {
        // for future use
    }

    public void OnNext(DiagnosticListener value)
    {
        foreach (IDiagnosticObserver listener in _innerObservers)
        {
            listener.Subscribe(value);
        }
    }

    public void Start()
    {
        if (Interlocked.CompareExchange(ref _started, 1, 0) == 0)
        {
            _listenersSubscription = DiagnosticListener.AllListeners.Subscribe(this);
        }
    }

    public void Stop()
    {
        if (Interlocked.CompareExchange(ref _started, 0, 1) == 1)
        {
            foreach (IDiagnosticObserver listener in _innerObservers)
            {
                listener.Dispose();
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            Stop();

            _innerObservers?.Clear();
            _innerSources?.Clear();
            _logger = null;

            _isDisposed = true;
        }
    }
}
