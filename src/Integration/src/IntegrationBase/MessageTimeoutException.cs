// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration;

public class MessageTimeoutException : MessageDeliveryException
{
    public MessageTimeoutException(string description)
        : base(description)
    {
    }

    public MessageTimeoutException(IMessage failedMessage, string description, Exception cause)
        : base(failedMessage, description, cause)
    {
    }

    public MessageTimeoutException(IMessage failedMessage, string description)
        : base(failedMessage, description)
    {
    }

    public MessageTimeoutException(IMessage failedMessage)
        : base(failedMessage)
    {
    }
}