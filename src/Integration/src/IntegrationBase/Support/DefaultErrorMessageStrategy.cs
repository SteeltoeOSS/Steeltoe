// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;

namespace Steeltoe.Integration.Support;

public class DefaultErrorMessageStrategy : IErrorMessageStrategy
{
    public ErrorMessage BuildErrorMessage(Exception exception, IAttributeAccessor attributeAccessor)
    {
        object inputMessage = attributeAccessor?.GetAttribute(ErrorMessageUtils.InputMessageContextKey);

        return inputMessage switch
        {
            IMessage iMessage => new ErrorMessage(exception, iMessage),
            _ => new ErrorMessage(exception)
        };
    }
}
