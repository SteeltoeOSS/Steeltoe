// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging;
using System;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Endpoint;

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
            if (_handler is IMessageProducer producer)
            {
                return producer.OutputChannel;
            }
            else if (_handler is IMessageRouter router)
            {
                return router.DefaultOutputChannel;
            }
            else
            {
                return null;
            }
        }
    }

    protected override Task DoStart()
    {
        _inputChannel.Subscribe(_handler);
        if (_handler is ILifecycle lifecycle)
        {
            return lifecycle.Start();
        }

        return Task.CompletedTask;
    }

    protected override Task DoStop()
    {
        _inputChannel.Unsubscribe(_handler);
        if (_handler is ILifecycle lifecycle)
        {
            return lifecycle.Stop();
        }

        return Task.CompletedTask;
    }
}