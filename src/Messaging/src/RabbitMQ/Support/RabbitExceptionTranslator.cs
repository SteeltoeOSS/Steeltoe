// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Impl;
using Steeltoe.Common;
using Steeltoe.Messaging.RabbitMQ.Exceptions;

namespace Steeltoe.Messaging.RabbitMQ.Support;

public static class RabbitExceptionTranslator
{
    public static Exception ConvertRabbitAccessException(Exception exception)
    {
        ArgumentGuard.NotNull(exception);

        return exception switch
        {
            RabbitException rabbitException => rabbitException,
            ChannelAllocationException => new RabbitResourceNotAvailableException(exception),
            ProtocolException or ShutdownSignalException => new RabbitConnectException(exception),
            ConnectFailureException or BrokerUnreachableException => new RabbitConnectException(exception),
            PossibleAuthenticationFailureException => new RabbitAuthenticationException(exception),
            OperationInterruptedException => new RabbitIOException(exception),
            IOException => new RabbitIOException(exception),
            TimeoutException => new RabbitTimeoutException(exception),
            ConsumerCancelledException => exception,
            _ => new RabbitUncategorizedException(exception)
        };
    }
}
