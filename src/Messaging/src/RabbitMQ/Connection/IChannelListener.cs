// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using RC=RabbitMQ.Client;

namespace Steeltoe.Messaging.RabbitMQ.Connection
{
    public interface IChannelListener
    {
        /// <summary>
        /// Called when a channel has been created
        /// </summary>
        /// <param name="channel">the created channel</param>
        /// <param name="transactional">true if channel is transactional</param>
        void OnCreate(RC.IModel channel, bool transactional);

        /// <summary>
        /// Called when a channel has been shutdown
        /// </summary>
        /// <param name="args">the shutdown event arguments</param>
        void OnShutDown(RC.ShutdownEventArgs args);
    }
}
