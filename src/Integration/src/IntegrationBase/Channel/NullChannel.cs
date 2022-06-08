// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Integration.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Channel;

public class NullChannel : Channel<IMessage>, IPollableChannel
{
    private readonly ILogger _logger;

    public string ServiceName { get; set; } = IntegrationContextUtils.NULL_CHANNEL_BEAN_NAME;

    public NullChannel(ILogger logger = null)
    {
        _logger = logger;
        Writer = new NotSupportedChannelWriter();
        Reader = new NotSupportedChannelReader();
    }

    public IMessage Receive()
    {
        _logger?.LogDebug("receive called on null channel");
        return null;
    }

    public IMessage Receive(int timeout)
    {
        _logger?.LogDebug("receive called on null channel");
        return null;
    }

    public ValueTask<IMessage> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("receive called on null channel");
        return new ValueTask<IMessage>((IMessage)null);
    }

    public bool Send(IMessage message)
    {
        _logger?.LogDebug("message sent to null channel: " + message);
        return true;
    }

    public bool Send(IMessage message, int timeout)
    {
        _logger?.LogDebug("message sent to null channel: " + message);
        return Send(message);
    }

    public ValueTask<bool> SendAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        _logger?.LogDebug("message sent to null channel: " + message);
        return new ValueTask<bool>(false);
    }
}
