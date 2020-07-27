// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.CloudFoundry.Connector.Services;
using Steeltoe.Common.HealthChecks;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.CloudFoundry.Connector.MongoDb.Test
{
    public class MongoDbHealthContributorTest
    {
        private readonly Type mongoDbImplementationType = MongoDbTypeLocator.MongoClient;

        [Fact]
        public void GetMongoDbContributor_ReturnsContributor()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["mongodb:client:server"] = "localhost",
                ["mongodb:client:port"] = "27018",
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();
            var contrib = MongoDbHealthContributor.GetMongoDbHealthContributor(config);
            Assert.NotNull(contrib);
            var status = contrib.Health();
            Assert.Equal(HealthStatus.DOWN, status.Status);
        }

        [Fact]
        public void Not_Connected_Returns_Down_Status()
        {
            // arrange
            var mongoDbConfig = new MongoDbConnectorOptions();
            var sInfo = new MongoDbServiceInfo("MyId", "mongodb://localhost:27018");
            var logrFactory = new LoggerFactory();
            var connFactory = new MongoDbConnectorFactory(sInfo, mongoDbConfig, mongoDbImplementationType);
            var h = new MongoDbHealthContributor(connFactory, logrFactory.CreateLogger<MongoDbHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.DOWN, status.Status);
            Assert.Equal("Failed to open MongoDb connection!", status.Description);
        }

        [Fact(Skip = "Integration test - Requires local MongoDb server")]
        public void Is_Connected_Returns_Up_Status()
        {
            // arrange
            var mongoDbConfig = new MongoDbConnectorOptions();
            var sInfo = new MongoDbServiceInfo("MyId", "mongodb://localhost:27017");
            var logrFactory = new LoggerFactory();
            var connFactory = new MongoDbConnectorFactory(sInfo, mongoDbConfig, mongoDbImplementationType);
            var h = new MongoDbHealthContributor(connFactory, logrFactory.CreateLogger<MongoDbHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.UP, status.Status);
        }
    }
}
