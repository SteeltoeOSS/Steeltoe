// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging;

public class MessageDeliveryException : MessagingException
{
    public MessageDeliveryException(string description)
        : base(description)
    {
    }

    public MessageDeliveryException(IMessage undeliveredMessage)
        : base(undeliveredMessage)
    {
    }

    public MessageDeliveryException(IMessage undeliveredMessage, string description)
        : base(undeliveredMessage, description)
    {
    }

    public MessageDeliveryException(IMessage message, Exception cause)
        : base(message, cause)
    {
    }

    public MessageDeliveryException(IMessage undeliveredMessage, string description, Exception cause)
        : base(undeliveredMessage, description, cause)
    {
    }
}
