// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Exceptions;

public class RabbitTimeoutException : RabbitException
{
    public RabbitTimeoutException(string message)
        : base(message)
    {
    }

    public RabbitTimeoutException(Exception innerException)
        : base(innerException)
    {
    }

    public RabbitTimeoutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
