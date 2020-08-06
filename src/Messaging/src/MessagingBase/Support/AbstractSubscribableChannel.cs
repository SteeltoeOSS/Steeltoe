// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Steeltoe.Messaging.Support
{
    public abstract class AbstractSubscribableChannel : AbstractMessageChannel, ISubscribableChannel
    {
        internal HashSet<IMessageHandler> _handlers = new HashSet<IMessageHandler>();
        private object _lock = new object();

        public AbstractSubscribableChannel(ILogger logger = null)
            : base(logger)
        {
        }

        public virtual int SubscriberCount
        {
            get
            {
                return _handlers.Count;
            }
        }

        public virtual ISet<IMessageHandler> Subscribers
        {
            get
            {
                lock (_lock)
                {
                    return new HashSet<IMessageHandler>(_handlers);
                }
            }
        }

        public virtual bool HasSubscription(IMessageHandler handler)
        {
            lock (_lock)
            {
                return _handlers.Contains(handler);
            }
        }

        public virtual bool Subscribe(IMessageHandler handler)
        {
            lock (_lock)
            {
                var handlers = new HashSet<IMessageHandler>(_handlers);
                var result = handlers.Add(handler);
                if (result)
                {
                    Logger?.LogDebug("{serviceName} added to {handler} ", ServiceName, handler);
                    _handlers = handlers;
                }

                return result;
            }
        }

        public virtual bool Unsubscribe(IMessageHandler handler)
        {
            lock (_lock)
            {
                var handlers = new HashSet<IMessageHandler>(_handlers);
                var result = handlers.Remove(handler);
                if (result)
                {
                    Logger?.LogDebug("{serviceName} removed from {handler} ", ServiceName, handler);
                    _handlers = handlers;
                }

                return result;
            }
        }
    }
}
