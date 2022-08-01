// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;

namespace Steeltoe.Integration.Rabbit.Support;

public class RabbitMessageHeaderErrorMessageStrategy : IErrorMessageStrategy
{
    public const string AmqpRawMessage = $"{MessageHeaders.Internal}raw_message";

    public ErrorMessage BuildErrorMessage(Exception exception, IAttributeAccessor attributeAccessor)
    {
        var inputMessage = attributeAccessor?.GetAttribute(ErrorMessageUtils.InputMessageContextKey);
        var headers = new Dictionary<string, object>();
        if (attributeAccessor != null)
        {
            headers[AmqpRawMessage] = attributeAccessor.GetAttribute(AmqpRawMessage);
            headers[IntegrationMessageHeaderAccessor.SourceData] = attributeAccessor.GetAttribute(AmqpRawMessage);
        }

        return inputMessage is IMessage message
            ? new ErrorMessage(exception, headers, message)
            : new ErrorMessage(exception, headers);
    }
}
