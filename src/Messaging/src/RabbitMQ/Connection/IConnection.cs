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

using RabbitMQ.Client;
using System;

namespace Steeltoe.Messaging.Rabbit.Connection
{
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
        IModel CreateChannel(bool transactional = false);

        /// <summary>
        /// Close the connection
        /// </summary>
        void Close();

        /// <summary>
        /// Gets the underlying RabbitMQ connection
        /// </summary>
        RabbitMQ.Client.IConnection Connection { get; }

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
}
