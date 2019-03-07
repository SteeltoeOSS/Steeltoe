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
                //["mongodb:client:options:0:timeout"] = "1"
            };

            ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
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
            MongoDbConnectorOptions mongoDbConfig = new MongoDbConnectorOptions();
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
            MongoDbConnectorOptions mongoDbConfig = new MongoDbConnectorOptions();
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
