// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;

namespace Steeltoe.Management.Diagnostics;

public abstract class DiagnosticObserver : IDiagnosticObserver
{
    private readonly ILogger _logger;

    private IDisposable _subscription;

    public string ListenerName { get; }

    public string ObserverName { get; }

    protected DiagnosticObserver(string name, string listenerName, ILoggerFactory loggerFactory)
    {
        ArgumentGuard.NotNullOrEmpty(name);
        ArgumentGuard.NotNullOrEmpty(listenerName);
        ArgumentGuard.NotNull(loggerFactory);

        ObserverName = name;
        ListenerName = listenerName;
        _logger = loggerFactory.CreateLogger<DiagnosticObserver>();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _subscription?.Dispose();
            _subscription = null;

            _logger.LogInformation("DiagnosticObserver {observer} Disposed", ObserverName);
        }
    }

    public void Subscribe(DiagnosticListener listener)
    {
        ArgumentGuard.NotNull(listener);

        if (ListenerName == listener.Name)
        {
            if (_subscription != null)
            {
                Dispose();
            }

            _subscription = listener.Subscribe(this);
            _logger.LogInformation("DiagnosticObserver {observer} Subscribed to {listener}", ObserverName, listener.Name);
        }
    }

    public virtual void OnCompleted()
    {
    }

    public virtual void OnError(Exception error)
    {
    }

    public virtual void OnNext(KeyValuePair<string, object> @event)
    {
        try
        {
            ProcessEvent(@event.Key, @event.Value);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "ProcessEvent exception: {Id}", @event.Key);
        }
    }

    public abstract void ProcessEvent(string eventName, object value);
}
