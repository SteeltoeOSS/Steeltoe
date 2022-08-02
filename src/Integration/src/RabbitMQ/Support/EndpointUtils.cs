// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ.Extensions;
using Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;

namespace Steeltoe.Integration.Rabbit.Support;

public static class EndpointUtils
{
    private const string ErrorMessage = "Message conversion failed";

    public static ListenerExecutionFailedException CreateErrorMessagePayload(IMessage message, IModel channel, bool isManualAck, Exception e)
    {
        if (isManualAck)
        {
            return new ManualAckListenerExecutionFailedException(ErrorMessage, e, message, channel, message.Headers.DeliveryTag().Value);
        }

        return new ListenerExecutionFailedException(ErrorMessage, e, message);
    }
}
