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

#pragma warning disable S3956 // "Generic.List" instances should not be part of public APIs
    public List<IMessage> FailedMessages { get; } = new();
#pragma warning restore S3956 // "Generic.List" instances should not be part of public APIs

    public ListenerExecutionFailedException(string message, Exception cause, params IMessage[] failedMessages)
        : base(message, cause)
    {
        FailedMessages.AddRange(failedMessages);
    }
}
