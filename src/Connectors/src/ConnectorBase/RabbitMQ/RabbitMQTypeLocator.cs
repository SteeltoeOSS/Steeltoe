﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Reflection;
using System;
using System.Reflection;

namespace Steeltoe.Connector.RabbitMQ
{
    /// <summary>
    /// Assemblies and types used for interacting with RabbitMQ
    /// </summary>
    public static class RabbitMQTypeLocator
    {
        /// <summary>
        /// Gets a list of supported RabbitMQ assemblies
        /// </summary>
        public static string[] Assemblies { get; internal set; } = new string[] { "RabbitMQ.Client" };

        /// <summary>
        /// Gets a list of RabbitMQ Interface types
        /// </summary>
        public static string[] ConnectionInterfaceTypeNames { get; internal set; } = new string[] { "RabbitMQ.Client.IConnectionFactory" };

        /// <summary>
        /// Gets a list of RabbitMQ Implementation types
        /// </summary>
        public static string[] ConnectionImplementationTypeNames { get; internal set; } = new string[] { "RabbitMQ.Client.ConnectionFactory" };

        /// <summary>
        /// Gets IConnectionFactory from a RabbitMQ Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type IConnectionFactory => ReflectionHelpers.FindTypeOrThrow(Assemblies, ConnectionInterfaceTypeNames, "IConnectionFactory", "the RabbitMQ.Client assembly");

        /// <summary>
        /// Gets ConnectionFactory from a RabbitMQ Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type ConnectionFactory => ReflectionHelpers.FindTypeOrThrow(Assemblies, ConnectionImplementationTypeNames, "ConnectionFactory", "the RabbitMQ.Client assembly");

        /// <summary>
        /// Gets IConnection from RabbitMQ Library
        /// </summary>
        public static Type IConnection => ReflectionHelpers.FindTypeOrThrow(Assemblies, new string[] { "RabbitMQ.Client.IConnection" }, "IConnection", "the RabbitMQ.Client assembly");

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
            var returnType = ReflectionHelpers.FindMethod(type, methodName, parameters);
            if (returnType == null)
            {
                throw new ConnectorException("Unable to find required RabbitMQ type, are you missing the RabbitMQ.Client Nuget package?");
            }

            return returnType;
        }
    }
}
