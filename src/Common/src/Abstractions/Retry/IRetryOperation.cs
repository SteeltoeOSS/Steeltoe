// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;

namespace Steeltoe.Common.Retry;

/// <summary>
/// Defines the basic set of operations to execute operations with configurable retry behaviour.
/// </summary>
public interface IRetryOperation
{
    /// <summary>
    /// Execute the supplied RetryCallback with the configured retry semantics.
    /// </summary>
    /// <typeparam name="T">the type of return value</typeparam>
    /// <param name="retryCallback">the callback</param>
    /// <returns>result of operation</returns>
    T Execute<T>(Func<IRetryContext, T> retryCallback);

    /// <summary>
    /// Execute the supplied RetryCallback with the configured retry semantics. When retry
    /// is exhausted, call the recoverycallback
    /// </summary>
    /// <typeparam name="T">the type of return value</typeparam>
    /// <param name="retryCallback">the callback</param>
    /// <param name="recoveryCallback">the callback after retries are exhausted</param>
    /// <returns>result of the operation</returns>
    T Execute<T>(Func<IRetryContext, T> retryCallback, IRecoveryCallback<T> recoveryCallback);

    /// <summary>
    /// Execute the supplied RetryCallback with the configured retry semantics. When retry
    /// is exhausted, call the recoverycallback
    /// </summary>
    /// <typeparam name="T">the type of return value</typeparam>
    /// <param name="retryCallback">the callback</param>
    /// <param name="recoveryCallback">the callback after retries are exhausted</param>
    /// <returns>result of the operation</returns>
    T Execute<T>(Func<IRetryContext, T> retryCallback, Func<IRetryContext, T> recoveryCallback);

    /// <summary>
    /// Execute the supplied RetryCallback with the configured retry semantics.
    /// </summary>
    /// <param name="retryCallback">the callback</param>
    void Execute(Action<IRetryContext> retryCallback);

    /// <summary>
    /// Execute the supplied RetryCallback with the configured retry semantics. When retry
    /// is exhausted, call the recoverycallback
    /// </summary>
    /// <param name="retryCallback">the callback</param>
    /// <param name="recoveryCallback">the callback after retries are exhausted</param>
    void Execute(Action<IRetryContext> retryCallback, IRecoveryCallback recoveryCallback);

    /// <summary>
    /// Execute the supplied RetryCallback with the configured retry semantics. When retry
    /// is exhausted, call the recoverycallback
    /// </summary>
    /// <param name="retryCallback">the callback</param>
    /// <param name="recoveryCallback">the callback after retries are exhausted</param>
    void Execute(Action<IRetryContext> retryCallback, Action<IRetryContext> recoveryCallback);
}
