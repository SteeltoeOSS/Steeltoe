// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Steeltoe.Integration.Rabbit.Support;

[System.Obsolete("This feature will be removed in the next major version. See https://steeltoe.io/docs/v3/obsolete for details.")]
public class NackedRabbitMessageException : MessagingException
{
    public object CorrelationData { get; }

    public string NackReason { get; }

    public NackedRabbitMessageException(IMessage message, object correlationData, string nackReason)
        : base(message)
    {
        CorrelationData = correlationData;
        NackReason = nackReason;
    }
}