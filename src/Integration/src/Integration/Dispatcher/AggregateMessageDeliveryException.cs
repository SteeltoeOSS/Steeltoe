// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Text;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Dispatcher;

public class AggregateMessageDeliveryException : MessageDeliveryException
{
    private readonly List<Exception> _aggregatedExceptions;

    public List<Exception> AggregatedExceptions => new(_aggregatedExceptions);

    public override string Message
    {
        get
        {
            string baseMessage = base.Message;
            var message = new StringBuilder($"{AppendPeriodIfNecessary(baseMessage)} Multiple causes:\n");

            foreach (Exception exception in _aggregatedExceptions)
            {
                message.Append($"    {exception.Message}\n");
            }

            message.Append("See below for the stacktrace of the first cause.");
            return message.ToString();
        }
    }

    public AggregateMessageDeliveryException(IMessage failedMessage, string message, List<Exception> aggregatedExceptions)
        : base(failedMessage, message, aggregatedExceptions[0])
    {
        _aggregatedExceptions = new List<Exception>(aggregatedExceptions);
    }

    private string AppendPeriodIfNecessary(string baseMessage)
    {
        if (string.IsNullOrEmpty(baseMessage))
        {
            return string.Empty;
        }

        if (!baseMessage.EndsWith(".", StringComparison.Ordinal))
        {
            return $"{baseMessage}.";
        }

        return baseMessage;
    }
}
