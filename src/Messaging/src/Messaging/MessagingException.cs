// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging;

public class MessagingException : Exception
{
    public IMessage FailedMessage { get; }

    public MessagingException(IMessage failedMessage)
    {
        FailedMessage = failedMessage;
    }

    public MessagingException(string message)
        : base(message)
    {
        FailedMessage = null;
    }

    public MessagingException(string message, Exception innerException)
        : base(message, innerException)
    {
        FailedMessage = null;
    }

    public MessagingException(IMessage failedMessage, string message)
        : base(message)
    {
        FailedMessage = failedMessage;
    }

    public MessagingException(IMessage failedMessage, Exception innerException)
        : base(null, innerException)
    {
        FailedMessage = failedMessage;
    }

    public MessagingException(IMessage failedMessage, string message, Exception innerException)
        : base(message, innerException)
    {
        FailedMessage = failedMessage;
    }

    public override string ToString()
    {
        return base.ToString() + (FailedMessage == null ? string.Empty : ", failedMessage=" + FailedMessage);
    }
}
