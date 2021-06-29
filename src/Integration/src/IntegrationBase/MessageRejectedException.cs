// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;
using System;

namespace Steeltoe.Integration
{
    public class MessageRejectedException : MessageHandlingException
    {
        public MessageRejectedException(IMessage failedMessage, string description)
         : base(failedMessage, description)
        {
        }

        public MessageRejectedException(IMessage failedMessage, string description, Exception cause)
        : base(failedMessage, description, cause)
        {
        }
    }
}
