// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Steeltoe.Messaging.Support
{
    public class NotSupportedChannelReader : ChannelReader<IMessage>
    {
        public override Task Completion => throw new NotSupportedException("This channel does not implement ChannelReaders");

        public override IAsyncEnumerable<IMessage> ReadAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("This channel does not implement ChannelReaders");
        }

        public override ValueTask<IMessage> ReadAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("This channel does not implement ChannelReaders");
        }

        public override bool TryRead(out IMessage item)
        {
            throw new NotSupportedException("This channel does not implement ChannelReaders");
        }

        public override ValueTask<bool> WaitToReadAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("This channel does not implement ChannelReaders");
        }
    }
}
