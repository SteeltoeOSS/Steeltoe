// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging
{
    public class MessagingException : Exception
    {
        public MessagingException(IMessage message)
        : base(null, null)
        {
            FailedMessage = message;
        }

        public MessagingException(string description)
        : base(description)
        {
            FailedMessage = null;
        }

        public MessagingException(string description, Exception cause)
        : base(description, cause)
        {
            FailedMessage = null;
        }

        public MessagingException(IMessage message, string description)
        : base(description)
        {
            FailedMessage = message;
        }

        public MessagingException(IMessage message, Exception cause)
        : base(null, cause)
        {
            FailedMessage = message;
        }

        public MessagingException(IMessage message, string description, Exception cause)
        : base(description, cause)
        {
            FailedMessage = message;
        }

        public IMessage FailedMessage { get; }

        public override string ToString()
        {
            return base.ToString() + (FailedMessage == null ? string.Empty
                    : (", failedMessage=" + FailedMessage));
        }
    }
}
