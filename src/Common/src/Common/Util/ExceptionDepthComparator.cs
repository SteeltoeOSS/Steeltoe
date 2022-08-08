// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Util;

public class ExceptionDepthComparator : IComparer<Type>
{
    private readonly Type _targetException;

    public ExceptionDepthComparator(Exception exception)
    {
        ArgumentGuard.NotNull(exception);

        _targetException = exception.GetType();
    }

    public ExceptionDepthComparator(Type exceptionType)
    {
        _targetException = exceptionType ?? throw new ArgumentNullException(nameof(exceptionType));
    }

    public int Compare(Type o1, Type o2)
    {
        int depth1 = GetDepth(o1, _targetException, 0);
        int depth2 = GetDepth(o2, _targetException, 0);
        return depth1 - depth2;
    }

    private int GetDepth(Type declaredException, Type exceptionToMatch, int depth)
    {
        if (exceptionToMatch.Equals(declaredException))
        {
            // Found it!
            return depth;
        }

        // If we've gone as far as we can go and haven't found it...
        if (exceptionToMatch == typeof(Exception))
        {
            return int.MaxValue;
        }

        return GetDepth(declaredException, exceptionToMatch.BaseType, depth + 1);
    }
}
