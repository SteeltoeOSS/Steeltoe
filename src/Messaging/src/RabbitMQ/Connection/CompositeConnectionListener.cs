// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public class CompositeConnectionListener : IConnectionListener
{
    private readonly object _lock = new();
    private readonly ILogger _logger;

    private List<IConnectionListener> _connectionListeners = new();

    public CompositeConnectionListener(ILogger logger = null)
    {
        _logger = logger;
    }

    public void OnClose(IConnection connection)
    {
        _logger?.LogDebug("OnClose");
        List<IConnectionListener> listeners = _connectionListeners;

        foreach (IConnectionListener listener in listeners)
        {
            listener.OnClose(connection);
        }
    }

    public void OnCreate(IConnection connection)
    {
        _logger?.LogDebug("OnCreate");
        List<IConnectionListener> listeners = _connectionListeners;

        foreach (IConnectionListener listener in listeners)
        {
            listener.OnCreate(connection);
        }
    }

    public void OnShutDown(RC.ShutdownEventArgs args)
    {
        _logger?.LogDebug("OnShutDown");
        List<IConnectionListener> listeners = _connectionListeners;

        foreach (IConnectionListener listener in listeners)
        {
            listener.OnShutDown(args);
        }
    }

    public void SetListeners(IEnumerable<IConnectionListener> connectionListeners)
    {
        _connectionListeners = connectionListeners.ToList();
    }

    public void AddListener(IConnectionListener connectionListener)
    {
        lock (_lock)
        {
            var listeners = new List<IConnectionListener>(_connectionListeners)
            {
                connectionListener
            };

            _connectionListeners = listeners;
        }
    }

    public bool RemoveListener(IConnectionListener connectionListener)
    {
        lock (_lock)
        {
            if (_connectionListeners.Contains(connectionListener))
            {
                var listeners = new List<IConnectionListener>(_connectionListeners);
                listeners.Remove(connectionListener);
                _connectionListeners = listeners;
                return true;
            }

            return false;
        }
    }

    public void ClearListeners()
    {
        _connectionListeners = new List<IConnectionListener>();
    }
}
