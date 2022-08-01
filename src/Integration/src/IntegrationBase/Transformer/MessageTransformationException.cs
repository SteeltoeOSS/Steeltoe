// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Transformer;

public class MessageTransformationException : MessagingException
{
    public MessageTransformationException(IMessage message, string description, Exception cause)
        : base(message, description, cause)
    {
    }

    public MessageTransformationException(IMessage message, string description)
        : base(message, description)
    {
    }

    public MessageTransformationException(string description, Exception cause)
        : base(description, cause)
    {
    }

    public MessageTransformationException(string description)
        : base(description)
    {
    }
}
