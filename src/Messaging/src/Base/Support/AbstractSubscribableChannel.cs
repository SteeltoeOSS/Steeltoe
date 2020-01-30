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
                    Logger?.LogDebug(Name + " added " + handler);
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
                    Logger?.LogDebug(Name + " removed " + handler);
                    _handlers = handlers;
                }

                return result;
            }
        }
    }
}
