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

using Steeltoe.Common.Util;
using System;

namespace Steeltoe.Common.Retry
{
    /// <summary>
    /// Low-level access to ongoing retry operation. Normally not needed by clients, but can be
    /// used to alter the course of the retry, e.g.force an early termination.
    /// </summary>
    public interface IRetryContext : IAttributeAccessor
    {
        /// <summary>
        /// Gets the last exception that caused the retry
        /// </summary>
        Exception LastException { get; }

        /// <summary>
        /// Gets the number of retry attempts
        /// </summary>
        int RetryCount { get; }
    }
}
