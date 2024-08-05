// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Diagnostics;

public abstract class DiagnosticObserver : IDiagnosticObserver
{
    private readonly ILogger _logger;
    private IDisposable? _subscription;

    public string ListenerName { get; }
    public string ObserverName { get; }

    protected DiagnosticObserver(string name, string listenerName, ILoggerFactory loggerFactory)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(listenerName);
        ArgumentNullException.ThrowIfNull(loggerFactory);

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

            _logger.LogInformation("DiagnosticObserver {Observer} Disposed", ObserverName);
        }
    }

    public void Subscribe(DiagnosticListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        if (ListenerName == listener.Name)
        {
            if (_subscription != null)
            {
                Dispose();
            }

            _subscription = listener.Subscribe(this);
            _logger.LogInformation("DiagnosticObserver {Observer} Subscribed to {Listener}", ObserverName, listener.Name);
        }
    }

    public virtual void OnCompleted()
    {
    }

    public virtual void OnError(Exception error)
    {
    }

    public virtual void OnNext(KeyValuePair<string, object?> @event)
    {
        try
        {
            ProcessEvent(@event.Key, @event.Value);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to process event {Id}", @event.Key);
        }
    }

    public abstract void ProcessEvent(string eventName, object? value);

    private protected static T? GetPropertyOrDefault<T>(object instance, string name)
    {
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        PropertyInfo? property = instance.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public);

        if (property == null)
        {
            return default;
        }

        return (T?)property.GetValue(instance);
    }
}
