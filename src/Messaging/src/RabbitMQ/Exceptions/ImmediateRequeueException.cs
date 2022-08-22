// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Exceptions;

public class ImmediateRequeueException : RabbitException
{
    public ImmediateRequeueException(string message)
        : base(message)
    {
    }

    public ImmediateRequeueException(Exception innerException)
        : base(innerException)
    {
    }

    public ImmediateRequeueException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
