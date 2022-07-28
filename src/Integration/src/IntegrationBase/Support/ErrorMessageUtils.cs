// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;

namespace Steeltoe.Integration.Support;

public static class ErrorMessageUtils
{
    public const string FailedMessageContextKey = $"{MessageHeaders.Internal}message";

    public const string InputMessageContextKey = $"{MessageHeaders.Internal}inputMessage";

    public static IAttributeAccessor GetAttributeAccessor(IMessage inputMessage, IMessage failedMessage)
    {
        AbstractAttributeAccessor attributes = new ErrorMessageAttributes();
        if (inputMessage != null)
        {
            attributes.SetAttribute(InputMessageContextKey, inputMessage);
        }

        if (failedMessage != null)
        {
            attributes.SetAttribute(FailedMessageContextKey, failedMessage);
        }

        return attributes;
    }
}
