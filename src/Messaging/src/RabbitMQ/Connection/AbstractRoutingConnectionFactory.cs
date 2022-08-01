// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public abstract class AbstractRoutingConnectionFactory : IConnectionFactory, IRoutingConnectionFactory
{
    private readonly ConcurrentDictionary<object, IConnectionFactory> _targetConnectionFactories = new ();

    private readonly List<IConnectionListener> _connectionListeners = new ();

    public IConnectionFactory DefaultTargetConnectionFactory { get; set; }

    public bool LenientFallback { get; set; }

    public string Host
    {
        get
        {
            return DetermineTargetConnectionFactory().Host;
        }
    }

    public int Port
    {
        get
        {
            return DetermineTargetConnectionFactory().Port;
        }
    }

    public string VirtualHost
    {
        get
        {
            return DetermineTargetConnectionFactory().VirtualHost;
        }
    }

    public string Username
    {
        get
        {
            return DetermineTargetConnectionFactory().Username;
        }
    }

    public IConnectionFactory PublisherConnectionFactory => null;

    public bool IsSimplePublisherConfirms => false;

    public bool IsPublisherConfirms => false;

    public bool IsPublisherReturns => false;

    public abstract string ServiceName { get; set; }

    public virtual IConnectionFactory GetTargetConnectionFactory(object key)
    {
        _targetConnectionFactories.TryGetValue(key, out var result);
        return result;
    }

    public virtual void SetTargetConnectionFactories(Dictionary<object, IConnectionFactory> targetConnectionFactories)
    {
        if (targetConnectionFactories == null)
        {
            throw new ArgumentNullException(nameof(targetConnectionFactories));
        }

        foreach (var factory in targetConnectionFactories.Values)
        {
            if (factory == null)
            {
                throw new ArgumentException("'targetConnectionFactories' cannot have null values.");
            }

            foreach (var kvp in targetConnectionFactories)
            {
                _targetConnectionFactories[kvp.Key] = kvp.Value;
            }
        }
    }

    public virtual IConnection CreateConnection()
    {
        return DetermineTargetConnectionFactory().CreateConnection();
    }

    public virtual void AddConnectionListener(IConnectionListener listener)
    {
        foreach (var connectionFactory in _targetConnectionFactories.Values)
        {
            connectionFactory.AddConnectionListener(listener);
        }

        if (DefaultTargetConnectionFactory != null)
        {
            DefaultTargetConnectionFactory.AddConnectionListener(listener);
        }

        _connectionListeners.Add(listener);
    }

    public virtual bool RemoveConnectionListener(IConnectionListener listener)
    {
        var removed = false;
        foreach (var connectionFactory in _targetConnectionFactories.Values)
        {
            var listenerRemoved = connectionFactory.RemoveConnectionListener(listener);
            if (!removed)
            {
                removed = listenerRemoved;
            }
        }

        if (DefaultTargetConnectionFactory != null)
        {
            var listenerRemoved = DefaultTargetConnectionFactory.RemoveConnectionListener(listener);
            if (!removed)
            {
                removed = listenerRemoved;
            }
        }

        _connectionListeners.Remove(listener);
        return removed;
    }

    public virtual void ClearConnectionListeners()
    {
        foreach (var connectionFactory in _targetConnectionFactories.Values)
        {
            connectionFactory.ClearConnectionListeners();
        }

        if (DefaultTargetConnectionFactory != null)
        {
            DefaultTargetConnectionFactory.ClearConnectionListeners();
        }

        _connectionListeners.Clear();
    }

    public virtual void Destroy()
    {
        // Do nothing
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    public virtual void AddTargetConnectionFactory(object key, IConnectionFactory connectionFactory)
    {
        _targetConnectionFactories[key] = connectionFactory;

        foreach (var listener in _connectionListeners)
        {
            connectionFactory.AddConnectionListener(listener);
        }
    }

    public virtual IConnectionFactory DetermineTargetConnectionFactory()
    {
        var lookupKey = DetermineCurrentLookupKey();
        IConnectionFactory connectionFactory = null;
        if (lookupKey != null)
        {
            _targetConnectionFactories.TryGetValue(lookupKey, out connectionFactory);
        }

        if (connectionFactory == null && (LenientFallback || lookupKey == null))
        {
            connectionFactory = DefaultTargetConnectionFactory;
        }

        if (connectionFactory == null)
        {
            throw new InvalidOperationException($"Cannot determine target ConnectionFactory for lookup key [{lookupKey}]");
        }

        return connectionFactory;
    }

    public virtual IConnectionFactory RemoveTargetConnectionFactory(object key)
    {
        _targetConnectionFactories.TryRemove(key, out var connectionFactory);
        return connectionFactory;
    }

    public abstract object DetermineCurrentLookupKey();
}
