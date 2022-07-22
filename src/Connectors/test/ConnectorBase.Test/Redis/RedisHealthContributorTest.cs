// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.Services;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.Redis.Test;

public class RedisHealthContributorTest
{
    [Fact]
    public void GetRedisContributor_ReturnsContributor()
    {
        var appsettings = new Dictionary<string, string>()
        {
            ["redis:client:host"] = "localhost",
            ["redis:client:port"] = "1234",
            ["redis:client:connectTimeout"] = "1"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();
        var contrib = RedisHealthContributor.GetRedisContributor(config);
        Assert.NotNull(contrib);
        var status = contrib.Health();
        Assert.Equal(HealthStatus.DOWN, status.Status);
    }

    [Fact]
    public void StackExchange_Not_Connected_Returns_Down_Status()
    {
        var redisOptions = new RedisCacheConnectorOptions() { ConnectTimeout = 1 };
        var sInfo = new RedisServiceInfo("MyId", "redis://localhost:6378");
        var logrFactory = new LoggerFactory();
        var connFactory = new RedisServiceConnectorFactory(sInfo, redisOptions, RedisTypeLocator.StackExchangeImplementation, RedisTypeLocator.StackExchangeOptions, RedisTypeLocator.StackExchangeInitializer);
        var h = new RedisHealthContributor(connFactory, RedisTypeLocator.StackExchangeImplementation, logrFactory.CreateLogger<RedisHealthContributor>());

        var status = h.Health();

        Assert.Equal(HealthStatus.DOWN, status.Status);
        Assert.Equal("Redis health check failed", status.Description);
    }

    [Fact(Skip = "Integration test - Requires local server")]
    public void StackExchange_Is_Connected_Returns_Up_Status()
    {
        var redisOptions = new RedisCacheConnectorOptions();
        var sInfo = new RedisServiceInfo("MyId", "redis://localhost:6379");
        var logrFactory = new LoggerFactory();
        var connFactory = new RedisServiceConnectorFactory(sInfo, redisOptions, RedisTypeLocator.StackExchangeImplementation, RedisTypeLocator.StackExchangeOptions, RedisTypeLocator.StackExchangeInitializer);
        var h = new RedisHealthContributor(connFactory, RedisTypeLocator.StackExchangeImplementation, logrFactory.CreateLogger<RedisHealthContributor>());

        var status = h.Health();

        Assert.Equal(HealthStatus.UP, status.Status);
    }

    [Fact]
    public void Microsoft_Not_Connected_Returns_Down_Status()
    {
        var redisOptions = new RedisCacheConnectorOptions() { ConnectTimeout = 1 };
        var sInfo = new RedisServiceInfo("MyId", "redis://localhost:6378");
        var logrFactory = new LoggerFactory();
        var connFactory = new RedisServiceConnectorFactory(sInfo, redisOptions, RedisTypeLocator.MicrosoftImplementation, RedisTypeLocator.MicrosoftOptions, null);
        var h = new RedisHealthContributor(connFactory, RedisTypeLocator.MicrosoftImplementation, logrFactory.CreateLogger<RedisHealthContributor>());

        var status = h.Health();

        Assert.Equal(HealthStatus.DOWN, status.Status);
        Assert.Equal("Redis health check failed", status.Description);
    }

    [Fact(Skip = "Integration test - Requires local server")]
    public void Microsoft_Is_Connected_Returns_Up_Status()
    {
        var redisOptions = new RedisCacheConnectorOptions();
        var sInfo = new RedisServiceInfo("MyId", "redis://localhost:6379");
        var logrFactory = new LoggerFactory();
        var connFactory = new RedisServiceConnectorFactory(sInfo, redisOptions, RedisTypeLocator.MicrosoftImplementation, RedisTypeLocator.MicrosoftOptions, null);
        var h = new RedisHealthContributor(connFactory, RedisTypeLocator.MicrosoftImplementation, logrFactory.CreateLogger<RedisHealthContributor>());

        var status = h.Health();

        Assert.Equal(HealthStatus.UP, status.Status);
    }
}