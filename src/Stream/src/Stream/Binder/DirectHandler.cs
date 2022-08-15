// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Stream.Binder;

public class DirectHandler : IMessageHandler
{
    private readonly IMessageChannel _outputChannel;

    public virtual string ServiceName { get; set; }

    public DirectHandler(IMessageChannel outputChannel)
    {
        _outputChannel = outputChannel;
        ServiceName = $"{GetType().Name}@{GetHashCode()}";
    }

    public void HandleMessage(IMessage message)
    {
        _outputChannel.Send(message);
    }
}
