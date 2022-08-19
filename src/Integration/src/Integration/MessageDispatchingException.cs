// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration;

public class MessageDispatchingException : MessageDeliveryException
{
    public MessageDispatchingException(string message)
        : base(message)
    {
    }

    public MessageDispatchingException(IMessage failedMessage)
        : base(failedMessage)
    {
    }

    public MessageDispatchingException(IMessage failedMessage, string message)
        : base(failedMessage, message)
    {
    }

    public MessageDispatchingException(IMessage failedMessage, string message, Exception innerException)
        : base(failedMessage, message, innerException)
    {
    }
}
