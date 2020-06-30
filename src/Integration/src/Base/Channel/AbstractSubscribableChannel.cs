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
