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
    /// Interface for listener that can be used to add behaviour to a retry.
    /// Implementations of RetryOperations can chose to issue callbacks to an
    /// interceptor during the retry lifecycle.
    /// </summary>
    public interface IRetryListener
    {
        /// <summary>
        /// Called before the first attempt in a retry.
        /// </summary>
        /// <param name="context">the current retry context</param>
        /// <returns>true if the retry should proceed</returns>
        bool Open(IRetryContext context);

        /// <summary>
        /// Called after the final attempt (successful or not).
        /// </summary>
        /// <param name="context">the current retry context</param>
        /// <param name="exception">the last exception that was thrown during retry</param>
        void Close(IRetryContext context, Exception exception);

        /// <summary>
        /// Called after every unsuccessful attempt at a retry.
        /// </summary>
        /// <param name="context">the current retry context</param>
        /// <param name="exception">the last exception that was thrown during retry</param>
        void OnError(IRetryContext context, Exception exception);
    }
}
