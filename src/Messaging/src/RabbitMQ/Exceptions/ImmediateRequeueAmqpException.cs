// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.Rabbit.Exceptions
{
    public class ImmediateRequeueAmqpException : AmqpException
    {
        public ImmediateRequeueAmqpException(string message)
        : base(message)
        {
        }

        public ImmediateRequeueAmqpException(Exception cause)
        : base(cause)
        {
        }

        public ImmediateRequeueAmqpException(string message, Exception cause)
        : base(message, cause)
        {
        }
    }
}
