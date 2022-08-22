// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Transformer;

public class MessageTransformationException : MessagingException
{
    public MessageTransformationException(string message)
        : base(message)
    {
    }

    public MessageTransformationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public MessageTransformationException(IMessage failedMessage, string message)
        : base(failedMessage, message)
    {
    }

    public MessageTransformationException(IMessage failedMessage, string message, Exception innerException)
        : base(failedMessage, message, innerException)
    {
    }
}
