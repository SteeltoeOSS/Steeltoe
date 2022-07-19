// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection;

public interface IConnectionListener
{
    /// <summary>
    /// Called when a new connection is established.
    /// </summary>
    /// <param name="connection">the connection.</param>
    void OnCreate(IConnection connection);

    /// <summary>
    /// Called when connection is closed.
    /// </summary>
    /// <param name="connection">the connection.</param>
    void OnClose(IConnection connection);

    /// <summary>
    /// Called when connection is forced to close.
    /// </summary>
    /// <param name="args">the event.</param>
    void OnShutDown(RC.ShutdownEventArgs args);
}
