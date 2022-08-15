// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Channel;

public class QueueChannelReader : ChannelReader<IMessage>
{
    private readonly ILogger _logger;

    protected QueueChannel Channel { get; }

    public QueueChannelReader(QueueChannel channel, ILogger logger = null)
    {
        Channel = channel;
        _logger = logger;
    }

    public override bool TryRead(out IMessage item)
    {
        _logger?.LogDebug("TryRead issued");
        item = Channel.Receive(0);
        return item != null;
    }

    public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
    {
        return Channel.Reader.WaitToReadAsync(cancellationToken);
    }
}
