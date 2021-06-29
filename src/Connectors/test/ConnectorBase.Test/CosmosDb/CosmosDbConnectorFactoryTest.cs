// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Azure.Cosmos;
using Steeltoe.Connector.Services;
using System;
using Xunit;

namespace Steeltoe.Connector.CosmosDb.Test
{
    public class CosmosDbConnectorFactoryTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            CosmosDbConnectorOptions config = null;
            CosmosDbServiceInfo si = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new CosmosDbConnectorFactory(si, config, typeof(CosmosClient)));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Create_ReturnsCosmosDbConnection()
        {
            var si = new CosmosDbServiceInfo("MyId")
            {
                Host = "https://someHost:443/",
                MasterKey = "lXYMGIE4mYITjXaHwQjkh0U07lwF513NdbTfeyGndeqjVXzwKQ3ZalKXQNYeIZovoyl57IY1J0KnJUH36EPufA==",
                ReadOnlyKey = "hy5XZOdVnBeMmbB9FGcD54tttGKExad9XkGhn5Esc4jAM60OF2U7TcCXgffqBtBRuPAp0uFqKvz1l13OX8auPw==",
                DatabaseId = "databaseId",
                DatabaseLink = "databaseLink"
            };

            var factory = new CosmosDbConnectorFactory(si, new CosmosDbConnectorOptions(), typeof(CosmosClient));
            var connection = factory.Create(null);
            Assert.NotNull(connection);
        }

        [Fact]
        public void Constructor_ThrowsIfConfigNull_v3()
        {
            // Arrange
            CosmosDbConnectorOptions config = null;

            CosmosDbServiceInfo si = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new CosmosDbConnectorFactory(si, config, typeof(Microsoft.Azure.Cosmos.CosmosClient)));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Create_ReturnsCosmosDbConnection_v3()
        {
            var optionsTypes = CosmosDbTypeLocator.ClientOptionsTypeNames;
            CosmosDbTypeLocator.ClientOptionsTypeNames = new string[] { CosmosDbTypeLocator.ClientOptionsTypeNames[1] };

            var si = new CosmosDbServiceInfo("MyId")
            {
                Host = "https://someHost:443/",
                MasterKey = "lXYMGIE4mYITjXaHwQjkh0U07lwF513NdbTfeyGndeqjVXzwKQ3ZalKXQNYeIZovoyl57IY1J0KnJUH36EPufA==",
                ReadOnlyKey = "hy5XZOdVnBeMmbB9FGcD54tttGKExad9XkGhn5Esc4jAM60OF2U7TcCXgffqBtBRuPAp0uFqKvz1l13OX8auPw==",
                DatabaseId = "databaseId",
                DatabaseLink = "databaseLink"
            };

            var factory = new CosmosDbConnectorFactory(si, new CosmosDbConnectorOptions(), typeof(Microsoft.Azure.Cosmos.CosmosClient));
            var connection = factory.Create(null);
            Assert.NotNull(connection);
            CosmosDbTypeLocator.ClientOptionsTypeNames = optionsTypes;
        }
    }
}
