﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Retry;
using Steeltoe.Messaging.Rabbit.Core;
using Steeltoe.Messaging.Rabbit.Data;

namespace Steeltoe.Messaging.Rabbit.Support
{
    public static class SendRetryContextAccessor
    {
        public const string MESSAGE = "message";
        public const string ADDRESS = "address";

        public static Message GetMessage(RetryContext context)
        {
            return (Message)context.GetAttribute(MESSAGE);
        }

        public static Address GetAddress(RetryContext context)
        {
            return (Address)context.GetAttribute(ADDRESS);
        }
    }
}
