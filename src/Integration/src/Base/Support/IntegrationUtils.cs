// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration.Support
{
    public static class IntegrationUtils
    {
        public static Exception WrapInDeliveryExceptionIfNecessary(IMessage message, string text, Exception e)
        {
            if (!(e is MessagingException me))
            {
                return new MessageDeliveryException(message, text, e);
            }

            if (me.FailedMessage == null)
            {
                return new MessageDeliveryException(message, text, e);
            }

            return me;
        }

        public static Exception WrapInHandlingExceptionIfNecessary(IMessage message, string text, Exception e)
        {
            if (!(e is MessagingException me))
            {
                return new MessageHandlingException(message, text, e);
            }

            if (me.FailedMessage == null)
            {
                return new MessageHandlingException(message, text, e);
            }

            return me;
        }
    }
}
