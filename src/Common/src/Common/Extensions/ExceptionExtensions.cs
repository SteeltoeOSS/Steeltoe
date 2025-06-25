// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace Steeltoe.Common.Extensions;

internal static class ExceptionExtensions
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
        ArgumentNullException.ThrowIfNull(exception);

        bool hasChanges = true;

        while (hasChanges)
        {
            hasChanges = false;

            if (exception is TargetInvocationException && exception.InnerException != null)
            {
                exception = exception.InnerException;
                hasChanges = true;
            }
            else if (exception is AggregateException { InnerExceptions.Count: 1 } aggregateException)
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

    /// <summary>
    /// Determines whether the thrown exception results from an HTTP request timeout.
    /// </summary>
    /// <param name="exception">
    /// The caught exception to inspect.
    /// </param>
    public static bool IsHttpClientTimeout(this Exception? exception)
    {
        // See note in remarks at https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.sendasync.
        if (exception is OperationCanceledException && exception.InnerException?.GetType() == typeof(TimeoutException))
        {
            return true;
        }

        return false;
    }
}
