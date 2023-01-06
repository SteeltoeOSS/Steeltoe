// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connector.PostgreSql;
using Xunit;

namespace Steeltoe.Connector.Test.PostgreSQL;

/// <summary>
/// Tests for the extension method that adds both the DbConnection and the health check.
/// </summary>
public class PostgreSqlProviderServiceCollectionExtensionsTest
{
    public PostgreSqlProviderServiceCollectionExtensionsTest()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public void AddPostgreSqlConnection_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot configurationRoot = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddPostgreSqlConnection(configurationRoot));
        Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddPostgreSqlConnection(configurationRoot, "foobar"));
        Assert.Contains(nameof(services), ex2.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddPostgreSqlConnection_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddPostgreSqlConnection(configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddPostgreSqlConnection(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddPostgreSqlConnection_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configurationRoot = null;
        const string serviceName = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddPostgreSqlConnection(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddPostgreSqlConnection_NoVCAPs_AddsPostgreSqlConnection()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        services.AddPostgreSqlConnection(configurationRoot);

        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddPostgreSqlConnection_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddPostgreSqlConnection(configurationRoot, "foobar"));
        Assert.Contains("foobar", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddPostgreSqlConnection_MultiplePostgreSqlServices_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgreSqlTestHelpers.TwoServerVcapEdb);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddPostgreSqlConnection(configurationRoot));
        Assert.Contains("Multiple", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddPostgreSqlConnection_WithVCAPs_AddsPostgreSqlConnection()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgreSqlTestHelpers.SingleServerVcapEdb);
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddPostgreSqlConnection(configurationRoot);

        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
        string connString = service.ConnectionString;
        Assert.Contains("1e9e5dae-ed26-43e7-abb4-169b4c3beaff", connString, StringComparison.Ordinal);
        Assert.Contains("5432", connString, StringComparison.Ordinal);
        Assert.Contains("postgres.testcloud.com", connString, StringComparison.Ordinal);
        Assert.Contains("lmu7c96mgl99b2t1hvdgd5q94v", connString, StringComparison.Ordinal);
        Assert.Contains("1e9e5dae-ed26-43e7-abb4-169b4c3beaff", connString, StringComparison.Ordinal);
    }

    [Fact]
    public void AddPostgreSqlConnection_WithAzureVCAPs_AddsPostgreSqlConnection()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgreSqlTestHelpers.SingleServerVcapAzure);
        var appsettings = new Dictionary<string, string>();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        builder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddPostgreSqlConnection(configurationRoot);

        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
        string connString = service.ConnectionString;
        Assert.Contains("Host=2980cfbe-e198-46fd-8f81-966584bb4678.postgres.database.azure.com;", connString, StringComparison.Ordinal);
        Assert.Contains("Port=5432;", connString, StringComparison.Ordinal);
        Assert.Contains("Database=g01w0qnrb7;", connString, StringComparison.Ordinal);

        Assert.Contains("Username=c2cdhwt4nd@2980cfbe-e198-46fd-8f81-966584bb4678@2980cfbe-e198-46fd-8f81-966584bb4678.postgres.database.azure.com;",
            connString, StringComparison.Ordinal);

        Assert.Contains("Password=Dko4PGJAsQyEj5gj;", connString, StringComparison.Ordinal);
    }

    [Fact]
    public void AddPostgreSqlConnection_WithCrunchyVCAPs_AddsPostgreSqlConnection()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgreSqlTestHelpers.SingleServerVcapCrunchy);
        var appsettings = new Dictionary<string, string>();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        builder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddPostgreSqlConnection(configurationRoot);

        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
        string connString = service.ConnectionString;
        Assert.Contains("Host=10.194.45.174;", connString, StringComparison.Ordinal);
        Assert.Contains("Port=5432;", connString, StringComparison.Ordinal);
        Assert.Contains("Database=postgresample;", connString, StringComparison.Ordinal);
        Assert.Contains("Username=steeltoe7b59f5b8a34bce2a3cf873061cfb5815;", connString, StringComparison.Ordinal);
        Assert.Contains("Password=!DQ4Wm!r4omt$h1929!$;", connString, StringComparison.Ordinal);
        Assert.Contains("sslmode=Require;", connString, StringComparison.Ordinal);
        Assert.Contains("pooling=true", connString, StringComparison.Ordinal);
    }

    [Fact]
    public void AddPostgreSqlConnection_AddsRelationalHealthContributor()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddPostgreSqlConnection(configurationRoot);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

        Assert.NotNull(healthContributor);
    }

    [Fact]
    public void AddPostgreSqlConnection_DoesNotAddRelationalHealthContributor_WhenCommunityHealthCheckExists()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var cm = new ConnectionStringManager(configurationRoot);
        Connection ci = cm.Get<PostgreSqlConnectionInfo>();
        services.AddHealthChecks().AddNpgSql(ci.ConnectionString, name: ci.Name);

        services.AddPostgreSqlConnection(configurationRoot);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

        Assert.Null(healthContributor);
    }

    [Fact]
    public void AddPostgreSqlConnection_AddsRelationalHealthContributor_WhenCommunityHealthCheckExistsAndForced()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var cm = new ConnectionStringManager(configurationRoot);
        Connection ci = cm.Get<PostgreSqlConnectionInfo>();
        services.AddHealthChecks().AddNpgSql(ci.ConnectionString, name: ci.Name);

        services.AddPostgreSqlConnection(configurationRoot, addSteeltoeHealthChecks: true);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

        Assert.NotNull(healthContributor);
    }
}
