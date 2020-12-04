// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Steeltoe.Discovery.Client.SimpleClients;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Discovery.ClientBase.Test
{
    public class NoOpDiscoveryClientTest
    {
        [Fact]
        public void NoOpDiscoveryClient_LooksForOtherClients()
        {
            // Arrange
            var appsettings = new Dictionary<string, string>()
            {
                { "spring:application:name", "myName" },
                { "Consul:somekey", "somevalue" },
                { "Eureka:somekey", "somevalue" },
                { "spring:cloud:kubernetes:discovery:somekey", "somevalue" },
                { "DiscoveryClients:TestClient", "TestClientConfigPath" },
                { "TestClientConfigPath:Uri", "http://someserver:1234" }
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(appsettings).Build();
            var logger = new Mock<ILogger<NoOpDiscoveryClient>>();

            // Act
            var client = new NoOpDiscoveryClient(config, logger.Object);

            // assert
            VerifyLogEntered(logger, LogLevel.Warning, "Found configuration values for TestClient, try adding a NuGet reference that enables TestClient to work with Steeltoe Discovery");
            foreach (var c in new List<string> { "Consul", "Eureka", "Kubernetes"})
            {
                VerifyLogEntered(logger, LogLevel.Warning, $"Found configuration values for {c}, try adding a NuGet reference to Steeltoe.Discovery.{c}");
            }
        }

        private void VerifyLogEntered(Mock<ILogger<NoOpDiscoveryClient>> logger, LogLevel level, string logEntry)
        {
            logger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == level),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString() == logEntry),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)));
        }
    }
}
