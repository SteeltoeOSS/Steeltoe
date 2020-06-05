// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

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
