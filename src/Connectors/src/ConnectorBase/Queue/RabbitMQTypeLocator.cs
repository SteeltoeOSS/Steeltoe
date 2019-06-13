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

using System;
using System.Reflection;

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
        public static string[] Assemblies = new string[] { "RabbitMQ.Client" };

        /// <summary>
        /// List of RabbitMQ Interface types
        /// </summary>
        public static string[] ConnectionInterfaceTypeNames = new string[] { "RabbitMQ.Client.IConnectionFactory" };

        /// <summary>
        /// List of RabbitMQ Implementation types
        /// </summary>
        public static string[] ConnectionImplementationTypeNames = new string[] { "RabbitMQ.Client.ConnectionFactory" };

        /// <summary>
        /// Gets IConnectionFactory from a RabbitMQ Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type IConnectionFactory => ConnectorHelpers.FindTypeOrThrow(Assemblies, ConnectionInterfaceTypeNames, "IConnectionFactory", "the RabbitMQ.Client assembly");

        /// <summary>
        /// Gets ConnectionFactory from a RabbitMQ Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type ConnectionFactory => ConnectorHelpers.FindTypeOrThrow(Assemblies, ConnectionImplementationTypeNames, "ConnectionFactory", "the RabbitMQ.Client assembly");

        /// <summary>
        /// Gets IConnection from RabbitMQ Library
        /// </summary>
        public static Type IConnection => ConnectorHelpers.FindTypeOrThrow(Assemblies, new string[] { "RabbitMQ.Client.IConnection" }, "IConnection", "the RabbitMQ.Client assembly");

        /// <summary>
        /// Gets the CreateConnection method of ConnectionFactory
        /// </summary>
        public static MethodInfo CreateConnectionMethod => FindMethodOrThrow(ConnectionFactory, "CreateConnection", Array.Empty<Type>());

        /// <summary>
        /// Gets the Close method for IConnection
        /// </summary>
        public static MethodInfo CloseConnectionMethod => FindMethodOrThrow(IConnection, "Close", Array.Empty<Type>());

        private static MethodInfo FindMethodOrThrow(Type type, string methodName, Type[] parameters = null)
        {
            var returnType = ConnectorHelpers.FindMethod(type, methodName, parameters);
            if (returnType == null)
            {
                throw new ConnectorException("Unable to find required RabbitMQ type, are you missing the RabbitMQ.Client Nuget package?");
            }

            return returnType;
        }
    }
}
