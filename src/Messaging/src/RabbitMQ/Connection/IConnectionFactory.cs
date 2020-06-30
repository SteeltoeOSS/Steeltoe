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

using Steeltoe.Common.Services;
using System;

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public interface IConnectionFactory : IDisposable, IServiceNameAware
    {
        /// <summary>
        /// Gets the host name for the connection factory
        /// </summary>
        string Host { get; }

        /// <summary>
        /// Gets the port number for this connection factory
        /// </summary>
        int Port { get; }

        /// <summary>
        /// Gets the virtual host name for the connection factory
        /// </summary>
        string VirtualHost { get; }

        /// <summary>
        /// Gets the user name for the connection factory
        /// </summary>
        string Username { get; }

        /// <summary>
        /// Gets the publisher connection factory that will be used;
        /// </summary>
        IConnectionFactory PublisherConnectionFactory { get; }

        /// <summary>
        /// Gets a value indicating whether if simple publisher confirms are enabled
        /// </summary>
        bool IsSimplePublisherConfirms { get; }

        /// <summary>
        /// Gets a value indicating whether if publisher confirms are enabled
        /// </summary>
        bool IsPublisherConfirms { get; }

        /// <summary>
        /// Gets a value indicating whether if publisher returns are enabled
        /// </summary>
        bool IsPublisherReturns { get; }

        /// <summary>
        /// Add a connection listener to this factory
        /// </summary>
        /// <param name="connectionListener">the listener to add</param>
        void AddConnectionListener(IConnectionListener connectionListener);

        /// <summary>
        /// Remove a connection facotry from this factory
        /// </summary>
        /// <param name="connectionListener">the listener to remove</param>
        /// <returns>true if removed</returns>
        bool RemoveConnectionListener(IConnectionListener connectionListener);

        /// <summary>
        /// Remove all connection listeners
        /// </summary>
        void ClearConnectionListeners();

        /// <summary>
        /// Create a connection
        /// </summary>
        /// <returns>the connection if successful</returns>
        IConnection CreateConnection();

        /// <summary>
        /// Close underlying shared connection. The factory is still able to create new connections
        /// after this call
        /// </summary>
        void Destroy();
    }
}
