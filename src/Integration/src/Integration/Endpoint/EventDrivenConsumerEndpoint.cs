// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common;
using Steeltoe.Common.Contexts;
using Steeltoe.Common.Lifecycle;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Endpoint;

public class EventDrivenConsumerEndpoint : AbstractEndpoint
{
    private readonly ISubscribableChannel _inputChannel;

    private readonly IMessageHandler _handler;

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

            if (_handler is IMessageRouter router)
            {
                return router.DefaultOutputChannel;
            }

            return null;
        }
    }

    public EventDrivenConsumerEndpoint(IApplicationContext context, ISubscribableChannel inputChannel, IMessageHandler handler)
        : base(context)
    {
        ArgumentGuard.NotNull(handler);
        ArgumentGuard.NotNull(inputChannel);

        _handler = handler;
        _inputChannel = inputChannel;
        Phase = int.MaxValue;
    }

    protected override Task DoStartAsync()
    {
        _inputChannel.Subscribe(_handler);

        if (_handler is ILifecycle lifecycle)
        {
            return lifecycle.StartAsync();
        }

        return Task.CompletedTask;
    }

    protected override Task DoStopAsync()
    {
        _inputChannel.Unsubscribe(_handler);

        if (_handler is ILifecycle lifecycle)
        {
            return lifecycle.StopAsync();
        }

        return Task.CompletedTask;
    }
}
