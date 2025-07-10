// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.Placeholder;

namespace Steeltoe.Configuration.ConfigServer.Test;

public sealed class ConfigServerHealthContributorTest
{
    [Fact]
    public void Constructor_FindsConfigServerProviderInsidePlaceholderProvider()
    {
        var values = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer))
        {
            ["spring:cloud:config:uri"] = "http://localhost:8888/",
            ["spring:cloud:config:name"] = "myName",
            ["spring:cloud:config:label"] = "myLabel",
            ["spring:cloud:config:timeout"] = "10"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();
        builder.AddPlaceholderResolver();

        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, TimeProvider.System, NullLogger<ConfigServerHealthContributor>.Instance);
        contributor.Provider.Should().NotBeNull();
    }

    [Fact]
    public void FindProvider_FindsProvider()
    {
        var values = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer))
        {
            ["spring:cloud:config:uri"] = "http://localhost:8888/",
            ["spring:cloud:config:name"] = "myName",
            ["spring:cloud:config:label"] = "myLabel",
            ["spring:cloud:config:timeout"] = "10"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();

        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, TimeProvider.System, NullLogger<ConfigServerHealthContributor>.Instance);
        contributor.Provider.Should().NotBeNull();
    }

    [Fact]
    public void GetTimeToLive_ReturnsExpected()
    {
        var values = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer))
        {
            ["spring:cloud:config:uri"] = "http://localhost:8888/",
            ["spring:cloud:config:name"] = "myName",
            ["spring:cloud:config:label"] = "myLabel",
            ["spring:cloud:config:health:timeToLive"] = "100000",
            ["spring:cloud:config:timeout"] = "10"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();

        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, TimeProvider.System, NullLogger<ConfigServerHealthContributor>.Instance);
        contributor.GetTimeToLive().Should().Be(100_000);
    }

    [Fact]
    public void IsEnabled_ReturnsExpected()
    {
        var values = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer))
        {
            ["spring:cloud:config:uri"] = "http://localhost:8888/",
            ["spring:cloud:config:name"] = "myName",
            ["spring:cloud:config:label"] = "myLabel",
            ["spring:cloud:config:health:enabled"] = "true",
            ["spring:cloud:config:timeout"] = "10"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();

        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, TimeProvider.System, NullLogger<ConfigServerHealthContributor>.Instance);
        contributor.IsEnabled().Should().BeTrue();
    }

    [Fact]
    public void IsCacheStale_ReturnsExpected()
    {
        var values = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer))
        {
            ["spring:cloud:config:name"] = "myName",
            ["spring:cloud:config:label"] = "myLabel",
            ["spring:cloud:config:health:enabled"] = "true",
            ["spring:cloud:config:health:timeToLive"] = "1",
            ["spring:cloud:config:timeout"] = "10",
            ["spring:cloud:config:uri"] = "http://localhost:8888/"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();

        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, TimeProvider.System, NullLogger<ConfigServerHealthContributor>.Instance);
        contributor.IsCacheStale(0).Should().BeTrue(); // No cache established yet
        contributor.Cached = new ConfigEnvironment();
        contributor.LastAccess = 9;
        contributor.IsCacheStale(10).Should().BeTrue();
        contributor.IsCacheStale(8).Should().BeFalse();
    }

    [Fact]
    public async Task GetPropertySources_ReturnsExpected()
    {
        // this test does NOT expect to find a running Config Server
        var values = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer))
        {
            ["spring:cloud:config:uri"] = "http://localhost:8887/",
            ["spring:cloud:config:name"] = "myName",
            ["spring:cloud:config:label"] = "myLabel",
            ["spring:cloud:config:health:enabled"] = "true",
            ["spring:cloud:config:health:timeToLive"] = "1",
            ["spring:cloud:config:timeout"] = "1"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();

        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, TimeProvider.System, NullLogger<ConfigServerHealthContributor>.Instance)
        {
            Cached = new ConfigEnvironment()
        };

        long lastAccess = contributor.LastAccess = DateTimeOffset.Now.ToUnixTimeMilliseconds() - 100;
        IList<PropertySource>? sources = await contributor.GetPropertySourcesAsync(contributor.Provider!, TestContext.Current.CancellationToken);

        contributor.LastAccess.Should().NotBe(lastAccess);
        sources.Should().BeNull();
        contributor.Cached.Should().BeNull();
    }

    [Fact]
    public async Task Health_NoProvider_ReturnsExpected()
    {
        var values = new Dictionary<string, string?>
        {
            ["spring:cloud:config:uri"] = "http://localhost:8888/",
            ["spring:cloud:config:name"] = "myName",
            ["spring:cloud:config:label"] = "myLabel",
            ["spring:cloud:config:health:enabled"] = "true",
            ["spring:cloud:config:health:timeToLive"] = "1",
            ["spring:cloud:config:timeout"] = "10"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, TimeProvider.System, NullLogger<ConfigServerHealthContributor>.Instance);
        contributor.Provider.Should().BeNull();
        HealthCheckResult? health = await contributor.CheckHealthAsync(TestContext.Current.CancellationToken);
        health.Should().NotBeNull();
        health.Status.Should().Be(HealthStatus.Unknown);
        health.Details.Should().ContainKey("error");
    }

    [Fact]
    public async Task Health_NotEnabled_ReturnsExpected()
    {
        var values = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer))
        {
            ["spring:cloud:config:uri"] = "http://localhost:8888/",
            ["spring:cloud:config:name"] = "myName",
            ["spring:cloud:config:label"] = "myLabel",
            ["spring:cloud:config:health:enabled"] = "false",
            ["spring:cloud:config:health:timeToLive"] = "1",
            ["spring:cloud:config:timeout"] = "10"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();
        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, TimeProvider.System, NullLogger<ConfigServerHealthContributor>.Instance);
        contributor.Provider.Should().NotBeNull();
        HealthCheckResult? health = await contributor.CheckHealthAsync(TestContext.Current.CancellationToken);
        health.Should().BeNull();
    }

    [Fact]
    public async Task Health_NoPropertySources_ReturnsExpected()
    {
        // this test does NOT expect to find a running Config Server
        var values = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer))
        {
            ["spring:cloud:config:uri"] = "http://localhost:8887/",
            ["spring:cloud:config:name"] = "myName",
            ["spring:cloud:config:label"] = "myLabel",
            ["spring:cloud:config:health:enabled"] = "true",
            ["spring:cloud:config:health:timeToLive"] = "1",
            ["spring:cloud:config:timeout"] = "10"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();
        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, TimeProvider.System, NullLogger<ConfigServerHealthContributor>.Instance);
        contributor.Provider.Should().NotBeNull();
        HealthCheckResult? health = await contributor.CheckHealthAsync(TestContext.Current.CancellationToken);
        health.Should().NotBeNull();
        health.Status.Should().Be(HealthStatus.Unknown);
        health.Details.Should().ContainKey("error");
    }

    [Fact]
    public void UpdateHealth_WithPropertySources_ReturnsExpected()
    {
        var values = new Dictionary<string, string?>(TestSettingsFactory.Get(FastTestConfigurations.ConfigServer))
        {
            ["spring:cloud:config:uri"] = "http://localhost:8888/",
            ["spring:cloud:config:name"] = "myName",
            ["spring:cloud:config:label"] = "myLabel",
            ["spring:cloud:config:health:enabled"] = "true",
            ["spring:cloud:config:health:timeToLive"] = "1",
            ["spring:cloud:config:timeout"] = "10"
        };

        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(values);
        builder.AddConfigServer();
        IConfigurationRoot configurationRoot = builder.Build();

        var contributor = new ConfigServerHealthContributor(configurationRoot, TimeProvider.System, NullLogger<ConfigServerHealthContributor>.Instance);
        var health = new HealthCheckResult();

        List<PropertySource> sources =
        [
            new()
            {
                Name = "foo"
            },
            new()
            {
                Name = "bar"
            }
        ];

        contributor.UpdateHealth(health, sources);

        health.Status.Should().Be(HealthStatus.Up);
        List<string> names = health.Details.Should().ContainKey("propertySources").WhoseValue.Should().BeOfType<List<string>>().Subject;
        names.Should().Contain("foo");
        names.Should().Contain("bar");
    }
}
