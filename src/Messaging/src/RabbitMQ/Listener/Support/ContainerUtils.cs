// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;
using Steeltoe.Messaging.RabbitMQ.Exceptions;

namespace Steeltoe.Messaging.RabbitMQ.Listener.Support;

public static class ContainerUtils
{
    public static bool ShouldRequeue(bool defaultRequeueRejected, Exception exception, ILogger logger = null)
    {
        bool shouldRequeue = defaultRequeueRejected || exception is MessageRejectedWhileStoppingException;
        Exception e = exception;

        while (e != null)
        {
            if (e is RabbitRejectAndDoNotRequeueException)
            {
                shouldRequeue = false;
                break;
            }

            if (e is ImmediateRequeueException)
            {
                shouldRequeue = true;
                break;
            }

            e = e.InnerException;
        }

        logger?.LogDebug(exception, "Rejecting messages (requeue={requeue})", shouldRequeue);
        return shouldRequeue;
    }

    public static bool IsRejectManual(Exception exception)
    {
        return exception is RabbitRejectAndDoNotRequeueException rejectException && rejectException.IsRejectManual;
    }
}
