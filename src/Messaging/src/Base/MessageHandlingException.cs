// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging
{
    public class MessageHandlingException : MessagingException
    {
        public MessageHandlingException(IMessage failedMessage)
            : base(failedMessage)
        {
        }

        public MessageHandlingException(IMessage message, string description)
        : base(message, description)
        {
        }

        public MessageHandlingException(IMessage failedMessage, Exception cause)
        : base(failedMessage, cause)
        {
        }

        public MessageHandlingException(IMessage message, string description, Exception cause)
        : base(message, description, cause)
        {
        }
    }
}
