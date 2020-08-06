// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration
{
    public class MessageDispatchingException : MessageDeliveryException
    {
        public MessageDispatchingException(string description)
        : base(description)
        {
        }

        public MessageDispatchingException(IMessage undeliveredMessage, string description, Exception cause)
        : base(undeliveredMessage, description, cause)
        {
        }

        public MessageDispatchingException(IMessage undeliveredMessage, string description)
        : base(undeliveredMessage, description)
        {
        }

        public MessageDispatchingException(IMessage undeliveredMessage)
        : base(undeliveredMessage)
        {
        }
    }
}
