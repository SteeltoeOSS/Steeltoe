// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public abstract class AbstractRoutingConnectionFactory : IConnectionFactory, IRoutingConnectionFactory
{
    private readonly ConcurrentDictionary<object, IConnectionFactory> _targetConnectionFactories = new();

    private readonly List<IConnectionListener> _connectionListeners = new();

    public IConnectionFactory DefaultTargetConnectionFactory { get; set; }

    public bool LenientFallback { get; set; }

    public string Host => DetermineTargetConnectionFactory().Host;

    public int Port => DetermineTargetConnectionFactory().Port;

    public string VirtualHost => DetermineTargetConnectionFactory().VirtualHost;

    public string Username => DetermineTargetConnectionFactory().Username;

    public IConnectionFactory PublisherConnectionFactory => null;

    public bool IsSimplePublisherConfirms => false;

    public bool IsPublisherConfirms => false;

    public bool IsPublisherReturns => false;

    public abstract string ServiceName { get; set; }

    public virtual IConnectionFactory GetTargetConnectionFactory(object key)
    {
        _targetConnectionFactories.TryGetValue(key, out IConnectionFactory result);
        return result;
    }

    public virtual void SetTargetConnectionFactories(Dictionary<object, IConnectionFactory> targetConnectionFactories)
    {
        if (targetConnectionFactories == null)
        {
            throw new ArgumentNullException(nameof(targetConnectionFactories));
        }

        foreach (IConnectionFactory factory in targetConnectionFactories.Values)
        {
            if (factory == null)
            {
                throw new ArgumentException("'targetConnectionFactories' cannot have null values.");
            }

            foreach (KeyValuePair<object, IConnectionFactory> kvp in targetConnectionFactories)
            {
                _targetConnectionFactories[kvp.Key] = kvp.Value;
            }
        }
    }

    public virtual IConnection CreateConnection()
    {
        return DetermineTargetConnectionFactory().CreateConnection();
    }

    public virtual void AddConnectionListener(IConnectionListener connectionListener)
    {
        foreach (IConnectionFactory connectionFactory in _targetConnectionFactories.Values)
        {
            connectionFactory.AddConnectionListener(connectionListener);
        }

        if (DefaultTargetConnectionFactory != null)
        {
            DefaultTargetConnectionFactory.AddConnectionListener(connectionListener);
        }

        _connectionListeners.Add(connectionListener);
    }

    public virtual bool RemoveConnectionListener(IConnectionListener connectionListener)
    {
        bool removed = false;

        foreach (IConnectionFactory connectionFactory in _targetConnectionFactories.Values)
        {
            bool listenerRemoved = connectionFactory.RemoveConnectionListener(connectionListener);

            if (!removed)
            {
                removed = listenerRemoved;
            }
        }

        if (DefaultTargetConnectionFactory != null)
        {
            bool listenerRemoved = DefaultTargetConnectionFactory.RemoveConnectionListener(connectionListener);

            if (!removed)
            {
                removed = listenerRemoved;
            }
        }

        _connectionListeners.Remove(connectionListener);
        return removed;
    }

    public virtual void ClearConnectionListeners()
    {
        foreach (IConnectionFactory connectionFactory in _targetConnectionFactories.Values)
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

        foreach (IConnectionListener listener in _connectionListeners)
        {
            connectionFactory.AddConnectionListener(listener);
        }
    }

    public virtual IConnectionFactory DetermineTargetConnectionFactory()
    {
        object lookupKey = DetermineCurrentLookupKey();
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
        _targetConnectionFactories.TryRemove(key, out IConnectionFactory connectionFactory);
        return connectionFactory;
    }

    public abstract object DetermineCurrentLookupKey();
}
