// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.CosmosDb;
using Steeltoe.Connector.Services;
using Xunit;

namespace Steeltoe.Connector.Redis.Test;

public class RedisHealthContributorTest
{
    [Fact]
    public void GetRedisContributor_ReturnsContributor()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["redis:client:host"] = "localhost",
            ["redis:client:port"] = "1234",
            ["redis:client:connectTimeout"] = "1"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = configurationBuilder.Build();
        IHealthContributor contrib = RedisHealthContributor.GetRedisContributor(configurationRoot);
        Assert.NotNull(contrib);
        HealthCheckResult status = contrib.Health();
        Assert.Equal(HealthStatus.Down, status.Status);
    }

    [Fact]
    public void StackExchange_Not_Connected_Returns_Down_Status()
    {
        var options = new RedisCacheConnectorOptions
        {
            ConnectTimeout = 1
        };

        var sInfo = new RedisServiceInfo("MyId", "redis://localhost:6378");
        var factory = new LoggerFactory();

        var connFactory = new RedisServiceConnectorFactory(sInfo, options, RedisTypeLocator.StackExchangeImplementation, RedisTypeLocator.StackExchangeOptions,
            RedisTypeLocator.StackExchangeInitializer);

        var h = new RedisHealthContributor(connFactory, RedisTypeLocator.StackExchangeImplementation, factory.CreateLogger<RedisHealthContributor>());

        HealthCheckResult status = h.Health();

        Assert.Equal(HealthStatus.Down, status.Status);
        Assert.Equal("Redis health check failed", status.Description);
    }

    [Fact(Skip = "Integration test - Requires local server")]
    public void StackExchange_Is_Connected_Returns_Up_Status()
    {
        var options = new RedisCacheConnectorOptions();
        var sInfo = new RedisServiceInfo("MyId", "redis://localhost:6379");
        var factory = new LoggerFactory();

        var connFactory = new RedisServiceConnectorFactory(sInfo, options, RedisTypeLocator.StackExchangeImplementation, RedisTypeLocator.StackExchangeOptions,
            RedisTypeLocator.StackExchangeInitializer);

        var h = new RedisHealthContributor(connFactory, RedisTypeLocator.StackExchangeImplementation, factory.CreateLogger<RedisHealthContributor>());

        HealthCheckResult status = h.Health();

        Assert.Equal(HealthStatus.Up, status.Status);
    }

    [Fact]
    public void Microsoft_Not_Connected_Returns_Down_Status()
    {
        var options = new RedisCacheConnectorOptions
        {
            ConnectTimeout = 1
        };

        var sInfo = new RedisServiceInfo("MyId", "redis://localhost:6378");
        var factory = new LoggerFactory();

        var connFactory = new RedisServiceConnectorFactory(sInfo, options, RedisTypeLocator.MicrosoftImplementation, RedisTypeLocator.MicrosoftOptions, null);

        var h = new RedisHealthContributor(connFactory, RedisTypeLocator.MicrosoftImplementation, factory.CreateLogger<RedisHealthContributor>());

        HealthCheckResult status = h.Health();

        Assert.Equal(HealthStatus.Down, status.Status);
        Assert.Equal("Redis health check failed", status.Description);
    }

    [Fact(Skip = "Integration test - Requires local server")]
    public void Microsoft_Is_Connected_Returns_Up_Status()
    {
        var options = new RedisCacheConnectorOptions();
        var sInfo = new RedisServiceInfo("MyId", "redis://localhost:6379");
        var factory = new LoggerFactory();

        var connFactory = new RedisServiceConnectorFactory(sInfo, options, RedisTypeLocator.MicrosoftImplementation, RedisTypeLocator.MicrosoftOptions, null);

        var h = new RedisHealthContributor(connFactory, RedisTypeLocator.MicrosoftImplementation, factory.CreateLogger<RedisHealthContributor>());

        HealthCheckResult status = h.Health();

        Assert.Equal(HealthStatus.Up, status.Status);
    }
}
