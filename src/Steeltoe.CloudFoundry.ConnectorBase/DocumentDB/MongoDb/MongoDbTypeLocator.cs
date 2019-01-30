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

namespace Steeltoe.CloudFoundry.Connector.MongoDb
{
    /// <summary>
    /// Assemblies and types used for interacting with MongoDB
    /// </summary>
    public static class MongoDbTypeLocator
    {
        /// <summary>
        /// List of supported MongoDB assemblies
        /// </summary>
        public static string[] Assemblies = new string[] { "MongoDB.Driver" };

        /// <summary>
        /// List of supported MongoDB client interface types
        /// </summary>
        public static string[] ConnectionInterfaceTypeNames = new string[] { "MongoDB.Driver.IMongoClient" };

        /// <summary>
        /// List of supported MongoDB client types
        /// </summary>
        public static string[] ConnectionTypeNames = new string[] { "MongoDB.Driver.MongoClient" };

        /// <summary>
        /// Class for describing MongoDB connection information
        /// </summary>
        public static string[] MongoConnectionInfo = new string[] { "MongoDB.Driver.MongoUrl" };

        /// <summary>
        /// Gets IMongoClient from MongoDB Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type IMongoClient => ConnectorHelpers.FindTypeOrThrow(Assemblies, ConnectionInterfaceTypeNames, "IMongoClient", "a MongoDB driver");

        /// <summary>
        /// Gets MongoClient from MongoDB Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type MongoClient => ConnectorHelpers.FindTypeOrThrow(Assemblies, ConnectionTypeNames, "MongoClient", "a MongoDB driver");

        /// <summary>
        /// Gets MongoUrl from MongoDB Library
        /// </summary>
        /// <exception cref="ConnectorException">When type is not found</exception>
        public static Type MongoUrl => ConnectorHelpers.FindTypeOrThrow(Assemblies, MongoConnectionInfo, "MongoUrl", "a MongoDB driver");
    }
}
