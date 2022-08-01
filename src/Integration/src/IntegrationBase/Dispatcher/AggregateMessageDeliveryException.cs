// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using System.Text;

namespace Steeltoe.Integration.Dispatcher;

public class AggregateMessageDeliveryException : MessageDeliveryException
{
    private readonly List<Exception> _aggregatedExceptions;

    public AggregateMessageDeliveryException(IMessage undeliveredMessage, string description, List<Exception> aggregatedExceptions)
        : base(undeliveredMessage, description, aggregatedExceptions[0])
    {
        _aggregatedExceptions = new List<Exception>(aggregatedExceptions);
    }

    public List<Exception> AggregatedExceptions
    {
        get { return new List<Exception>(_aggregatedExceptions); }
    }

    public override string Message
    {
        get
        {
            var baseMessage = base.Message;
            var message = new StringBuilder($"{AppendPeriodIfNecessary(baseMessage)} Multiple causes:\n");
            foreach (var exception in _aggregatedExceptions)
            {
                message.Append($"    {exception.Message}\n");
            }

            message.Append("See below for the stacktrace of the first cause.");
            return message.ToString();
        }
    }

    private string AppendPeriodIfNecessary(string baseMessage)
    {
        if (string.IsNullOrEmpty(baseMessage))
        {
            return string.Empty;
        }
        else if (!baseMessage.EndsWith("."))
        {
            return $"{baseMessage}.";
        }
        else
        {
            return baseMessage;
        }
    }
}
