// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Util;

namespace Steeltoe.Common.Retry;

/// <summary>
/// Low-level access to ongoing retry operation. Normally not needed by clients, but can be
/// used to alter the course of the retry, e.g.force an early termination.
/// </summary>
public interface IRetryContext : IAttributeAccessor
{
    /// <summary>
    /// Gets the last exception that caused the retry.
    /// </summary>
    Exception LastException { get; }

    /// <summary>
    /// Gets the number of retry attempts.
    /// </summary>
    int RetryCount { get; }

    /// <summary>
    /// Gets the parent context if present.
    /// </summary>
    IRetryContext Parent { get; }
}
