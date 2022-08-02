// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Stream.Binder;

internal sealed class FinalRethrowingErrorMessageHandler : ILastSubscriberMessageHandler
{
    private readonly ILastSubscriberAwareChannel _errorChannel;

    private readonly bool _defaultErrorChannelPresent;

    public string ServiceName { get; set; }

    public FinalRethrowingErrorMessageHandler(ILastSubscriberAwareChannel errorChannel, bool defaultErrorChannelPresent)
    {
        _errorChannel = errorChannel;
        _defaultErrorChannelPresent = defaultErrorChannelPresent;
        ServiceName = $"{GetType().Name}@{GetHashCode()}";
    }

    public void HandleMessage(IMessage message)
    {
        if (_errorChannel.Subscribers > (_defaultErrorChannelPresent ? 2 : 1))
        {
            // user has subscribed; default is 2, this and the bridge to the
            // errorChannel
            return;
        }

        if (message.Payload is MessagingException exception)
        {
            throw exception;
        }

        throw new MessagingException((IMessage)null, (Exception)message.Payload);
    }
}
