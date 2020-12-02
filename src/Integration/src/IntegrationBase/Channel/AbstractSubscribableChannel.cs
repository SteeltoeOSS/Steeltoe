// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Common.Contexts;
using Steeltoe.Integration.Dispatcher;
using Steeltoe.Messaging;
using System;
using System.Threading;

namespace Steeltoe.Integration.Channel
{
    public abstract class AbstractSubscribableChannel : AbstractMessageChannel, ISubscribableChannel
    {
        protected AbstractSubscribableChannel(IApplicationContext context, IMessageDispatcher dispatcher, ILogger logger = null)
            : this(context, dispatcher, null, logger)
        {
        }

        protected AbstractSubscribableChannel(IApplicationContext context, IMessageDispatcher dispatcher, string name, ILogger logger = null)
            : base(context, name, logger)
        {
            Dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public virtual int SubscriberCount
        {
            get { return Dispatcher.HandlerCount; }
        }

        public virtual int MaxSubscribers
        {
            get { return Dispatcher.MaxSubscribers; }
            set { Dispatcher.MaxSubscribers = value; }
        }

        public virtual bool Failover
        {
            get { return Dispatcher.Failover; }
            set { Dispatcher.Failover = value; }
        }

        public IMessageDispatcher Dispatcher { get; }

        public virtual bool Subscribe(IMessageHandler handler)
        {
            var added = Dispatcher.AddHandler(handler);
            if (added)
            {
                Logger?.LogTrace("Channel '" + ServiceName + "' has " + handler.ServiceName + " subscriber(s).");
                Logger?.LogInformation("Channel '" + ServiceName + "' has " + Dispatcher.HandlerCount + " subscriber(s).");
            }

            return added;
        }

        public virtual bool Unsubscribe(IMessageHandler handler)
        {
            var removed = Dispatcher.RemoveHandler(handler);
            if (removed)
            {
                Logger?.LogInformation("Channel '" + ServiceName + "' has " + Dispatcher.HandlerCount + " subscriber(s).");
            }

            return removed;
        }

        protected override bool DoSendInternal(IMessage message, CancellationToken cancellationToken)
        {
            try
            {
                return Dispatcher.Dispatch(message, cancellationToken);
            }
            catch (MessageDispatchingException e)
            {
                var description = e.Message + " for channel '" + ServiceName + "'.";
                throw new MessageDeliveryException(message, description, e);
            }
        }
    }
}
