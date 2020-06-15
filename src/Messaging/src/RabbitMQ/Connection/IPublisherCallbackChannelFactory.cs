// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    /// <summary>
    /// Factory for creating publisher callbacks
    /// </summary>
    public interface IPublisherCallbackChannelFactory
    {
        /// <summary>
        /// Create a publisher callback for the given channel
        /// </summary>
        /// <param name="channel">the channel</param>
        /// <returns>the callback</returns>
        IPublisherCallbackChannel CreateChannel(IModel channel);
    }
}
