// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Threading;

namespace Steeltoe.Common.Diagnostics;

public class DiagnosticsManager : IObserver<DiagnosticListener>, IDisposable, IDiagnosticsManager
{
    internal IDisposable _listenersSubscription;
    internal ILogger<DiagnosticsManager> _logger;
    internal IList<IDiagnosticObserver> _observers;
    internal IList<IRuntimeDiagnosticSource> _sources;
    internal IList<EventListener> _eventListeners;

    internal bool _workerThreadShutdown = false;
    internal int _started = 0;
    private static readonly Lazy<DiagnosticsManager> AsSingleton = new (() => new DiagnosticsManager());

    public static DiagnosticsManager Instance => AsSingleton.Value;

    public DiagnosticsManager(
        IEnumerable<IRuntimeDiagnosticSource> runtimeSources,
        IEnumerable<IDiagnosticObserver> observers,
        IEnumerable<EventListener> eventListeners,
        ILogger<DiagnosticsManager> logger = null)
    {
        if (observers == null)
        {
            throw new ArgumentNullException(nameof(observers));
        }

        _logger = logger;
        _observers = observers.ToList();
        _sources = runtimeSources.ToList();
        _eventListeners = eventListeners.ToList();
    }

    internal DiagnosticsManager(ILogger<DiagnosticsManager> logger = null)
    {
        _logger = logger;
        _observers = new List<IDiagnosticObserver>();
        _sources = new List<IRuntimeDiagnosticSource>();
    }

    public IList<IDiagnosticObserver> Observers => _observers;

    public IList<IRuntimeDiagnosticSource> Sources => _sources;

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
        foreach (var listener in _observers)
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
            _workerThreadShutdown = true;

            foreach (var listener in _observers)
            {
                listener.Dispose();
            }
        }
    }

    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        // Cleanup
        if (!_disposed)
        {
            if (disposing)
            {
                Stop();

                if (_observers != null)
                {
                    _observers.Clear();
                }

                if (_sources != null)
                {
                    _sources.Clear();
                }

                _logger = null;
            }

            _disposed = true;
        }
    }

    ~DiagnosticsManager()
    {
        Dispose(false);
    }
}