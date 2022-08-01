// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Exceptions;

public class RabbitException : Exception
{
    public RabbitException(string message)
        : base(message)
    {
    }

    public RabbitException(Exception cause)
        : base(null, cause)
    {
    }

    public RabbitException(string message, Exception cause)
        : base(message, cause)
    {
    }
}
