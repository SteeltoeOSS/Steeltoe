// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.Rabbit.Exceptions
{
    public class ImmediateAcknowledgeAmqpException : AmqpException
    {
        public ImmediateAcknowledgeAmqpException(string message)
        : base(message)
        {
        }

        public ImmediateAcknowledgeAmqpException(Exception cause)
        : base(cause)
        {
        }

        public ImmediateAcknowledgeAmqpException(string message, Exception cause)
        : base(message, cause)
        {
        }
    }
}
