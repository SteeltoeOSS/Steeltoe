// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.Placeholder;
using Xunit;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerHealthContributorTest
{
    [Fact]
    public void Constructor_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ConfigServerHealthContributor(null, NullLogger<ConfigServerHealthContributor>.Instance));
        Assert.Throws<ArgumentNullException>(() => new ConfigServerHealthContributor(new ConfigurationBuilder().Build(), null));
    }

    [Fact]
    public void Constructor_FindsConfigServerProvider()
    {
        var values = new Dictionary<string, string>(TestHelpers.FastTestsConfiguration)
        {
            { "spring:cloud:config:uri", "http://localhost:8888/" },
            { "spring:cloud:config:name", "myName" },
            { "spring:cloud:config:label", "myLabel" },
            { "spring:cloud:config:timeout", "10" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();
        builder.AddPlaceholderResolver();

        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, NullLogger<ConfigServerHealthContributor>.Instance);
        Assert.NotNull(contributor.Provider);
    }

    [Fact]
    public void FindProvider_FindsProvider()
    {
        var values = new Dictionary<string, string>(TestHelpers.FastTestsConfiguration)
        {
            { "spring:cloud:config:uri", "http://localhost:8888/" },
            { "spring:cloud:config:name", "myName" },
            { "spring:cloud:config:label", "myLabel" },
            { "spring:cloud:config:timeout", "10" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();

        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, NullLogger<ConfigServerHealthContributor>.Instance);
        Assert.NotNull(contributor.Provider);
    }

    [Fact]
    public void GetTimeToLive_ReturnsExpected()
    {
        var values = new Dictionary<string, string>(TestHelpers.FastTestsConfiguration)
        {
            { "spring:cloud:config:uri", "http://localhost:8888/" },
            { "spring:cloud:config:name", "myName" },
            { "spring:cloud:config:label", "myLabel" },
            { "spring:cloud:config:health:timeToLive", "100000" },
            { "spring:cloud:config:timeout", "10" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();

        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, NullLogger<ConfigServerHealthContributor>.Instance);
        Assert.Equal(100000, contributor.GetTimeToLive());
    }

    [Fact]
    public void IsEnabled_ReturnsExpected()
    {
        var values = new Dictionary<string, string>(TestHelpers.FastTestsConfiguration)
        {
            { "spring:cloud:config:uri", "http://localhost:8888/" },
            { "spring:cloud:config:name", "myName" },
            { "spring:cloud:config:label", "myLabel" },
            { "spring:cloud:config:health:enabled", "true" },
            { "spring:cloud:config:timeout", "10" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();

        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, NullLogger<ConfigServerHealthContributor>.Instance);
        Assert.True(contributor.IsEnabled());
    }

    [Fact]
    public void IsCacheStale_ReturnsExpected()
    {
        var values = new Dictionary<string, string>(TestHelpers.FastTestsConfiguration)
        {
            { "spring:cloud:config:uri", "http://localhost:8888/" },
            { "spring:cloud:config:name", "myName" },
            { "spring:cloud:config:label", "myLabel" },
            { "spring:cloud:config:health:enabled", "true" },
            { "spring:cloud:config:health:timeToLive", "1" },
            { "spring:cloud:config:timeout", "10" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();

        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, NullLogger<ConfigServerHealthContributor>.Instance);
        Assert.True(contributor.IsCacheStale(0)); // No cache established yet
        contributor.Cached = new ConfigEnvironment();
        contributor.LastAccess = 9;
        Assert.True(contributor.IsCacheStale(10));
        Assert.False(contributor.IsCacheStale(8));
    }

    [Fact]
    public void GetPropertySources_ReturnsExpected()
    {
        // this test does NOT expect to find a running Config Server
        var values = new Dictionary<string, string>(TestHelpers.FastTestsConfiguration)
        {
            { "spring:cloud:config:uri", "http://localhost:8887/" },
            { "spring:cloud:config:name", "myName" },
            { "spring:cloud:config:label", "myLabel" },
            { "spring:cloud:config:health:enabled", "true" },
            { "spring:cloud:config:health:timeToLive", "1" },
            { "spring:cloud:config:timeout", "1" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();

        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, NullLogger<ConfigServerHealthContributor>.Instance)
        {
            Cached = new ConfigEnvironment()
        };

        long lastAccess = contributor.LastAccess = DateTimeOffset.Now.ToUnixTimeMilliseconds() - 100;
        IList<PropertySource> sources = contributor.GetPropertySources();

        Assert.NotEqual(lastAccess, contributor.LastAccess);
        Assert.Null(sources);
        Assert.Null(contributor.Cached);
    }

    [Fact]
    public void Health_NoProvider_ReturnsExpected()
    {
        var values = new Dictionary<string, string>
        {
            { "spring:cloud:config:uri", "http://localhost:8888/" },
            { "spring:cloud:config:name", "myName" },
            { "spring:cloud:config:label", "myLabel" },
            { "spring:cloud:config:health:enabled", "true" },
            { "spring:cloud:config:health:timeToLive", "1" },
            { "spring:cloud:config:timeout", "10" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, NullLogger<ConfigServerHealthContributor>.Instance);
        Assert.Null(contributor.Provider);
        HealthCheckResult health = contributor.Health();
        Assert.NotNull(health);
        Assert.Equal(HealthStatus.Unknown, health.Status);
        Assert.True(health.Details.ContainsKey("error"));
    }

    [Fact]
    public void Health_NotEnabled_ReturnsExpected()
    {
        var values = new Dictionary<string, string>(TestHelpers.FastTestsConfiguration)
        {
            { "spring:cloud:config:uri", "http://localhost:8888/" },
            { "spring:cloud:config:name", "myName" },
            { "spring:cloud:config:label", "myLabel" },
            { "spring:cloud:config:health:enabled", "false" },
            { "spring:cloud:config:health:timeToLive", "1" },
            { "spring:cloud:config:timeout", "10" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();
        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, NullLogger<ConfigServerHealthContributor>.Instance);
        Assert.NotNull(contributor.Provider);
        HealthCheckResult health = contributor.Health();
        Assert.NotNull(health);
        Assert.Equal(HealthStatus.Unknown, health.Status);
    }

    [Fact]
    public void Health_NoPropertySources_ReturnsExpected()
    {
        // this test does NOT expect to find a running Config Server
        var values = new Dictionary<string, string>(TestHelpers.FastTestsConfiguration)
        {
            { "spring:cloud:config:uri", "http://localhost:8887/" },
            { "spring:cloud:config:name", "myName" },
            { "spring:cloud:config:label", "myLabel" },
            { "spring:cloud:config:health:enabled", "true" },
            { "spring:cloud:config:health:timeToLive", "1" },
            { "spring:cloud:config:timeout", "10" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();
        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, NullLogger<ConfigServerHealthContributor>.Instance);
        Assert.NotNull(contributor.Provider);
        HealthCheckResult health = contributor.Health();
        Assert.NotNull(health);
        Assert.Equal(HealthStatus.Unknown, health.Status);
        Assert.True(health.Details.ContainsKey("error"));
    }

    [Fact]
    public void UpdateHealth_WithPropertySources_ReturnsExpected()
    {
        var values = new Dictionary<string, string>(TestHelpers.FastTestsConfiguration)
        {
            { "spring:cloud:config:uri", "http://localhost:8888/" },
            { "spring:cloud:config:name", "myName" },
            { "spring:cloud:config:label", "myLabel" },
            { "spring:cloud:config:health:enabled", "true" },
            { "spring:cloud:config:health:timeToLive", "1" },
            { "spring:cloud:config:timeout", "10" }
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();
        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, NullLogger<ConfigServerHealthContributor>.Instance);
        var health = new HealthCheckResult();

        var sources = new List<PropertySource>
        {
            new("foo", new Dictionary<string, object>()),
            new("bar", new Dictionary<string, object>())
        };

        contributor.UpdateHealth(health, sources);

        Assert.Equal(HealthStatus.Up, health.Status);
        Assert.True(health.Details.ContainsKey("propertySources"));
        var names = health.Details["propertySources"] as IList<string>;
        Assert.NotNull(names);
        Assert.Contains("foo", names);
        Assert.Contains("bar", names);
    }
}
