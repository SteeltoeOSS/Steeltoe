// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public class CompositeConnectionListener : IConnectionListener
    {
        private readonly object _lock = new object();
        private readonly ILogger _logger;

        private List<IConnectionListener> _connectionListeners = new List<IConnectionListener>();

        public CompositeConnectionListener(ILogger logger = null)
        {
            _logger = logger;
        }

        public void OnClose(IConnection connection)
        {
            _logger?.LogDebug("OnClose");
            var listeners = _connectionListeners;
            foreach (var listener in listeners)
            {
                listener.OnClose(connection);
            }
        }

        public void OnCreate(IConnection connection)
        {
            _logger?.LogDebug("OnCreate");
            var listeners = _connectionListeners;
            foreach (var listener in listeners)
            {
                listener.OnCreate(connection);
            }
        }

        public void OnShutDown(ShutdownEventArgs args)
        {
            _logger?.LogDebug("OnShutDown");
            var listeners = _connectionListeners;
            foreach (var listener in listeners)
            {
                listener.OnShutDown(args);
            }
        }

        public void SetListeners(List<IConnectionListener> connectionListeners)
        {
            _connectionListeners = connectionListeners;
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
}
