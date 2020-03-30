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

using Steeltoe.CloudFoundry.Connector.Services;
using System;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.CosmosDb.Test
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
            var ex = Assert.Throws<ArgumentNullException>(() => new CosmosDbConnectorFactory(si, config, typeof(Azure.Cosmos.CosmosClient)));
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

            var factory = new CosmosDbConnectorFactory(si, new CosmosDbConnectorOptions(), typeof(Azure.Cosmos.CosmosClient));
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
