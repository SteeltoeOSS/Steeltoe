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

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.RabbitMQ;
using Steeltoe.Connector.Services;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.Queue.Test
{
    public class RabbitMQHealthContributorTest
    {
        [Fact]
        public void GetRabbitMQContributor_ReturnsContributor()
        {
            var appsettings = new Dictionary<string, string>()
            {
                ["rabbit:client:server"] = "localhost",
                ["rabbit:client:port"] = "1234",
            };

            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(appsettings);
            var config = configurationBuilder.Build();
            var contrib = RabbitMQHealthContributor.GetRabbitMQContributor(config);
            Assert.NotNull(contrib);
            var status = contrib.Health();
            Assert.Equal(HealthStatus.DOWN, status.Status);
        }

        [Fact]
        public void Not_Connected_Returns_Down_Status()
        {
            // arrange
            _ = RabbitMQTypeLocator.IConnectionFactory;
            var rabbitMQImplementationType = RabbitMQTypeLocator.ConnectionFactory;
            var rabbitMQConfig = new RabbitMQProviderConnectorOptions();
            var sInfo = new RabbitMQServiceInfo("MyId", "amqp://si_username:si_password@localhost:5672/si_vhost");
            var logrFactory = new LoggerFactory();
            var connFactory = new RabbitMQProviderConnectorFactory(sInfo, rabbitMQConfig, rabbitMQImplementationType);
            var h = new RabbitMQHealthContributor(connFactory, logrFactory.CreateLogger<RabbitMQHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.DOWN, status.Status);
            Assert.Equal("Failed to open RabbitMQ connection!", status.Description);
        }

        [Fact(Skip = "Integration test - Requires local RMQ server")]
        public void Is_Connected_Returns_Up_Status()
        {
            // arrange
            _ = RabbitMQTypeLocator.IConnectionFactory;
            var rabbitMQImplementationType = RabbitMQTypeLocator.ConnectionFactory;
            var rabbitMQConfig = new RabbitMQProviderConnectorOptions();
            var sInfo = new RabbitMQServiceInfo("MyId", "amqp://localhost:5672");
            var logrFactory = new LoggerFactory();
            var connFactory = new RabbitMQProviderConnectorFactory(sInfo, rabbitMQConfig, rabbitMQImplementationType);
            var h = new RabbitMQHealthContributor(connFactory, logrFactory.CreateLogger<RabbitMQHealthContributor>());

            // act
            var status = h.Health();

            // assert
            Assert.Equal(HealthStatus.UP, status.Status);
        }
    }
}
