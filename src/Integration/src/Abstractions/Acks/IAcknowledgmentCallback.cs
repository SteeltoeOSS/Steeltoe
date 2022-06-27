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

/// <summary>
/// General abstraction over acknowlegements.
/// </summary>
public interface IAcknowledgmentCallback
{
    /// <summary>
    /// Acknowledge the message.
    /// </summary>
    /// <param name="status">true if the message is already acked.</param>
    void Acknowledge(Status status);

    /// <summary>
    /// Gets or sets a value indicating whether the ack has been
    /// processed by the user so that the framework can auto-ack if needed.
    /// </summary>
    bool IsAcknowledged { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether return true if this acknowledgment supports auto ack when it has not been
    /// already ack'd by the application.
    /// </summary>
    bool IsAutoAck { get; set; }
}
