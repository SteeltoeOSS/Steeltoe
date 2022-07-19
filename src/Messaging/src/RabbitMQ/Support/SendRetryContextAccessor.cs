// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Retry;
using Steeltoe.Messaging.RabbitMQ.Core;

namespace Steeltoe.Messaging.RabbitMQ.Support;

public static class SendRetryContextAccessor
{
    public const string Message = "message";
    public const string Address = "address";

    public static IMessage GetMessage(IRetryContext context)
    {
        return (IMessage)context.GetAttribute(Message);
    }

    public static Address GetAddress(IRetryContext context)
    {
        return (Address)context.GetAttribute(Address);
    }
}
