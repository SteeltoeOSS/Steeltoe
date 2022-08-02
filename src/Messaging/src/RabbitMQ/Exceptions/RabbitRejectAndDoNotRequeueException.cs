// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Exceptions;

public class RabbitRejectAndDoNotRequeueException : RabbitException
{
    public bool IsRejectManual { get; }

    public RabbitRejectAndDoNotRequeueException(string message)
        : this(message, false, null)
    {
    }

    public RabbitRejectAndDoNotRequeueException(Exception cause)
        : this(null, false, cause)
    {
    }

    public RabbitRejectAndDoNotRequeueException(string message, Exception cause)
        : this(message, false, cause)
    {
    }

    public RabbitRejectAndDoNotRequeueException(string message, bool rejectManual, Exception cause)
        : base(message, cause)
    {
        IsRejectManual = rejectManual;
    }
}
