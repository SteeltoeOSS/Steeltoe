// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Reflection;

namespace Steeltoe.Common;

internal static class ExceptionExtensions
{
    public static Exception UnwrapAll(this Exception exception)
    {
        ArgumentGuard.NotNull(exception);

        bool hasChanges = true;

        while (hasChanges)
        {
            hasChanges = false;

            if (exception is TargetInvocationException && exception.InnerException != null)
            {
                exception = exception.InnerException;
                hasChanges = true;
            }
            else if (exception is AggregateException aggregateException && aggregateException.InnerExceptions.Count == 1)
            {
                exception = aggregateException.InnerExceptions[0];
                hasChanges = true;
            }
        }

        return exception;
    }
}
