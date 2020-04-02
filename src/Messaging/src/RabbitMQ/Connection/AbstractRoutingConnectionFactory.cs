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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Rabbit.Connection
{
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public abstract class AbstractRoutingConnectionFactory : IConnectionFactory, IRoutingConnectionFactory
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        private readonly ConcurrentDictionary<object, IConnectionFactory> _targetConnectionFactories = new ConcurrentDictionary<object, IConnectionFactory>();

        private readonly List<IConnectionListener> _connectionListeners = new List<IConnectionListener>();

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

        public IConnectionFactory GetTargetConnectionFactory(object key)
        {
            _targetConnectionFactories.TryGetValue(key, out var result);
            return result;
        }

        public void SetTargetConnectionFactories(Dictionary<object, IConnectionFactory> targetConnectionFactories)
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

        public IConnection CreateConnection()
        {
            return DetermineTargetConnectionFactory().CreateConnection();
        }

        public void AddConnectionListener(IConnectionListener listener)
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

        public bool RemoveConnectionListener(IConnectionListener listener)
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

        public void ClearConnectionListeners()
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

        public void Destroy()
        {
            // Do nothing
        }

        public void Dispose()
        {
            // Do nothing
        }

        protected void AddTargetConnectionFactory(object key, IConnectionFactory connectionFactory)
        {
            _targetConnectionFactories[key] = connectionFactory;

            foreach (var listener in _connectionListeners)
            {
                connectionFactory.AddConnectionListener(listener);
            }
        }

        protected IConnectionFactory DetermineTargetConnectionFactory()
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
                throw new InvalidOperationException("Cannot determine target ConnectionFactory for lookup key [" + lookupKey + "]");
            }

            return connectionFactory;
        }

        protected IConnectionFactory RemoveTargetConnectionFactory(object key)
        {
            _targetConnectionFactories.TryRemove(key, out var connectionFactory);
            return connectionFactory;
        }

        protected abstract object DetermineCurrentLookupKey();
    }
}
