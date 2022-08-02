// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Channel;

public abstract class AbstractMessageChannelWriter : ChannelWriter<IMessage>
{
    protected AbstractMessageChannel channel;
    protected ILogger logger;

    protected AbstractMessageChannelWriter(AbstractMessageChannel channel, ILogger logger = null)
    {
        this.channel = channel ?? throw new ArgumentNullException(nameof(channel));
        this.logger = logger;
    }

    public override bool TryComplete(Exception error = null)
    {
        return false;
    }

    public override bool TryWrite(IMessage message)
    {
        return channel.Send(message, 0);
    }

    public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
    {
        return cancellationToken.IsCancellationRequested ? new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken)) : new ValueTask<bool>(true);
    }

    public override ValueTask WriteAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        if (TryWrite(message))
        {
            return default;
        }

        return new ValueTask(Task.FromException(new MessageDeliveryException(message)));
    }
}
