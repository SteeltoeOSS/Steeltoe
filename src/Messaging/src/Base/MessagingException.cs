// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging
{
    public class MessagingException : Exception
    {
        private readonly IMessage failedMessage;

        public MessagingException(IMessage message)
        : base(null, null)
        {
            failedMessage = message;
        }

        public MessagingException(string description)
        : base(description)
        {
            failedMessage = null;
        }

        public MessagingException(string description, Exception cause)
        : base(description, cause)
        {
            failedMessage = null;
        }

        public MessagingException(IMessage message, string description)
        : base(description)
        {
            failedMessage = message;
        }

        public MessagingException(IMessage message, Exception cause)
        : base(null, cause)
        {
            failedMessage = message;
        }

        public MessagingException(IMessage message, string description, Exception cause)
        : base(description, cause)
        {
            failedMessage = message;
        }

        public IMessage FailedMessage
        {
            get { return failedMessage; }
        }

        public override string ToString()
        {
            return base.ToString() + (failedMessage == null ? string.Empty
                    : (", failedMessage=" + failedMessage));
        }
    }
}
