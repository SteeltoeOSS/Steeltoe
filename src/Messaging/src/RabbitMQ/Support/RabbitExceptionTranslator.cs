// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using Steeltoe.Messaging.Rabbit.Exceptions;
using System;
using System.IO;

namespace Steeltoe.Messaging.Rabbit.Support
{
    public static class RabbitExceptionTranslator
    {
        public static Exception ConvertRabbitAccessException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (exception is AmqpException)
            {
                return (AmqpException)exception;
            }

            if (exception is ShutdownSignalException)
            {
                return new AmqpConnectException((ShutdownSignalException)exception);
            }

            if (exception is ConnectFailureException || exception is BrokerUnreachableException)
            {
                return new AmqpConnectException(exception);
            }

            if (exception is PossibleAuthenticationFailureException)
            {
                return new AmqpAuthenticationException(exception);
            }

            if (exception is ProtocolViolationException || exception is UnsupportedMethodFieldException || exception is UnsupportedMethodException)
            {
                return new AmqpUnsupportedEncodingException(exception);
            }

            if (exception is IOException)
            {
                return new AmqpIOException((IOException)exception);
            }

            if (exception is TimeoutException)
            {
                return new AmqpTimeoutException(exception);
            }

            // if (ex is ConsumerCancelledException)
            // {
            //    return new org.springframework.amqp.rabbit.support.ConsumerCancelledException(ex);
            // }
            if (exception is ConsumerCancelledException)
            {
                return exception;
            }

            // fallback
            return new UncategorizedAmqpException(exception);
        }
    }
}
