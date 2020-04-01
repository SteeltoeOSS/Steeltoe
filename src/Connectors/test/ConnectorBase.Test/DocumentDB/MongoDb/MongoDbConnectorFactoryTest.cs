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

using Steeltoe.Connector.Services;
using System;
using Xunit;

namespace Steeltoe.Connector.MongoDb.Test
{
    public class MongoDbConnectorFactoryTest
    {
        [Fact]
        public void Constructor_ThrowsIfConfigNull()
        {
            // Arrange
            MongoDbConnectorOptions config = null;
            MongoDbServiceInfo si = null;

            // Act and Assert
            var ex = Assert.Throws<ArgumentNullException>(() => new MongoDbConnectorFactory(si, config, MongoDbTypeLocator.MongoClient));
            Assert.Contains(nameof(config), ex.Message);
        }

        [Fact]
        public void Create_ReturnsMongoDbConnection()
        {
            var config = new MongoDbConnectorOptions()
            {
                Server = "localhost",
                Port = 27016,
                Password = "password",
                Username = "username",
            };
            var si = new MongoDbServiceInfo("MyId", "mongodb://localhost:27017");
            var factory = new MongoDbConnectorFactory(si, config, MongoDbTypeLocator.MongoClient);
            var connection = factory.Create(null);
            Assert.NotNull(connection);
        }
    }
}
