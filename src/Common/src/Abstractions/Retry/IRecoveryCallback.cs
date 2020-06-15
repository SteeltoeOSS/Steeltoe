// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Retry
{
    /// <summary>
    /// Callback for stateful retry after all tries are exhausted
    /// </summary>
    public interface IRecoveryCallback
    {
        /// <summary>
        /// The callback that is issued
        /// </summary>
        /// <param name="context">the current retry context</param>
        /// <returns>an object that can be used to replace the callback result that failed</returns>
        object Recover(IRetryContext context);
    }

    /// <summary>
    /// Typed callback for stateful retry after all tries are exhausted
    /// </summary>
    /// <typeparam name="T">the type returned from callback</typeparam>
    public interface IRecoveryCallback<out T> : IRecoveryCallback
    {
        /// <summary>
        /// The callback that is issued
        /// </summary>
        /// <param name="context">the current retry context</param>
        /// <returns>an object that can be used to replace the callback result that failed</returns>
        new T Recover(IRetryContext context);
    }
}
