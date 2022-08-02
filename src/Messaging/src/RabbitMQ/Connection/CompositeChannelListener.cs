// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using RC = RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public class CompositeChannelListener : IChannelListener
{
    private readonly object _lock = new();
    private readonly ILogger _logger;

    private List<IChannelListener> _channelListeners = new();

    public CompositeChannelListener(ILogger logger = null)
    {
        _logger = logger;
    }

    public void OnCreate(RC.IModel channel, bool transactional)
    {
        _logger?.LogDebug("OnCreate");
        List<IChannelListener> listeners = _channelListeners;

        foreach (IChannelListener listener in listeners)
        {
            listener.OnCreate(channel, transactional);
        }
    }

    public void OnShutDown(RC.ShutdownEventArgs args)
    {
        _logger?.LogDebug("OnShutDown");
        List<IChannelListener> listeners = _channelListeners;

        foreach (IChannelListener listener in listeners)
        {
            listener.OnShutDown(args);
        }
    }

    public void SetListeners(List<IChannelListener> channelListeners)
    {
        _channelListeners = channelListeners;
    }

    public void AddListener(IChannelListener channelListener)
    {
        lock (_lock)
        {
            var listeners = new List<IChannelListener>(_channelListeners)
            {
                channelListener
            };

            _channelListeners = listeners;
        }
    }

    public bool RemoveListener(IChannelListener channelListener)
    {
        lock (_lock)
        {
            if (_channelListeners.Contains(channelListener))
            {
                var listeners = new List<IChannelListener>(_channelListeners);
                listeners.Remove(channelListener);
                _channelListeners = listeners;
                return true;
            }

            return false;
        }
    }

    public void ClearListeners()
    {
        _channelListeners = new List<IChannelListener>();
    }
}
