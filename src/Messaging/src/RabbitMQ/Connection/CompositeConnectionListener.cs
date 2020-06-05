// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public class CompositeConnectionListener : IConnectionListener
    {
        private readonly object _lock = new object();

        private List<IConnectionListener> _connectionListeners = new List<IConnectionListener>();

        public void OnClose(IConnection connection)
        {
            foreach (var listener in _connectionListeners)
            {
                listener.OnClose(connection);
            }
        }

        public void OnCreate(IConnection connection)
        {
            foreach (var listener in _connectionListeners)
            {
                listener.OnCreate(connection);
            }
        }

        public void OnShutDown(ShutdownEventArgs args)
        {
            foreach (var listener in _connectionListeners)
            {
                listener.OnShutDown(args);
            }
        }

        public void SetListeners(List<IConnectionListener> channelListeners)
        {
            _connectionListeners = channelListeners;
        }

        public void AddListener(IConnectionListener channelListener)
        {
            lock (_lock)
            {
                var listeners = new List<IConnectionListener>(_connectionListeners)
                {
                    channelListener
                };
                _connectionListeners = listeners;
            }
        }

        public bool RemoveListener(IConnectionListener channelListener)
        {
            lock (_lock)
            {
                if (_connectionListeners.Contains(channelListener))
                {
                    var listeners = new List<IConnectionListener>(_connectionListeners);
                    listeners.Remove(channelListener);
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
}
