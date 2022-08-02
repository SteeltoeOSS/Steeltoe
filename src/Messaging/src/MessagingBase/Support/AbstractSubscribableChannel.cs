// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Messaging.Support;

public abstract class AbstractSubscribableChannel : AbstractMessageChannel, ISubscribableChannel
{
    private readonly object _lock = new();
    internal HashSet<IMessageHandler> Handlers = new();

    public virtual int SubscriberCount => Handlers.Count;

    public virtual ISet<IMessageHandler> Subscribers
    {
        get
        {
            lock (_lock)
            {
                return new HashSet<IMessageHandler>(Handlers);
            }
        }
    }

    protected AbstractSubscribableChannel(ILogger logger = null)
        : base(logger)
    {
    }

    public virtual bool HasSubscription(IMessageHandler handler)
    {
        lock (_lock)
        {
            return Handlers.Contains(handler);
        }
    }

    public virtual bool Subscribe(IMessageHandler handler)
    {
        lock (_lock)
        {
            var handlers = new HashSet<IMessageHandler>(Handlers);
            bool result = handlers.Add(handler);

            if (result)
            {
                Logger?.LogDebug("{serviceName} added to {handler} ", ServiceName, handler);
                Handlers = handlers;
            }

            return result;
        }
    }

    public virtual bool Unsubscribe(IMessageHandler handler)
    {
        lock (_lock)
        {
            var handlers = new HashSet<IMessageHandler>(Handlers);
            bool result = handlers.Remove(handler);

            if (result)
            {
                Logger?.LogDebug("{serviceName} removed from {handler} ", ServiceName, handler);
                Handlers = handlers;
            }

            return result;
        }
    }
}
