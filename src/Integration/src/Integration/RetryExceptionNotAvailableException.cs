// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration;

public class RetryExceptionNotAvailableException : MessagingException
{
    public RetryExceptionNotAvailableException(IMessage message, string description)
        : base(message, description)
    {
    }

    internal RetryExceptionNotAvailableException(string description)
        : base(description)
    {
    }
}
