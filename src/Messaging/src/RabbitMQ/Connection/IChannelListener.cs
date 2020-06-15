// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RabbitMQ.Client;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public interface IChannelListener
    {
        /// <summary>
        /// Called when a channel has been created
        /// </summary>
        /// <param name="channel">the created channel</param>
        /// <param name="transactional">true if channel is transactional</param>
        void OnCreate(IModel channel, bool transactional);

        /// <summary>
        /// Called when a channel has been shutdown
        /// </summary>
        /// <param name="args">the shutdown event arguments</param>
        void OnShutDown(ShutdownEventArgs args);
    }
}
