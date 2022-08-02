// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

namespace Steeltoe.Messaging;

/// <summary>
/// A MessageChannel that maintains a registry of subscribers and invokes them to handle messages sent through this channel.
/// </summary>
public interface ISubscribableChannel : IMessageChannel
{
    /// <summary>
    /// Register a message handler.
    /// </summary>
    /// <param name="handler">
    /// the handler to register.
    /// </param>
    /// <returns>
    /// false if already registered; otherwise true.
    /// </returns>
    bool Subscribe(IMessageHandler handler);

    /// <summary>
    /// Un-register a message handler.
    /// </summary>
    /// <param name="handler">
    /// the handler to remove.
    /// </param>
    /// <returns>
    /// false if not registered; otherwise true.
    /// </returns>
    bool Unsubscribe(IMessageHandler handler);
}
