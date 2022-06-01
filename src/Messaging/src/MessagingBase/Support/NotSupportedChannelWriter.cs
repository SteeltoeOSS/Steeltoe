// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Support;

public class NotSupportedChannelWriter : ChannelWriter<IMessage>
{
    public override bool TryComplete(Exception error = null)
    {
        throw new NotSupportedException("This channel does not implement ChannelWriters");
    }

    public override bool TryWrite(IMessage item)
    {
        throw new NotSupportedException("This channel does not implement ChannelWriters");
    }

    public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("This channel does not implement ChannelWriters");
    }

    public override ValueTask WriteAsync(IMessage item, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("This channel does not implement ChannelWriters");
    }
}
