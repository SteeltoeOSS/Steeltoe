// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Reflection;

namespace Steeltoe.Common;

public static class ExceptionExtensions
{
    /// <summary>
    /// Unwraps a potential tree of nested exceptions, returning the deepest one. Exceptions of type <see cref="TargetInvocationException" /> and
    /// <see cref="AggregateException" /> with a single inner exception are unwrapped.
    /// </summary>
    /// <param name="exception">
    /// The exception to unwrap.
    /// </param>
    /// <returns>
    /// The deepest exception, or the original <paramref name="exception" /> is there's nothing to unwrap.
    /// </returns>
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

    /// <summary>
    /// Determines whether the thrown exception results from incoming request cancellation.
    /// </summary>
    /// <param name="exception">
    /// The caught exception to inspect.
    /// </param>
    public static bool IsCancellation(this Exception? exception)
    {
        // See note in remarks at https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.sendasync.
        if (exception is OperationCanceledException && exception.InnerException?.GetType() != typeof(TimeoutException))
        {
            return true;
        }

        return false;
    }
}
