// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Connector.PostgreSql.Test;

/// <summary>
/// Tests for the extension method that adds just the health check.
/// </summary>
public class PostgreSqlServiceCollectionExtensionsTest
{
    public PostgreSqlServiceCollectionExtensionsTest()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public void AddPostgreSqlHealthContributor_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot configurationRoot = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddPostgresHealthContributor(configurationRoot));
        Assert.Contains(nameof(services), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddPostgresHealthContributor(configurationRoot, "foobar"));
        Assert.Contains(nameof(services), ex2.Message);
    }

    [Fact]
    public void AddPostgreSqlHealthContributor_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddPostgresHealthContributor(configuration));
        Assert.Contains(nameof(configuration), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddPostgresHealthContributor(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message);
    }

    [Fact]
    public void AddPostgreSqlHealthContributor_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configurationRoot = null;
        const string serviceName = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddPostgresHealthContributor(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message);
    }

    [Fact]
    public void AddPostgreSqlHealthContributor_NoVCAPs_AddsIHealthContributor()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        services.AddPostgresHealthContributor(configurationRoot);

        var service = services.BuildServiceProvider().GetService<IHealthContributor>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddPostgreSqlHealthContributor_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddPostgresHealthContributor(configurationRoot, "foobar"));
        Assert.Contains("foobar", ex.Message);
    }

    [Fact]
    public void AddPostgreSqlHealthContributor_AddsRelationalHealthContributor()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddPostgresHealthContributor(configurationRoot);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

        Assert.NotNull(healthContributor);
    }
}
