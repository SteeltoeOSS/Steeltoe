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

using Steeltoe.Common.Order;
using System;

namespace Steeltoe.Messaging.Support
{
    public abstract class AbstractChannelInterceptor : AbstractOrdered, IChannelInterceptor
    {
        public AbstractChannelInterceptor()
        {
        }

        public AbstractChannelInterceptor(int order)
            : base(order)
        {
        }

        public virtual void AfterReceiveCompletion(IMessage message, IMessageChannel channel, Exception exception)
        {
        }

        public virtual void AfterSendCompletion(IMessage message, IMessageChannel channel, bool sent, Exception exception)
        {
        }

        public virtual IMessage PostReceive(IMessage message, IMessageChannel channel)
        {
            return message;
        }

        public virtual void PostSend(IMessage message, IMessageChannel channel, bool sent)
        {
        }

        public virtual bool PreReceive(IMessageChannel channel)
        {
            return true;
        }

        public virtual IMessage PreSend(IMessage message, IMessageChannel channel)
        {
            return message;
        }
    }
}
