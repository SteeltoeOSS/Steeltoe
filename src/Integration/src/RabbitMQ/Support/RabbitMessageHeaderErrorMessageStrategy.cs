// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using Steeltoe.Integration.Support;
using Steeltoe.Messaging;
using Steeltoe.Messaging.RabbitMQ;
using Steeltoe.Messaging.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Steeltoe.Integration.Rabbit.Support
{
    public class RabbitMessageHeaderErrorMessageStrategy : IErrorMessageStrategy
    {
        public const string AMQP_RAW_MESSAGE = MessageHeaders.INTERNAL + "raw_message";

        public ErrorMessage BuildErrorMessage(Exception exception, IAttributeAccessor context)
        {
            var inputMessage = context == null ? null : context.GetAttribute(ErrorMessageUtils.INPUT_MESSAGE_CONTEXT_KEY);
            var headers = new Dictionary<string, object>();
            if (context != null)
            {
                headers[AMQP_RAW_MESSAGE] = context.GetAttribute(AMQP_RAW_MESSAGE);
                headers[IntegrationMessageHeaderAccessor.SOURCE_DATA] = context.GetAttribute(AMQP_RAW_MESSAGE);
            }

            if (inputMessage is IMessage)
            {
                return new ErrorMessage(exception.InnerException, headers, (IMessage)inputMessage);
            }
            else
            {
                return new ErrorMessage(exception.InnerException, headers);
            }
        }
    }
}
