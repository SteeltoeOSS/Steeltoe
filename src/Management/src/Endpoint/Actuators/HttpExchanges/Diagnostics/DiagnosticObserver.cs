// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Steeltoe.Management.Endpoint.Actuators.HttpExchanges.Diagnostics;

internal abstract class DiagnosticObserver : IObserver<KeyValuePair<string, object?>>, IDisposable
{
    private const string ListenerName = "Microsoft.AspNetCore";
    private const string ObserverName = "HttpExchangesDiagnosticObserver";
    private readonly ILogger _logger;
    private IDisposable? _subscription;

    protected DiagnosticObserver(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

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

            _logger.LogTrace("DiagnosticObserver {Observer} disposed", ObserverName);
        }
    }

    public void Subscribe(DiagnosticListener listener)
    {
        ArgumentNullException.ThrowIfNull(listener);

        if (listener.Name == ListenerName)
        {
            if (_subscription != null)
            {
                Dispose();
            }

            _subscription = listener.Subscribe(this);
            _logger.LogTrace("DiagnosticObserver {Observer} subscribed to {Listener}", ObserverName, listener.Name);
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
            _logger.LogError(exception, "Failed to process event {Id}", value.Key);
        }
    }

    public abstract void ProcessEvent(string eventName, object? value);
}
