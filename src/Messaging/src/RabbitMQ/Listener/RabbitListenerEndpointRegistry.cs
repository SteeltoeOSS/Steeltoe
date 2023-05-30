// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Steeltoe.Common;
using Steeltoe.Common.Contexts;

namespace Steeltoe.Messaging.RabbitMQ.Listener;

public class RabbitListenerEndpointRegistry : IRabbitListenerEndpointRegistry
{
    public const string DefaultServiceName = nameof(RabbitListenerEndpointRegistry);

    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, IMessageListenerContainer> _listenerContainers = new();
    private bool _isDisposed;

    public IApplicationContext ApplicationContext { get; set; }

    public int Phase { get; set; } = int.MaxValue;

    public bool IsAutoStartup => true;

    public bool IsRunning => GetListenerContainers().Any(listener => listener.IsRunning);

    public string ServiceName { get; set; } = DefaultServiceName;

    public RabbitListenerEndpointRegistry(IApplicationContext applicationContext, ILogger logger = null)
    {
        _logger = logger;
        ApplicationContext = applicationContext;
    }

    public IMessageListenerContainer GetListenerContainer(string id)
    {
        ArgumentGuard.NotNullOrEmpty(id);

        _listenerContainers.TryGetValue(id, out IMessageListenerContainer messageListenerContainer);
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

    public void RegisterListenerContainer(IRabbitListenerEndpoint endpointHandler, IRabbitListenerContainerFactory factory)
    {
        RegisterListenerContainer(endpointHandler, factory, false);
    }

    public void RegisterListenerContainer(IRabbitListenerEndpoint endpointHandler, IRabbitListenerContainerFactory factory, bool startImmediately)
    {
        ArgumentGuard.NotNull(endpointHandler);
        ArgumentGuard.NotNull(factory);

        if (string.IsNullOrEmpty(endpointHandler.Id))
        {
            throw new ArgumentException($"{nameof(endpointHandler.Id)} in {nameof(endpointHandler)} must not be null or empty.", nameof(endpointHandler));
        }

        lock (_listenerContainers)
        {
            if (_listenerContainers.ContainsKey(endpointHandler.Id))
            {
                throw new InvalidOperationException($"Another endpointHandler is already registered with id '{endpointHandler.Id}'");
            }

            IMessageListenerContainer container = CreateListenerContainer(endpointHandler, factory);
            _listenerContainers.TryAdd(endpointHandler.Id, container);

            if (!string.IsNullOrEmpty(endpointHandler.Group) && ApplicationContext != null &&
                ApplicationContext.GetService<IMessageListenerContainerCollection>(endpointHandler.Group) is MessageListenerContainerCollection
                    containerCollection)
            {
                containerCollection.AddContainer(container);
            }

            if (startImmediately && container.IsAutoStartup)
            {
                container.StartAsync();
            }
        }
    }

    public IMessageListenerContainer UnregisterListenerContainer(string id)
    {
        _listenerContainers.TryRemove(id, out IMessageListenerContainer removed);
        return removed;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && !_isDisposed)
        {
            foreach (IMessageListenerContainer listenerContainer in _listenerContainers.Values)
            {
                if (listenerContainer is IDisposable disposable)
                {
                    try
                    {
                        disposable.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogWarning(ex, "Failed to destroy listener container [{container}]", listenerContainer);
                    }
                }
            }

            _isDisposed = true;
        }
    }

    public async Task StopAsync(Action callback)
    {
        ICollection<IMessageListenerContainer> containers = _listenerContainers.Values;

        if (containers.Count > 0)
        {
            int count = containers.Count;

            Action aggCallback = () =>
            {
                int result = Interlocked.Decrement(ref count);

                if (result == 0)
                {
                    callback();
                }
            };

            foreach (IMessageListenerContainer listenerContainer in containers)
            {
                try
                {
                    await listenerContainer.StopAsync(aggCallback);
                }
                catch (Exception e)
                {
                    _logger?.LogWarning(e, "Failed to stop listener container [{container}]", listenerContainer);
                }
            }
        }
        else
        {
            callback();
        }
    }

    public async Task StopAsync()
    {
        foreach (IMessageListenerContainer listenerContainer in _listenerContainers.Values)
        {
            try
            {
                await listenerContainer.StopAsync();
            }
            catch (Exception e)
            {
                _logger?.LogWarning(e, "Failed to stop listener container [{container}]", listenerContainer);
            }
        }
    }

    public async Task StartAsync()
    {
        foreach (IMessageListenerContainer listenerContainer in _listenerContainers.Values)
        {
            if (listenerContainer.IsAutoStartup)
            {
                await listenerContainer.StartAsync();
            }
        }
    }

    protected IMessageListenerContainer CreateListenerContainer(IRabbitListenerEndpoint endpointHandler, IRabbitListenerContainerFactory factory)
    {
        IMessageListenerContainer listenerContainer = factory.CreateListenerContainer(endpointHandler);

        try
        {
            listenerContainer.Initialize();
        }
        catch (Exception ex)
        {
            throw new TypeInitializationException("Failed to initialize message listener container", ex);
        }

        int containerPhase = listenerContainer.Phase;

        if (containerPhase < int.MaxValue)
        {
            // a custom phase value
            if (Phase < int.MaxValue && Phase != containerPhase)
            {
                throw new InvalidOperationException($"Encountered phase mismatch between container factory definitions: {Phase} vs {containerPhase}");
            }

            Phase = listenerContainer.Phase;
        }

        return listenerContainer;
    }
}
