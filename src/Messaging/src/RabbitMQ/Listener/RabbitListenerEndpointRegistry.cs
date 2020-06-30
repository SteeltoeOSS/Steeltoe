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
using Steeltoe.Common.Contexts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Rabbit.Listener
{
#pragma warning disable S3881 // "IDisposable" should be implemented correctly
    public class RabbitListenerEndpointRegistry : IRabbitListenerEndpointRegistry
#pragma warning restore S3881 // "IDisposable" should be implemented correctly
    {
        public const string DEFAULT_SERVICE_NAME = nameof(RabbitListenerEndpointRegistry);

        private readonly ILogger _logger;
        private readonly ConcurrentDictionary<string, IMessageListenerContainer> _listenerContainers = new ConcurrentDictionary<string, IMessageListenerContainer>();
        private bool _isDisposed;

        public RabbitListenerEndpointRegistry(IApplicationContext applicationContext, ILogger logger = null)
        {
            _logger = logger;
            ApplicationContext = applicationContext;
        }

        public IApplicationContext ApplicationContext { get; set; }

        public int Phase { get; set; } = int.MaxValue;

        public bool IsAutoStartup => true;

        public bool IsRunning
        {
            get
            {
                foreach (var listenerContainer in GetListenerContainers())
                {
                    if (listenerContainer.IsRunning)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public string ServiceName { get; set; } = DEFAULT_SERVICE_NAME;

        public IMessageListenerContainer GetListenerContainer(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException(nameof(id));
            }

            _listenerContainers.TryGetValue(id, out var messageListenerContainer);
            return messageListenerContainer;
        }

        public ISet<string> GetListenerContainerIds()
        {
            return new HashSet<string>(_listenerContainers.Keys);
        }

        public ICollection<IMessageListenerContainer> GetListenerContainers()
        {
            return new List<IMessageListenerContainer>(_listenerContainers.Values);
        }

        public void RegisterListenerContainer(IRabbitListenerEndpoint endpoint, IRabbitListenerContainerFactory factory)
        {
            RegisterListenerContainer(endpoint, factory, false);
        }

        public void RegisterListenerContainer(IRabbitListenerEndpoint endpoint, IRabbitListenerContainerFactory factory, bool startImmediately)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var id = endpoint.Id;
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentException("Endpoint id must not be empty");
            }

            lock (_listenerContainers)
            {
                if (_listenerContainers.ContainsKey(id))
                {
                    throw new InvalidOperationException("Another endpoint is already registered with id '" + id + "'");
                }

                var container = CreateListenerContainer(endpoint, factory);
                _listenerContainers.TryAdd(id, container);

                if (!string.IsNullOrEmpty(endpoint.Group) && ApplicationContext != null)
                {
                    var containerCollection =
                        ApplicationContext.GetService<IMessageListenerContainerCollection>(endpoint.Group) as MessageListenerContainerCollection;
                    if (containerCollection != null)
                    {
                        containerCollection.AddContainer(container);
                    }
                }

                // if (this.contextRefreshed)
                // {
                //    container.lazyLoad();
                // }
                if (startImmediately && container.IsAutoStartup)
                {
                    container.Start();
                }
            }
        }

        public IMessageListenerContainer UnregisterListenerContainer(string id)
        {
            _listenerContainers.TryRemove(id, out var removed);
            return removed;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            foreach (var listenerContainer in _listenerContainers.Values)
            {
                if (listenerContainer is IDisposable)
                {
                    try
                    {
                        ((IDisposable)listenerContainer).Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning("Failed to destroy listener container [" + listenerContainer + "]", ex);
                    }
                }
            }
        }

        public async Task Stop(Action callback)
        {
            var containers = _listenerContainers.Values;
            if (containers.Count > 0)
            {
                var count = containers.Count;
                Action aggCallback = () =>
                {
                    var result = Interlocked.Decrement(ref count);
                    if (result == 0)
                    {
                        callback();
                    }
                };

                foreach (var listenerContainer in containers)
                {
                    try
                    {
                        await listenerContainer.Stop(aggCallback);
                    }
                    catch (Exception e)
                    {
                        _logger?.LogWarning("Failed to stop listener container [" + listenerContainer + "]", e);
                    }
                }
            }
            else
            {
                callback();
            }
        }

        public async Task Stop()
        {
            foreach (var listenerContainer in _listenerContainers.Values)
            {
                try
                {
                    await listenerContainer.Stop();
                }
                catch (Exception e)
                {
                    _logger?.LogWarning("Failed to stop listener container [" + listenerContainer + "]", e);
                }
            }
        }

        public async Task Start()
        {
            foreach (var listenerContainer in _listenerContainers.Values)
            {
                if (listenerContainer.IsAutoStartup)
                {
                    await listenerContainer.Start();
                }
            }
        }

        protected IMessageListenerContainer CreateListenerContainer(IRabbitListenerEndpoint endpoint, IRabbitListenerContainerFactory factory)
        {
            var listenerContainer = factory.CreateListenerContainer(endpoint);

            try
            {
                listenerContainer.Initialize();
            }
            catch (Exception ex)
            {
                throw new TypeInitializationException("Failed to initialize message listener container", ex);
            }

            var containerPhase = listenerContainer.Phase;
            if (containerPhase < int.MaxValue)
            {
                // a custom phase value
                if (Phase < int.MaxValue && Phase != containerPhase)
                {
                    throw new InvalidOperationException("Encountered phase mismatch between container factory definitions: " + Phase + " vs " + containerPhase);
                }

                Phase = listenerContainer.Phase;
            }

            return listenerContainer;
        }
    }
}
