// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Messaging.Rabbit.Exceptions
{
    public class AmqpException : Exception
    {
        public AmqpException(string message)
        : base(message)
        {
        }

        public AmqpException(Exception cause)
        : base(null, cause)
        {
        }

        public AmqpException(string message, Exception cause)
        : base(message, cause)
        {
        }
    }
}
