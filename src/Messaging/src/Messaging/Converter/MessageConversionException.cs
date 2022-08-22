// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.Converter;

public class MessageConversionException : MessagingException
{
    public MessageConversionException(string message)
        : base(message)
    {
    }

    public MessageConversionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public MessageConversionException(IMessage failedMessage, string message)
        : base(failedMessage, message)
    {
    }

    public MessageConversionException(IMessage failedMessage, string message, Exception innerException)
        : base(failedMessage, message, innerException)
    {
    }
}
