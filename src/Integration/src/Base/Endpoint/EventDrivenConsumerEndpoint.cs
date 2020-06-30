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

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Endpoint
{
    public class EventDrivenConsumerEndpoint : AbstractEndpoint
    {
        private readonly ISubscribableChannel _inputChannel;

        private readonly IMessageHandler _handler;

        public EventDrivenConsumerEndpoint(IApplicationContext context, ISubscribableChannel inputChannel, IMessageHandler handler)
            : base(context)
        {
            _handler = handler ?? throw new ArgumentNullException(nameof(handler));
            _inputChannel = inputChannel ?? throw new ArgumentNullException(nameof(inputChannel));
            Phase = int.MaxValue;
        }

        public virtual IMessageChannel InputChannel => _inputChannel;

        public virtual IMessageHandler Handler => _handler;

        public virtual IMessageChannel OutputChannel
        {
            get
            {
                if (_handler is IMessageProducer)
                {
                    return ((IMessageProducer)_handler).OutputChannel;
                }
                else if (_handler is IMessageRouter)
                {
                    return ((IMessageRouter)_handler).DefaultOutputChannel;
                }
                else
                {
                    return null;
                }
            }
        }

        protected override async Task DoStart()
        {
            _inputChannel.Subscribe(_handler);
            if (_handler is ILifecycle)
            {
                await ((ILifecycle)_handler).Start();
            }
        }

        protected override async Task DoStop()
        {
            _inputChannel.Unsubscribe(_handler);
            if (_handler is ILifecycle)
            {
                await ((ILifecycle)_handler).Stop();
            }
        }
    }
}
