// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client.Exceptions;
using RabbitMQ.Client.Impl;
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

            if (exception is RabbitException)
            {
                return (RabbitException)exception;
            }

            if (exception is ChannelAllocationException)
            {
                return new RabbitResourceNotAvailableException(exception);
            }

            if (exception is ProtocolException || exception is ShutdownSignalException)
            {
                return new RabbitConnectException(exception);
            }

            if (exception is ConnectFailureException || exception is BrokerUnreachableException)
            {
                return new RabbitConnectException(exception);
            }

            if (exception is PossibleAuthenticationFailureException)
            {
                return new RabbitAuthenticationException(exception);
            }

            if (exception is OperationInterruptedException)
            {
                return new RabbitIOException(exception);
            }

            if (exception is IOException)
            {
                return new RabbitIOException(exception);
            }

            if (exception is TimeoutException)
            {
                return new RabbitTimeoutException(exception);
            }

            if (exception is ConsumerCancelledException)
            {
                throw exception;
            }

            // fallback
            return new RabbitUncategorizedException(exception);
        }
    }
}
