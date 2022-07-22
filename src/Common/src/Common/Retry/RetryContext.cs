// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.Common.Retry;

public class RetryContext : AbstractAttributeAccessor, IRetryContext
{
    private const string LAST_EXCEPTION = "RetryContext.LastException";

    private const string RETRY_COUNT = "RetryContext.RetryCount";

    private const string RETRY_PARENT = "RetryContext.RetryParent";

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
            var result = (int?)GetAttribute(RETRY_COUNT);
            if (result == null)
            {
                return 0;
            }

            return result.Value;
        }

#pragma warning disable S4275 // Getters and setters should access the expected fields
        set
#pragma warning restore S4275 // Getters and setters should access the expected fields
        {
            SetAttribute(RETRY_COUNT, value);
        }
    }

    public IRetryContext Parent
    {
        get
        {
            return (IRetryContext)GetAttribute(RETRY_PARENT);
        }

#pragma warning disable S4275 // Getters and setters should access the expected fields
        set
#pragma warning restore S4275 // Getters and setters should access the expected fields
        {
            if (value == null && HasAttribute(RETRY_PARENT))
            {
                RemoveAttribute(RETRY_PARENT);
            }
            else
            {
                SetAttribute(RETRY_PARENT, value);
            }
        }
    }

    public override string ToString()
    {
        return $"LastException: {LastException?.Message}, RetryCount: {RetryCount}, RetryParent: {Parent}";
    }
}