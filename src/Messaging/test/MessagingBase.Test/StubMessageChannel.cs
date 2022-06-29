// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Test;

internal sealed class StubMessageChannel : ISubscribableChannel
{
    private readonly List<IMessage<byte[]>> _messages = new ();

    private readonly List<IMessageHandler> _handlers = new ();

    public string ServiceName { get; set; } = "StubMessageChannel";

    public ValueTask<bool> SendAsync(IMessage message, CancellationToken cancellationToken = default)
    {
        _messages.Add((IMessage<byte[]>)message);
        return new ValueTask<bool>(true);
    }

    public bool Send(IMessage message)
    {
        return Send(message, -1);
    }

    public bool Send(IMessage message, int timeout)
    {
        _messages.Add((IMessage<byte[]>)message);
        return true;
    }

    public bool Subscribe(IMessageHandler handler)
    {
        _handlers.Add(handler);
        return true;
    }

    public bool Unsubscribe(IMessageHandler handler)
    {
        _handlers.Remove(handler);
        return true;
    }
}
