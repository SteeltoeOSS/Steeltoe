// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging;

public class MessageHandlingException : MessagingException
{
    public MessageHandlingException(IMessage failedMessage)
        : base(failedMessage)
    {
    }

    public MessageHandlingException(IMessage failedMessage, string message)
        : base(failedMessage, message)
    {
    }

    public MessageHandlingException(IMessage failedMessage, Exception innerException)
        : base(failedMessage, innerException)
    {
    }

    public MessageHandlingException(IMessage failedMessage, string message, Exception innerException)
        : base(failedMessage, message, innerException)
    {
    }
}
