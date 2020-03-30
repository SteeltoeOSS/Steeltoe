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

using Steeltoe.Common.Reflection;
using System;

namespace Steeltoe.CloudFoundry.Connector.CosmosDb
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
