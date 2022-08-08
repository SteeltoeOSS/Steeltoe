// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Common.Diagnostics;

public class DiagnosticsManager : IObserver<DiagnosticListener>, IDisposable, IDiagnosticsManager
{
    private static readonly Lazy<DiagnosticsManager> AsSingleton = new(() => new DiagnosticsManager());

    private bool _isDisposed;
    internal IDisposable ListenersSubscription;
    internal ILogger<DiagnosticsManager> Logger;
    internal IList<IDiagnosticObserver> InnerObservers;
    internal IList<IRuntimeDiagnosticSource> InnerSources;
    internal IList<EventListener> EventListeners;

    internal bool WorkerThreadShutdown;
    internal int Started;

    public static DiagnosticsManager Instance => AsSingleton.Value;

    public IList<IDiagnosticObserver> Observers => InnerObservers;

    public IList<IRuntimeDiagnosticSource> Sources => InnerSources;

    public DiagnosticsManager(IEnumerable<IRuntimeDiagnosticSource> runtimeSources, IEnumerable<IDiagnosticObserver> observers,
        IEnumerable<EventListener> eventListeners, ILogger<DiagnosticsManager> logger = null)
    {
        ArgumentGuard.NotNull(observers);

        Logger = logger;
        InnerObservers = observers.ToList();
        InnerSources = runtimeSources.ToList();
        EventListeners = eventListeners.ToList();
    }

    internal DiagnosticsManager(ILogger<DiagnosticsManager> logger = null)
    {
        Logger = logger;
        InnerObservers = new List<IDiagnosticObserver>();
        InnerSources = new List<IRuntimeDiagnosticSource>();
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
        foreach (IDiagnosticObserver listener in InnerObservers)
        {
            listener.Subscribe(value);
        }
    }

    public void Start()
    {
        if (Interlocked.CompareExchange(ref Started, 1, 0) == 0)
        {
            ListenersSubscription = DiagnosticListener.AllListeners.Subscribe(this);
        }
    }

    public void Stop()
    {
        if (Interlocked.CompareExchange(ref Started, 0, 1) == 1)
        {
            WorkerThreadShutdown = true;

            foreach (IDiagnosticObserver listener in InnerObservers)
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

            InnerObservers?.Clear();
            InnerSources?.Clear();
            Logger = null;

            _isDisposed = true;
        }
    }
}
