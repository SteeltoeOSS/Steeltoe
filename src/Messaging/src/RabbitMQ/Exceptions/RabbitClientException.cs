// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client.Exceptions;

namespace Steeltoe.Messaging.RabbitMQ.Exceptions;

public class RabbitClientException : RabbitException
{
    public RabbitClientException(RabbitMQClientException cause)
        : base(cause)
    {
    }

    public RabbitClientException(string message, RabbitMQClientException cause)
        : base(message, cause)
    {
    }
}