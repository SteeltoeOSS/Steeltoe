// Copyright 2017 the original author or authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Steeltoe.CloudFoundry.Connector.RabbitMQ
{
    /// <summary>
    /// Assemblies and types used for interacting with RabbitMQ
    /// </summary>
    public static class RabbitMQTypeLocator
    {
        /// <summary>
        /// List of supported RabbitMQ assemblies
        /// </summary>
        public static readonly string[] Assemblies = new string[] { "RabbitMQ.Client" };

        /// <summary>
        /// List of RabbitMQ Interface types
        /// </summary>
        public static readonly string[] ConnectionInterfaceTypeNames = new string[] { "RabbitMQ.Client.IConnectionFactory" };

        /// <summary>
        /// List of RabbitMQ Implementation types
        /// </summary>
        public static readonly string[] ConnectionImplementationTypeNames = new string[] { "RabbitMQ.Client.ConnectionFactory" };

        /// <summary>
        /// Gets IConnectionFactory from a RabbitMQ Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type IConnectionFactory
        {
            get
            {
                var type = ConnectorHelpers.FindType(Assemblies, ConnectionInterfaceTypeNames);
                if (type == null)
                {
                    throw new ConnectorException("Unable to find RabbitMQ ConnectionFactory type. RabbitMQ.Client assembly may be missing");
                }

                return type;
            }
        }

        /// <summary>
        /// Gets ConnectionFactory from a RabbitMQ Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type ConnectionFactory
        {
            get
            {
                var type = ConnectorHelpers.FindType(Assemblies, ConnectionImplementationTypeNames);
                if (type == null)
                {
                    throw new ConnectorException("Unable to find RabbitMQ ConnectionFactory type. RabbitMQ.Client assembly may be missing");
                }

                return type;
            }
        }
    }
}
