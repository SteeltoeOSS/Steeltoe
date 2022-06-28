// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Integration.Acks;

/// <summary>
/// AcknowledgmentCallback status values.
/// </summary>
public enum Status
{
    /// <summary>
    /// Mark message as accepted.
    /// </summary>
    ACCEPT,

    /// <summary>
    /// Mark message as rejected.
    /// </summary>
    REJECT,

    /// <summary>
    /// Reject message and requeue.
    /// </summary>
    REQUEUE
}
