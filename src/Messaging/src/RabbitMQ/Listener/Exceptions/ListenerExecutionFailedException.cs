// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging.RabbitMQ.Listener.Exceptions;

public class ListenerExecutionFailedException : Exception
{
    public IMessage FailedMessage
    {
        get
        {
            if (FailedMessages.Count > 0)
            {
                return FailedMessages[0];
            }

            return null;
        }
    }

    public List<IMessage> FailedMessages { get; } = new();

    public ListenerExecutionFailedException(string message, Exception innerException, params IMessage[] failedMessages)
        : base(message, innerException)
    {
        FailedMessages.AddRange(failedMessages);
    }
}
