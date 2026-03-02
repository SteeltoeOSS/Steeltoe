// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges.Diagnostics;

internal abstract partial class DiagnosticObserver : IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private readonly string _observerName;
    private readonly string _listenerName;
    private readonly ILogger _logger;
    private IDisposable? _subscription;

    protected DiagnosticObserver(string name, string listenerName, ILoggerFactory loggerFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(listenerName);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _observerName = name;
        _listenerName = listenerName;
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

            LogObserverDisposed(_observerName);
        }
    }

    public void Subscribe(DiagnosticListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        if (_listenerName == listener.Name)
        {
            if (_subscription != null)
            {
                Dispose();
            }

            _subscription = listener.Subscribe(this);
            LogObserverSubscribed(_observerName, listener.Name);
        }
    }

    public virtual void OnCompleted()
    {
    }

#pragma warning disable CA1716 // Identifiers should not match keywords
    public virtual void OnError(Exception error)
#pragma warning restore CA1716 // Identifiers should not match keywords
    {
    }

    public virtual void OnNext(KeyValuePair<string, object?> value)
    {
        try
        {
            ProcessEvent(value.Key, value.Value);
        }
        catch (Exception exception)
        {
            LogFailedToProcessEvent(exception, value.Key);
        }
    }

    public abstract void ProcessEvent(string eventName, object? value);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Diagnostic observer {Observer} disposed.")]
    private partial void LogObserverDisposed(string observer);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Diagnostic observer {Observer} subscribed to {Listener}.")]
    private partial void LogObserverSubscribed(string observer, string listener);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to process event {Id}.")]
    private partial void LogFailedToProcessEvent(Exception exception, string id);
}
