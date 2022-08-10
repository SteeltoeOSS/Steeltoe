// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace Steeltoe.Integration.Channel;

public class QueueChannelWriter : AbstractMessageChannelWriter
{
    public QueueChannelWriter(AbstractPollableChannel channel, ILogger logger = null)
        : base(channel, logger)
    {
    }
}
