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
using Steeltoe.Messaging.Rabbit.Exceptions;
using System;

namespace Steeltoe.Messaging.Rabbit.Listener.Support
{
    public static class ContainerUtils
    {
        public static bool ShouldRequeue(bool defaultRequeueRejected, Exception exception, ILogger logger = null)
        {
            var shouldRequeue = defaultRequeueRejected || exception is MessageRejectedWhileStoppingException;
            var e = exception;
            while (e != null)
            {
                if (e is RabbitRejectAndDontRequeueException)
                {
                    shouldRequeue = false;
                    break;
                }
                else if (e is ImmediateRequeueException)
                {
                    shouldRequeue = true;
                    break;
                }

                e = e.InnerException;
            }

            logger?.LogDebug("Rejecting messages (requeue={requeue})", shouldRequeue);
            return shouldRequeue;
        }

        public static bool IsRejectManual(Exception exception)
        {
            return exception is RabbitRejectAndDontRequeueException && ((RabbitRejectAndDontRequeueException)exception).IsRejectManual;
        }
    }
}
