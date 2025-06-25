// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges.Diagnostics;

internal sealed class DiagnosticsManager : IObserver<DiagnosticListener>, IDisposable
{
    private readonly ILogger<DiagnosticsManager> _logger;
    private readonly List<DiagnosticObserver> _observers;

    private bool _isDisposed;
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
    // disabled because an object reference is needed to not dispose when method scope is completed.
    private IDisposable? _listenersSubscription;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables

    private int _started;

    public DiagnosticsManager(IEnumerable<DiagnosticObserver> observers, ILogger<DiagnosticsManager> logger)
    {
        ArgumentNullException.ThrowIfNull(observers);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        _observers = observers.ToList();
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
        foreach (DiagnosticObserver observer in _observers)
        {
            observer.Subscribe(value);
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
        }
    }

    public void Stop()
    {
        if (Interlocked.CompareExchange(ref _started, 0, 1) == 1)
        {
            foreach (DiagnosticObserver observer in _observers)
            {
                observer.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (!_isDisposed)
        {
            Stop();
            _observers.Clear();
            _isDisposed = true;
        }
    }
}
