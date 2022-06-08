// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.Converter;

public class MessageConversionException : MessagingException
{
    public MessageConversionException(string description)
        : base(description)
    {
    }

    public MessageConversionException(string description, Exception cause)
        : base(description, cause)
    {
    }

    public MessageConversionException(IMessage failedMessage, string description)
        : base(failedMessage, description)
    {
    }

    public MessageConversionException(IMessage failedMessage, string description, Exception cause)
        : base(failedMessage, description, cause)
    {
    }
}
