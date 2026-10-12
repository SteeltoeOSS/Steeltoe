// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges.Diagnostics;

internal sealed partial class DiagnosticsManager : IObserver<DiagnosticListener>, IDisposable
{
    private readonly ILogger<DiagnosticsManager> _logger;
    private readonly List<DiagnosticObserver> _observers;

    private volatile IDisposable? _listenersSubscription;
    private long _isStarted;
    private long _isDisposed;

    private bool IsStarted => Interlocked.Read(ref _isStarted) == 1;
    private bool IsDisposed => Interlocked.Read(ref _isDisposed) == 1;

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
        if (IsStarted)
        {
            foreach (DiagnosticObserver observer in _observers)
            {
                observer.Subscribe(value);
            }
        }
    }

    public void Start()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (Interlocked.CompareExchange(ref _isStarted, 1, 0) == 0)
        {
            _listenersSubscription = DiagnosticListener.AllListeners.Subscribe(this);
            LogSubscribed();
        }
    }

    public void Stop()
    {
        if (Interlocked.CompareExchange(ref _isStarted, 0, 1) == 1)
        {
            _listenersSubscription?.Dispose();
            _listenersSubscription = null;

            foreach (DiagnosticObserver observer in _observers)
            {
                observer.Dispose();
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _isDisposed, 1) == 0)
        {
            Stop();
            _observers.Clear();
        }
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Subscribed to diagnostic listener.")]
    private partial void LogSubscribed();
}
