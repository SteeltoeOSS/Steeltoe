// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.Common.Retry;

public class RetryContext : AbstractAttributeAccessor, IRetryContext
{
    private const string LastExceptionName = "RetryContext.LastException";
    private const string RetryCountName = "RetryContext.RetryCount";
    private const string RetryParentName = "RetryContext.RetryParent";

    public Exception LastException
    {
        get => (Exception)GetAttribute(LastExceptionName);
        set
        {
            if (value == null && HasAttribute(LastExceptionName))
            {
                RemoveAttribute(LastExceptionName);
            }
            else
            {
                SetAttribute(LastExceptionName, value);
            }
        }
    }

    public int RetryCount
    {
        get
        {
            int? result = (int?)GetAttribute(RetryCountName);

            if (result == null)
            {
                return 0;
            }

            return result.Value;
        }
        set => SetAttribute(RetryCountName, value);
    }

    public IRetryContext Parent
    {
        get => (IRetryContext)GetAttribute(RetryParentName);
        set
        {
            if (value == null && HasAttribute(RetryParentName))
            {
                RemoveAttribute(RetryParentName);
            }
            else
            {
                SetAttribute(RetryParentName, value);
            }
        }
    }

    public override string ToString()
    {
        return $"LastException: {LastException?.Message}, RetryCount: {RetryCount}, RetryParent: {Parent}";
    }
}
