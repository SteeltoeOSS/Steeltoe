// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// https://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Steeltoe.Common.Retry
{
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
}
