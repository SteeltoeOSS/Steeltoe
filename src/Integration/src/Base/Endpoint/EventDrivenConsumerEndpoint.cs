// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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

        public EventDrivenConsumerEndpoint(IServiceProvider serviceProvider, ISubscribableChannel inputChannel, IMessageHandler handler)
            : base(serviceProvider)
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
