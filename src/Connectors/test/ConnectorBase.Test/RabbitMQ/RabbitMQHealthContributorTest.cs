// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.Services;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Steeltoe.Connector.RabbitMQ.Test;

public class RabbitMQHealthContributorTest
{
    [Fact]
    public void GetRabbitMQContributor_ReturnsContributor()
    {
        var appsettings = new Dictionary<string, string>
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
        Assert.Equal(HealthStatus.Down, status.Status);
    }

    [Fact]
    public void Not_Connected_Returns_Down_Status()
    {
        _ = RabbitMQTypeLocator.ConnectionFactoryInterface;
        var rabbitMQImplementationType = RabbitMQTypeLocator.ConnectionFactory;
        var rabbitMQConfig = new RabbitMQProviderConnectorOptions();
        var sInfo = new RabbitMQServiceInfo("MyId", "amqp://si_username:si_password@localhost:5672/si_vhost");
        var factory = new LoggerFactory();
        var connFactory = new RabbitMQProviderConnectorFactory(sInfo, rabbitMQConfig, rabbitMQImplementationType);
        var h = new RabbitMQHealthContributor(connFactory, factory.CreateLogger<RabbitMQHealthContributor>());

        var status = h.Health();

        Assert.Equal(HealthStatus.Down, status.Status);
        Assert.Equal("Failed to open RabbitMQ connection!", status.Description);
    }

    [Fact(Skip = "Integration test - Requires local RMQ server")]
    public void Is_Connected_Returns_Up_Status()
    {
        var rabbitMQImplementationType = RabbitMQTypeLocator.ConnectionFactory;
        var rabbitMQConfig = new RabbitMQProviderConnectorOptions();
        var sInfo = new RabbitMQServiceInfo("MyId", "amqp://localhost:5672");
        var factory = new LoggerFactory();
        var connFactory = new RabbitMQProviderConnectorFactory(sInfo, rabbitMQConfig, rabbitMQImplementationType);
        var h = new RabbitMQHealthContributor(connFactory, factory.CreateLogger<RabbitMQHealthContributor>());

        var status = h.Health();

        Assert.Equal(HealthStatus.Up, status.Status);
        Assert.Contains("version", status.Details.Keys);
    }

    [Fact]
    public void ScenarioTesting()
    {
        var mockConnection = new Mock<IConnection>();
        mockConnection.Setup(a => a.IsOpen).Returns(false);
        mockConnection.Setup(s => s.ServerProperties).Returns(new Dictionary<string, object> { { "version", Encoding.UTF8.GetBytes("test") } });
        var mockConnFactory = new Mock<ConnectionFactory>();
        mockConnFactory.Setup(s => s.CreateConnection()).Returns(mockConnection.Object);
        var mockProviderFactory = new Mock<RabbitMQProviderConnectorFactory>(null, new RabbitMQProviderConnectorOptions(), RabbitMQTypeLocator.ConnectionFactory);
        mockProviderFactory.Setup(a => a.Create(null)).Returns(mockConnFactory.Object);
        var h = new RabbitMQHealthContributor(mockProviderFactory.Object);

        // check health, get object that is closed
        var status = h.Health();
        Assert.Equal(HealthStatus.Down, status.Status);
        Assert.Equal("ConnectorException: RabbitMQ connection is closed!", status.Details["error"]);

        // check health, get object that is open
        mockConnection.Setup(a => a.IsOpen).Returns(true);
        status = h.Health();
        Assert.Equal(HealthStatus.Up, status.Status);
        Assert.Equal("test", status.Details["version"]);
    }
}
