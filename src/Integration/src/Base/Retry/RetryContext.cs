// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.Integration.Retry
{
    public class RetryContext : AbstractAttributeAccessor, IRetryContext
    {
        private const string LAST_EXCEPTION = "RetryContext.LastException";

        private const string RETRY_COUNT = "RetryContext.RetryCount";

        public Exception LastException
        {
            get
            {
                return (Exception)GetAttribute(LAST_EXCEPTION);
            }

#pragma warning disable S4275 // Getters and setters should access the expected fields
            set
#pragma warning restore S4275 // Getters and setters should access the expected fields
            {
                if (value == null && HasAttribute(LAST_EXCEPTION))
                {
                    RemoveAttribute(LAST_EXCEPTION);
                }
                else
                {
                    SetAttribute(LAST_EXCEPTION, value);
                }
            }
        }

        public int RetryCount
        {
            get
            {
                return (int)GetAttribute(RETRY_COUNT);
            }

#pragma warning disable S4275 // Getters and setters should access the expected fields
            set
#pragma warning restore S4275 // Getters and setters should access the expected fields
            {
                SetAttribute(RETRY_COUNT, value);
            }
        }
    }
}
