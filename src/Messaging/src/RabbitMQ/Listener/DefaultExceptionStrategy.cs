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

using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.Converter;
using Steeltoe.Messaging.Handler.Invocation;
using Steeltoe.Messaging.Rabbit.Listener.Exceptions;
using System;

namespace Steeltoe.Messaging.Rabbit.Listener
{
    public class DefaultExceptionStrategy : IFatalExceptionStrategy
    {
        private readonly ILogger _logger;

        public DefaultExceptionStrategy(ILogger logger = null)
        {
            _logger = logger;
        }

        public bool IsFatal(Exception exception)
        {
            var cause = exception.InnerException;
            while (cause is MessagingException && !(cause is MessageConversionException) && !(cause is MethodArgumentResolutionException))
            {
                cause = cause.InnerException;
            }

            if (exception is ListenerExecutionFailedException && IsCauseFatal(cause))
            {
                _logger?.LogWarning(
                        "Fatal message conversion error; message rejected; "
                                + "it will be dropped or routed to a dead letter exchange, if so configured: "
                                + ((ListenerExecutionFailedException)exception).FailedMessage);

                return true;
            }

            return false;
        }

        protected virtual bool IsUserCauseFatal(Exception cause)
        {
            return false;
        }

        private bool IsCauseFatal(Exception cause)
        {
            return cause is MessageConversionException
                    || cause is MethodArgumentResolutionException
                    || cause is MissingMethodException
                    || cause is InvalidCastException

                    || IsUserCauseFatal(cause);
        }
    }
}
