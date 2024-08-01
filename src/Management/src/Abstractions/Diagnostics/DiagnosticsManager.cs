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
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
    // disabled because an object reference is needed to not dispose when method scope is completed.
    private IDisposable? _listenersSubscription;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables

    private volatile int _started;

    public DiagnosticsManager(IOptionsMonitor<MetricsObserverOptions> observerOptions, IEnumerable<IRuntimeDiagnosticSource> runtimeSources,
        IEnumerable<IDiagnosticObserver> observers, IEnumerable<EventListener> eventListeners, ILogger<DiagnosticsManager> logger)
    {
        ArgumentGuard.NotNull(observerOptions);
        ArgumentGuard.NotNull(runtimeSources);
        ArgumentGuard.NotNull(observers);
        ArgumentGuard.NotNull(eventListeners);
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
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (Interlocked.CompareExchange(ref _started, 1, 0) == 0)
        {
            _listenersSubscription = DiagnosticListener.AllListeners.Subscribe(this);

            if (_listenersSubscription != null)
            {
                _logger.LogTrace("Subscribed to Diagnostic Listener");
            }

            foreach (IRuntimeDiagnosticSource source in _runtimeSources)
            {
                source.AddInstrumentation();
            }

            _logger.LogTrace("Subscribed to EventListeners: {EventListeners}", string.Join(",", _eventListeners.Select(listener => listener.GetType().Name)));
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
