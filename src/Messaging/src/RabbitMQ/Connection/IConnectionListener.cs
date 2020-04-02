﻿// Copyright 2017 the original author or authors.
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

namespace Steeltoe.Messaging.Rabbit.Connection
{
    public interface IConnectionListener
    {
        /// <summary>
        /// Called when a new connection is established
        /// </summary>
        /// <param name="connection">the connection</param>
        void OnCreate(IConnection connection);

        /// <summary>
        /// Called when connection is closed
        /// </summary>
        /// <param name="connection">the connection</param>
        void OnClose(IConnection connection);

        /// <summary>
        /// Called when connection is forced to close
        /// </summary>
        /// <param name="args">the event</param>
        void OnShutDown(ShutdownEventArgs args);
    }
}
