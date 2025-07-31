// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class MessagingException : Exception
{
    private readonly IMessage _failedMessage;

    public MessagingException(IMessage message)
        : base(null, null)
    {
        _failedMessage = message;
    }

    public MessagingException(string description)
        : base(description)
    {
        _failedMessage = null;
    }

    public MessagingException(string description, Exception cause)
        : base(description, cause)
    {
        _failedMessage = null;
    }

    public MessagingException(IMessage message, string description)
        : base(description)
    {
        _failedMessage = message;
    }

    public MessagingException(IMessage message, Exception cause)
        : base(null, cause)
    {
        _failedMessage = message;
    }

    public MessagingException(IMessage message, string description, Exception cause)
        : base(description, cause)
    {
        _failedMessage = message;
    }

    public IMessage FailedMessage
    {
        get { return _failedMessage; }
    }

    public override string ToString()
    {
        return base.ToString() + (_failedMessage == null ? string.Empty
            : (", failedMessage=" + _failedMessage));
    }
}