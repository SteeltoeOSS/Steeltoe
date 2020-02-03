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

using Steeltoe.Messaging;
using System.Threading;

namespace Steeltoe.Integration.Dispatcher
{
    /// <summary>
    /// Strategy interface for dispatching messages to handlers.
    /// </summary>
    public interface IMessageDispatcher
    {
        /// <summary>
        /// Gets the current handler count
        /// </summary>
        int HandlerCount { get; }

        /// <summary>
        /// Gets or sets the maximum number of subscribers this dispatcher supports
        /// </summary>
        int MaxSubscribers { get; set; }

        /// <summary>
        /// Gets or sets the load balancing strategy in use by the dispatcher
        /// </summary>
        ILoadBalancingStrategy LoadBalancingStrategy { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this dispatcher should failover upon a dispatching error
        /// </summary>
        bool Failover { get; set; }

        /// <summary>
        /// Gets or sets the message handling decorator that should be applied to the message for processing
        /// </summary>
        IMessageHandlingDecorator MessageHandlingDecorator { get; set; }

        /// <summary>
        /// Adds a handler to the dispatcher
        /// </summary>
        /// <param name="handler">the handler to add</param>
        /// <returns>true if added</returns>
        bool AddHandler(IMessageHandler handler);

        /// <summary>
        /// Remove the specified handler from the dispatcher
        /// </summary>
        /// <param name="handler">the handler to remove</param>
        /// <returns>true if removed</returns>
        bool RemoveHandler(IMessageHandler handler);

        /// <summary>
        /// Dispatch the message to one or more handlers
        /// </summary>
        /// <param name="message">the message to dispatch</param>
        /// <param name="cancellationToken">token used to cancel the operation</param>
        /// <returns>the value returned from the handler</returns>
        bool Dispatch(IMessage message, CancellationToken cancellationToken = default);
    }
}
