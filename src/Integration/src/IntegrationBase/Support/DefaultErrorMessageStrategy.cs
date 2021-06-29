﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Messaging;
using Steeltoe.Messaging.Support;
using System;

namespace Steeltoe.Integration.Support
{
    public class DefaultErrorMessageStrategy : IErrorMessageStrategy
    {
        public ErrorMessage BuildErrorMessage(Exception exception, IAttributeAccessor attributes)
        {
            var inputMessage = attributes == null ? null
                  : attributes.GetAttribute(ErrorMessageUtils.INPUT_MESSAGE_CONTEXT_KEY);
            if (inputMessage is IMessage)
            {
                return new ErrorMessage(exception, (IMessage)inputMessage);
            }
            else
            {
                return new ErrorMessage(exception);
            }
        }
    }
}
