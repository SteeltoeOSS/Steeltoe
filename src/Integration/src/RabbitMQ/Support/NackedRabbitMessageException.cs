// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Rabbit.Support;

public class NackedRabbitMessageException : MessagingException
{
    public object CorrelationData { get; }
    public string NackReason { get; }

    public NackedRabbitMessageException(IMessage failedMessage, object correlationData, string nackReason)
        : base(failedMessage)
    {
        CorrelationData = correlationData;
        NackReason = nackReason;
    }
}
