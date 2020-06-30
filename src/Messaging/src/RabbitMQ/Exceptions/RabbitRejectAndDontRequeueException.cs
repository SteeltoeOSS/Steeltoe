// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.Rabbit.Exceptions
{
    public class RabbitRejectAndDontRequeueException : RabbitException
    {
        public RabbitRejectAndDontRequeueException(string message)
        : this(message, false, null)
        {
        }

        public RabbitRejectAndDontRequeueException(Exception cause)
        : this(null, false, cause)
        {
        }

        public RabbitRejectAndDontRequeueException(string message, Exception cause)
        : this(message, false, cause)
        {
        }

        public RabbitRejectAndDontRequeueException(string message, bool rejectManual, Exception cause)
        : base(message, cause)
        {
            IsRejectManual = rejectManual;
        }

        public bool IsRejectManual { get; }
    }
}
