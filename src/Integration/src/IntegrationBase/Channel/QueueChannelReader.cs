// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Messaging;
using System.Threading.Channels;

namespace Steeltoe.Integration.Channel;

public class QueueChannelReader : ChannelReader<IMessage>
{
    private readonly ILogger _logger;

    public QueueChannelReader(QueueChannel channel, ILogger logger = null)
    {
        Channel = channel;
        _logger = logger;
    }

    protected QueueChannel Channel { get; }

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
