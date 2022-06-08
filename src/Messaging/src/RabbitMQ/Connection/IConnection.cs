// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System;
using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public interface IConnection : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the connection is open
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// Gets the local port of the connection
    /// </summary>
    int LocalPort { get; }

    /// <summary>
    /// Create a new channel, using an inernally allocated channel number
    /// </summary>
    /// <param name="transactional">true if transaction support on channel</param>
    /// <returns>the new channel</returns>
    RC.IModel CreateChannel(bool transactional = false);

    /// <summary>
    /// Close the connection
    /// </summary>
    void Close();

    /// <summary>
    /// Gets the underlying RabbitMQ connection
    /// </summary>
    RC.IConnection Connection { get; }

    /// <summary>
    /// Add a Blocked listener to the connection
    /// </summary>
    /// <param name="listener">the listener to add</param>
    void AddBlockedListener(IBlockedListener listener);

    /// <summary>
    /// Remove a Blocked listener from the connection
    /// </summary>
    /// <param name="listener">the listener to remove</param>
    /// <returns>true if successful</returns>
    bool RemoveBlockedListener(IBlockedListener listener);
}
