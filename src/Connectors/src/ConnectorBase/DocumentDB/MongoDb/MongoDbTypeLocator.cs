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
using System.Reflection;
using System.Threading;

namespace Steeltoe.Connector.MongoDb
{
    /// <summary>
    /// Assemblies and types used for interacting with MongoDB
    /// </summary>
    public static class MongoDbTypeLocator
    {
        /// <summary>
        /// Gets a list of supported MongoDB assemblies
        /// </summary>
        public static string[] Assemblies { get; internal set; } = new string[] { "MongoDB.Driver" };

        /// <summary>
        /// Gets a list of supported MongoDB client interface types
        /// </summary>
        public static string[] ConnectionInterfaceTypeNames { get; internal set; } = new string[] { "MongoDB.Driver.IMongoClient" };

        /// <summary>
        /// Gets a list of supported MongoDB client types
        /// </summary>
        public static string[] ConnectionTypeNames { get; internal set; } = new string[] { "MongoDB.Driver.MongoClient" };

        /// <summary>
        /// Gets the class used for describing MongoDB connection information
        /// </summary>
        public static string[] MongoConnectionInfo { get; internal set; } = new string[] { "MongoDB.Driver.MongoUrl" };

        /// <summary>
        /// Gets IMongoClient from MongoDB Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type IMongoClient => ReflectionHelpers.FindTypeOrThrow(Assemblies, ConnectionInterfaceTypeNames, "IMongoClient", "a MongoDB driver");

        /// <summary>
        /// Gets MongoClient from MongoDB Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type MongoClient => ReflectionHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "MongoClient", "a MongoDB driver");

        /// <summary>
        /// Gets MongoUrl from MongoDB Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type MongoUrl => ReflectionHelpers.FindTypeOrThrow(Assemblies, MongoConnectionInfo, "MongoUrl", "a MongoDB driver");

        /// <summary>
        /// Gets a method that lists databases available in a MongoClient
        /// </summary>
        public static MethodInfo ListDatabasesMethod => FindMethodOrThrow(MongoClient, "ListDatabases", new Type[] { typeof(CancellationToken) });

        private static MethodInfo FindMethodOrThrow(Type type, string methodName, Type[] parameters = null)
        {
            var returnType = ReflectionHelpers.FindMethod(type, methodName, parameters);
            if (returnType == null)
            {
                throw new ConnectorException("Unable to find required MongoDb type or method, are you missing a MongoDb Nuget package?");
            }

            return returnType;
        }
    }
}
