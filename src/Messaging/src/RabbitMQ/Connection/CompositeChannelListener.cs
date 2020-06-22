﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public class CompositeChannelListener : IChannelListener
    {
        private readonly object _lock = new object();

        private List<IChannelListener> _channelListeners = new List<IChannelListener>();

        public void OnCreate(IModel channel, bool transactional)
        {
            var listeners = _channelListeners;
            foreach (var listener in listeners)
            {
                listener.OnCreate(channel, transactional);
            }
        }

        public void OnShutDown(ShutdownEventArgs @event)
        {
            var listeners = _channelListeners;
            foreach (var listener in listeners)
            {
                listener.OnShutDown(@event);
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
}
