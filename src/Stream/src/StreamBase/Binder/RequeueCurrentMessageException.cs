﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Stream.Binder
{
    public class RequeueCurrentMessageException : Exception
    {
        public RequeueCurrentMessageException()
         : base()
        {
        }

        public RequeueCurrentMessageException(string message, Exception cause)
            : base(message, cause)
        {
        }

        public RequeueCurrentMessageException(string message)
        : base(message)
        {
        }

        public RequeueCurrentMessageException(Exception cause)
        : base(string.Empty, cause)
        {
        }
    }
}
