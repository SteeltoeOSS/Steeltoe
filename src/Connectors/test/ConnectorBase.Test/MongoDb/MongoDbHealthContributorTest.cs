// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.Services;
using System;
using System.Collections.Generic;
using Xunit;

namespace Steeltoe.Connector.MongoDb.Test;

public class MongoDbHealthContributorTest
{
    private readonly Type _mongoDbImplementationType = MongoDbTypeLocator.MongoClient;

    [Fact]
    public void GetMongoDbContributor_ReturnsContributor()
    {
        var appsettings = new Dictionary<string, string>
        {
            ["mongodb:client:server"] = "localhost",
            ["mongodb:client:port"] = "27018",
            ["mongodb:client:options:connecttimeoutms"] = "1"
        };

        var configurationBuilder = new ConfigurationBuilder();
        configurationBuilder.AddInMemoryCollection(appsettings);
        var config = configurationBuilder.Build();
        var contrib = MongoDbHealthContributor.GetMongoDbHealthContributor(config);
        Assert.NotNull(contrib);
        var status = contrib.Health();
        Assert.Equal(HealthStatus.Down, status.Status);
    }

    [Fact]
    public void Not_Connected_Returns_Down_Status()
    {
        var mongoDbConfig = new MongoDbConnectorOptions();
        var sInfo = new MongoDbServiceInfo("MyId", "mongodb://localhost:27018");
        var loggerFactory = new LoggerFactory();
        var connFactory = new MongoDbConnectorFactory(sInfo, mongoDbConfig, _mongoDbImplementationType);
        var h = new MongoDbHealthContributor(connFactory, loggerFactory.CreateLogger<MongoDbHealthContributor>(), 1);

        var status = h.Health();

        Assert.Equal(HealthStatus.Down, status.Status);
        Assert.Equal("Failed to open MongoDb connection!", status.Description);
    }

    [Fact(Skip = "Integration test - Requires local MongoDb server")]
    public void Is_Connected_Returns_Up_Status()
    {
        var mongoDbConfig = new MongoDbConnectorOptions();
        var sInfo = new MongoDbServiceInfo("MyId", "mongodb://localhost:27017");
        var loggerFactory = new LoggerFactory();
        var connFactory = new MongoDbConnectorFactory(sInfo, mongoDbConfig, _mongoDbImplementationType);
        var h = new MongoDbHealthContributor(connFactory, loggerFactory.CreateLogger<MongoDbHealthContributor>());

        var status = h.Health();

        Assert.Equal(HealthStatus.Up, status.Status);
    }
}
