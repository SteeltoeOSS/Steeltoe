// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
