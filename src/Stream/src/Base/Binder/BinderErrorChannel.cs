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

using Steeltoe.Integration.Channel;
using Steeltoe.Messaging;
using System;
using System.Threading;

namespace Steeltoe.Stream.Binder
{
    internal class BinderErrorChannel : PublishSubscribeChannel, ILastSubscriberAwareChannel
    {
        private int _subscribers;

        private volatile ILastSubscriberMessageHandler _finalHandler;

        public BinderErrorChannel(IServiceProvider serviceProvider, string name)
            : base(serviceProvider, name)
        {
        }

        public override bool Subscribe(IMessageHandler handler)
        {
            Interlocked.Increment(ref _subscribers);
            if (handler is ILastSubscriberMessageHandler && _finalHandler != null)
            {
                throw new InvalidOperationException("Only one LastSubscriberMessageHandler is allowed");
            }

            if (_finalHandler != null)
            {
                base.Unsubscribe(_finalHandler);
            }

            var result = base.Subscribe(handler);
            if (_finalHandler != null)
            {
                base.Subscribe(_finalHandler);
            }

            if (handler is ILastSubscriberMessageHandler && _finalHandler == null)
            {
                _finalHandler = (ILastSubscriberMessageHandler)handler;
            }

            return result;
        }

        public override bool Unsubscribe(IMessageHandler handler)
        {
            Interlocked.Decrement(ref _subscribers);
            return base.Unsubscribe(handler);
        }

        public int Subscribers
        {
            get { return _subscribers; }
        }
    }
}
