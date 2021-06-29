// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Steeltoe.Common.Reflection;
using System;

namespace Steeltoe.Connector.CosmosDb
{
    /// <summary>
    /// Assemblies and types used for interacting with CosmosDbDB
    /// </summary>
    public static class CosmosDbTypeLocator
    {
        /// <summary>
        /// Gets a list of supported CosmosDbDB assemblies
        /// </summary>
        public static string[] Assemblies { get; internal set; } = new string[] { "Azure.Cosmos", "Microsoft.Azure.Cosmos.Client" };

        /// <summary>
        /// Gets a list of supported CosmosDbDB client types
        /// </summary>
        public static string[] ConnectionTypeNames { get; internal set; } = new string[] { "Azure.Cosmos.CosmosClient", "Microsoft.Azure.Cosmos.CosmosClient" };

        public static string[] ClientOptionsTypeNames { get; internal set; } = new string[] { "Azure.Cosmos.CosmosClientOptions", "Microsoft.Azure.Cosmos.CosmosClientOptions" };

        /// <summary>
        /// Gets CosmosDbClient from CosmosDbDB Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type CosmosClient => ReflectionHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "CosmosClient", "a CosmosDB client");

        public static Type CosmosClientOptions => ReflectionHelpers.FindTypeOrThrow(Assemblies, ClientOptionsTypeNames, "CosmosClientOptions", "a CosmosDB client");
    }
}
