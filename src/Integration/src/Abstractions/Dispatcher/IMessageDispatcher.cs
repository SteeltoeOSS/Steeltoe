// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Messaging;

namespace Steeltoe.Integration.Dispatcher;

/// <summary>
/// Strategy interface for dispatching messages to handlers.
/// </summary>
public interface IMessageDispatcher
{
    /// <summary>
    /// Gets the current handler count.
    /// </summary>
    int HandlerCount { get; }

    /// <summary>
    /// Gets or sets the maximum number of subscribers this dispatcher supports.
    /// </summary>
    int MaxSubscribers { get; set; }

    /// <summary>
    /// Gets or sets the load balancing strategy in use by the dispatcher.
    /// </summary>
    ILoadBalancingStrategy LoadBalancingStrategy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this dispatcher should failover upon a dispatching error.
    /// </summary>
    bool Failover { get; set; }

    /// <summary>
    /// Gets or sets the message handling decorator that should be applied to the message for processing.
    /// </summary>
    IMessageHandlingDecorator MessageHandlingDecorator { get; set; }

    /// <summary>
    /// Adds a handler to the dispatcher.
    /// </summary>
    /// <param name="handler">the handler to add.</param>
    /// <returns>true if added.</returns>
    bool AddHandler(IMessageHandler handler);

    /// <summary>
    /// Remove the specified handler from the dispatcher.
    /// </summary>
    /// <param name="handler">the handler to remove.</param>
    /// <returns>true if removed.</returns>
    bool RemoveHandler(IMessageHandler handler);

    /// <summary>
    /// Dispatch the message to one or more handlers.
    /// </summary>
    /// <param name="message">the message to dispatch.</param>
    /// <param name="cancellationToken">token used to cancel the operation.</param>
    /// <returns>the value returned from the handler.</returns>
    bool Dispatch(IMessage message, CancellationToken cancellationToken = default);
}
