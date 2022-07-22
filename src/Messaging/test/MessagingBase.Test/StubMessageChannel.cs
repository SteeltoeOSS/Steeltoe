// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Test;

internal class StubMessageChannel : ISubscribableChannel
{
    private readonly List<IMessage<byte[]>> messages = new ();

    private readonly List<IMessageHandler> handlers = new ();

    public string ServiceName { get; set; } = "StubMessageChannel";

    public ValueTask<bool> SendAsync(IMessage message, CancellationToken cancellationToken)
    {
        messages.Add((IMessage<byte[]>)message);
        return new ValueTask<bool>(true);
    }

    public bool Send(IMessage message, int timeout)
    {
        messages.Add((IMessage<byte[]>)message);
        return true;
    }

    public bool Subscribe(IMessageHandler handler)
    {
        handlers.Add(handler);
        return true;
    }

    public bool Unsubscribe(IMessageHandler handler)
    {
        handlers.Remove(handler);
        return true;
    }

    public bool Send(IMessage message)
    {
        return Send(message, -1);
    }
}