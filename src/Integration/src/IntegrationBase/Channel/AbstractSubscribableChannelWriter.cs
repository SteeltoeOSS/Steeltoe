// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Steeltoe.Integration.Channel;

public abstract class AbstractSubscribableChannelWriter : AbstractMessageChannelWriter
{
    protected AbstractSubscribableChannelWriter(AbstractSubscribableChannel channel, ILogger logger = null)
        : base(channel, logger)
    {
    }

    public virtual AbstractSubscribableChannel Channel => (AbstractSubscribableChannel)innerChannel;

    public override ValueTask<bool> WaitToWriteAsync(CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return new ValueTask<bool>(Task.FromCanceled<bool>(cancellationToken));
        }

        if (Channel.SubscriberCount > 0)
        {
            return new ValueTask<bool>(true);
        }

        return new ValueTask<bool>(false);
    }
}
