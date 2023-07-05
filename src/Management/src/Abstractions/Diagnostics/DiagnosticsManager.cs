// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Steeltoe.Common;

namespace Steeltoe.Management.Diagnostics;

internal sealed class DiagnosticsManager : IObserver<DiagnosticListener>, IDisposable, IDiagnosticsManager
{
    private readonly IList<IRuntimeDiagnosticSource> _runtimeSources;
    private readonly IList<EventListener> _eventListeners;
    private readonly IList<IDiagnosticObserver> _observers;
    private readonly ILogger<DiagnosticsManager> _logger;

    private bool _isDisposed;
    private IDisposable _listenersSubscription;

    private int _started;

    public DiagnosticsManager(IOptionsMonitor<MetricsObserverOptions> observerOptions, IEnumerable<IRuntimeDiagnosticSource> runtimeSources,
        IEnumerable<IDiagnosticObserver> observers, IEnumerable<EventListener> eventListeners, ILogger<DiagnosticsManager> logger)
    {
        ArgumentGuard.NotNull(observerOptions);
        ArgumentGuard.NotNull(observers);
        ArgumentGuard.NotNull(logger);
        ArgumentGuard.NotNull(runtimeSources);
        ArgumentGuard.NotNull(eventListeners);

        _logger = logger;
        var filteredObservers = new List<IDiagnosticObserver>();

        foreach (IDiagnosticObserver observer in observers)
        {
            if (observerOptions.CurrentValue.IncludeObserver(observer.ObserverName))
            {
                filteredObservers.Add(observer);
            }
        }

        _observers = filteredObservers;
        _runtimeSources = runtimeSources.ToList();
        _eventListeners = eventListeners.ToList();
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
        foreach (IDiagnosticObserver listener in _observers)
        {
            listener.Subscribe(value);
        }
    }

    public void Start()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }

        if (Interlocked.CompareExchange(ref _started, 1, 0) == 0)
        {
            _listenersSubscription = DiagnosticListener.AllListeners.Subscribe(this);

            if (_listenersSubscription != null)
            {
                _logger.LogTrace("Subscribed to Diagnostic Listener");
            }

            if (_runtimeSources != null)
            {
                foreach (IRuntimeDiagnosticSource source in _runtimeSources)
                {
                    source.AddInstrumentation();
                }
            }

            if (_eventListeners != null)
            {
                _logger.LogTrace("Subscribed to EventListeners: {eventListeners}", string.Join(",", _eventListeners.Select(e => e.GetType().Name)));
            }
        }
    }

    public void Stop()
    {
        if (Interlocked.CompareExchange(ref _started, 0, 1) == 1)
        {
            foreach (IDiagnosticObserver listener in _observers)
            {
                listener.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            Stop();
            _observers.Clear();
            _runtimeSources.Clear();
            _eventListeners.Clear();
            _isDisposed = true;
        }
    }
}
