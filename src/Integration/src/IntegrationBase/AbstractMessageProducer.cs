// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration;

public abstract class AbstractMessageProducer : IMessageProducer
{
    public abstract IMessageChannel OutputChannel { get; set; }

    public string OutputChannelName
    {
        get
        {
            return string.Empty;
        }

        set
        {
            throw new InvalidOperationException("This MessageProducer does not support setting the channel by name.");
        }
    }
}
