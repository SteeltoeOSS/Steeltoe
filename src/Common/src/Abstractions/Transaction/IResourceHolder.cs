// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Common.Transaction;

public interface IResourceHolder
{
    /// <summary>
    /// Reset the transactional state
    /// </summary>
    void Reset();

    /// <summary>
    /// Notify holder that it has been unbound from transaction
    /// </summary>
    void Unbound();

    /// <summary>
    /// Gets a value indicating whether this holder is considered void, leftover from previous thread
    /// </summary>
    bool IsVoid { get; }
}