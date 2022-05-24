// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Order;
using Steeltoe.Common.Util;
using Steeltoe.Integration.Channel;
using Steeltoe.Integration.Support;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Dispatcher
{
    public abstract class AbstractDispatcher : IMessageDispatcher
    {
        protected readonly IApplicationContext _context;
        protected readonly ILogger _logger;
        protected readonly TaskScheduler _executor;
        protected readonly TaskFactory _factory;
        protected List<IMessageHandler> _handlers = new ();

        private readonly object _lock = new ();
        private readonly MessageHandlerComparer _comparer = new ();
        private IErrorHandler _errorHandler;
        private volatile IMessageHandler _theOneHandler;
        private IIntegrationServices _integrationServices;

        protected AbstractDispatcher(IApplicationContext context, TaskScheduler executor, ILogger logger = null)
        {
            _context = context;
            _logger = logger;
            _executor = executor;
            if (executor != null)
            {
                _factory = new TaskFactory(executor);
            }
        }

        public virtual int MaxSubscribers { get; set; } = int.MaxValue;

        public virtual int HandlerCount => _handlers.Count;

        public virtual ILoadBalancingStrategy LoadBalancingStrategy { get; set; }

        public virtual bool Failover { get; set; } = true;

        public virtual IMessageHandlingDecorator MessageHandlingDecorator { get; set; }

        public virtual IIntegrationServices IntegrationServices
        {
            get
            {
                _integrationServices ??= IntegrationServicesUtils.GetIntegrationServices(_context);
                return _integrationServices;
            }
        }

        public virtual IErrorHandler ErrorHandler
        {
            get
            {
                if (_factory != null && _errorHandler == null)
                {
                    _errorHandler = new MessagePublishingErrorHandler(_context);
                }

                return _errorHandler;
            }

            set
            {
                _errorHandler = value;
            }
        }

        public virtual bool AddHandler(IMessageHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            lock (_lock)
            {
                if (_handlers.Count == MaxSubscribers)
                {
                    throw new ArgumentException("Maximum subscribers exceeded");
                }

                var handlers = new List<IMessageHandler>(_handlers);
                if (handlers.Contains(handler))
                {
                    return false;
                }

                handlers.Add(handler);
                handlers.Sort(_comparer);
                _theOneHandler = handlers.Count == 1 ? handler : null;

                _handlers = handlers;
            }

            return true;
        }

        public virtual bool RemoveHandler(IMessageHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            lock (_lock)
            {
                var handlers = new List<IMessageHandler>(_handlers);
                var removed = handlers.Remove(handler);
                if (handlers.Count == 1)
                {
                    _theOneHandler = handlers[0];
                }
                else
                {
                    handlers.Sort(_comparer);
                    _theOneHandler = null;
                }

                _handlers = handlers;

                return removed;
            }
        }

        public override string ToString()
        {
            return $"{GetType().Name} with handlers: {_handlers.Count}";
        }

        public virtual bool Dispatch(IMessage message, CancellationToken cancellationToken = default)
        {
            return DoDispatch(message, cancellationToken);
        }

        protected abstract bool DoDispatch(IMessage message, CancellationToken cancellationToken);

        protected virtual bool TryOptimizedDispatch(IMessage message)
        {
            var handler = _theOneHandler;
            if (handler != null)
            {
                try
                {
                    handler.HandleMessage(message);
                    return true;
                }
                catch (Exception e)
                {
                    var wrapped = IntegrationUtils.WrapInDeliveryExceptionIfNecessary(message, "Dispatcher failed to deliver Message", e);
                    if (wrapped != e)
                    {
                        throw wrapped;
                    }

                    throw;
                }
            }

            return false;
        }

        internal List<IMessageHandler> Handlers => new (_handlers);

        private sealed class MessageHandlerComparer : OrderComparer, IComparer<IMessageHandler>
        {
            public int Compare(IMessageHandler x, IMessageHandler y)
            {
                var xo = x as IOrdered;
                var yo = y as IOrdered;
                if (xo != null && yo != null)
                {
                    return Compare(xo, yo);
                }

                if (xo != null)
                {
                    return GetOrder(xo.Order, AbstractOrdered.LOWEST_PRECEDENCE);
                }

                if (yo != null)
                {
                    return GetOrder(AbstractOrdered.LOWEST_PRECEDENCE, yo.Order);
                }

                return 0;
            }
        }
    }
}
