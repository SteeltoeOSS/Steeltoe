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

namespace Steeltoe.Integration.Dispatcher;

public abstract class AbstractDispatcher : IMessageDispatcher
{
    protected readonly IApplicationContext Context;
    protected readonly ILogger InnerLogger;
    protected readonly TaskScheduler Executor;
    protected readonly TaskFactory Factory;
    protected List<IMessageHandler> handlers = new ();

    private readonly object _lock = new ();
    private readonly MessageHandlerComparer _comparer = new ();
    private IErrorHandler _errorHandler;
    private volatile IMessageHandler _theOneHandler;
    private IIntegrationServices _integrationServices;

    protected AbstractDispatcher(IApplicationContext context, TaskScheduler executor, ILogger logger = null)
    {
        this.Context = context;
        this.InnerLogger = logger;
        this.Executor = executor;
        if (executor != null)
        {
            Factory = new TaskFactory(executor);
        }
    }

    public virtual int MaxSubscribers { get; set; } = int.MaxValue;

    public virtual int HandlerCount => handlers.Count;

    public virtual ILoadBalancingStrategy LoadBalancingStrategy { get; set; }

    public virtual bool Failover { get; set; } = true;

    public virtual IMessageHandlingDecorator MessageHandlingDecorator { get; set; }

    public virtual IIntegrationServices IntegrationServices
    {
        get
        {
            _integrationServices ??= IntegrationServicesUtils.GetIntegrationServices(Context);
            return _integrationServices;
        }
    }

    public virtual IErrorHandler ErrorHandler
    {
        get
        {
            if (Factory != null && _errorHandler == null)
            {
                _errorHandler = new MessagePublishingErrorHandler(Context);
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
            if (this.handlers.Count == MaxSubscribers)
            {
                throw new ArgumentException("Maximum subscribers exceeded");
            }

            var newHandlers = new List<IMessageHandler>(this.handlers);
            if (newHandlers.Contains(handler))
            {
                return false;
            }

            newHandlers.Add(handler);
            newHandlers.Sort(_comparer);
            _theOneHandler = newHandlers.Count == 1 ? handler : null;

            this.handlers = newHandlers;
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
            var newHandlers = new List<IMessageHandler>(this.handlers);
            var removed = newHandlers.Remove(handler);
            if (newHandlers.Count == 1)
            {
                _theOneHandler = newHandlers[0];
            }
            else
            {
                newHandlers.Sort(_comparer);
                _theOneHandler = null;
            }

            this.handlers = newHandlers;

            return removed;
        }
    }

    public override string ToString()
    {
        return $"{GetType().Name} with handlers: {handlers.Count}";
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

    internal List<IMessageHandler> Handlers => new (handlers);

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
                return GetOrder(xo.Order, AbstractOrdered.LowestPrecedence);
            }

            if (yo != null)
            {
                return GetOrder(AbstractOrdered.LowestPrecedence, yo.Order);
            }

            return 0;
        }
    }
}
