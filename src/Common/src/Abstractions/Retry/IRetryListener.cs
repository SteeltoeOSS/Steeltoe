// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Retry;

/// <summary>
/// Interface for listener that can be used to add behaviour to a retry.
/// Implementations of RetryOperations can chose to issue callbacks to an
/// interceptor during the retry lifecycle.
/// </summary>
public interface IRetryListener
{
    /// <summary>
    /// Called before the first attempt in a retry.
    /// </summary>
    /// <param name="context">the current retry context.</param>
    /// <returns>true if the retry should proceed.</returns>
    bool Open(IRetryContext context);

    /// <summary>
    /// Called after the final attempt (successful or not).
    /// </summary>
    /// <param name="context">the current retry context.</param>
    /// <param name="exception">the last exception that was thrown during retry.</param>
    void Close(IRetryContext context, Exception exception);

    /// <summary>
    /// Called after every unsuccessful attempt at a retry.
    /// </summary>
    /// <param name="context">the current retry context.</param>
    /// <param name="exception">the last exception that was thrown during retry.</param>
    void OnError(IRetryContext context, Exception exception);
}
