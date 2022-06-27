// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using System;
using System.Collections.Generic;
using System.Data;
using Xunit;

namespace Steeltoe.Connector.PostgreSql.Test;

/// <summary>
/// Tests for the extension method that adds both the DbConnection and the health check.
/// </summary>
public class PostgresProviderServiceCollectionExtensionsTest
{
    public PostgresProviderServiceCollectionExtensionsTest()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public void AddPostgresConnection_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddPostgresConnection(config));
        Assert.Contains(nameof(services), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddPostgresConnection(config, "foobar"));
        Assert.Contains(nameof(services), ex2.Message);
    }

    [Fact]
    public void AddPostgresConnection_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddPostgresConnection(config));
        Assert.Contains(nameof(config), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddPostgresConnection(config, "foobar"));
        Assert.Contains(nameof(config), ex2.Message);
    }

    [Fact]
    public void AddPostgresConnection_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot config = null;
        const string serviceName = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddPostgresConnection(config, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message);
    }

    [Fact]
    public void AddPostgresConnection_NoVCAPs_AddsPostgresConnection()
    {
        IServiceCollection services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddPostgresConnection(config);

        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddPostgresConnection_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddPostgresConnection(config, "foobar"));
        Assert.Contains("foobar", ex.Message);
    }

    [Fact]
    public void AddPostgresConnection_MultiplePostgresServices_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.TwoServerVCAP_EDB);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddPostgresConnection(config));
        Assert.Contains("Multiple", ex.Message);
    }

    [Fact]
    public void AddPostgresConnection_WithVCAPs_AddsPostgresConnection()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.SingleServerVCAP_EDB);
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        services.AddPostgresConnection(config);

        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
        var connString = service.ConnectionString;
        Assert.Contains("1e9e5dae-ed26-43e7-abb4-169b4c3beaff", connString);
        Assert.Contains("5432", connString);
        Assert.Contains("postgres.testcloud.com", connString);
        Assert.Contains("lmu7c96mgl99b2t1hvdgd5q94v", connString);
        Assert.Contains("1e9e5dae-ed26-43e7-abb4-169b4c3beaff", connString);
    }

    [Fact]
    public void AddPostgresConnection_WithAzureVCAPs_AddsPostgresConnection()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.SingleServerVCAP_Azure);
        var appsettings = new Dictionary<string, string>();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        builder.AddInMemoryCollection(appsettings);
        var config = builder.Build();

        services.AddPostgresConnection(config);

        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
        var connString = service.ConnectionString;
        Assert.Contains("Host=2980cfbe-e198-46fd-8f81-966584bb4678.postgres.database.azure.com;", connString);
        Assert.Contains("Port=5432;", connString);
        Assert.Contains("Database=g01w0qnrb7;", connString);
        Assert.Contains("Username=c2cdhwt4nd@2980cfbe-e198-46fd-8f81-966584bb4678;", connString);
        Assert.Contains("Password=Dko4PGJAsQyEj5gj;", connString);
    }

    [Fact]
    public void AddPostgresConnection_WithCruncyVCAPs_AddsPostgresConnection()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", PostgresTestHelpers.SingleServerVCAP_Crunchy);
        var appsettings = new Dictionary<string, string>();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        builder.AddInMemoryCollection(appsettings);
        var config = builder.Build();

        services.AddPostgresConnection(config);

        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
        var connString = service.ConnectionString;
        Assert.Contains("Host=10.194.45.174;", connString);
        Assert.Contains("Port=5432;", connString);
        Assert.Contains("Database=postgresample;", connString);
        Assert.Contains("Username=steeltoe7b59f5b8a34bce2a3cf873061cfb5815;", connString);
        Assert.Contains("Password=!DQ4Wm!r4omt$h1929!$;", connString);
        Assert.Contains("sslmode=Require;", connString);
        Assert.Contains("pooling=true;", connString);
    }

    [Fact]
    public void AddPosgreSqlConnection_AddsRelationalHealthContributor()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        services.AddPostgresConnection(config);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

        Assert.NotNull(healthContributor);
    }

    [Fact]
    public void AddPosgreSqlConnection_DoesntAddRelationalHealthContributor_WhenCommunityHealthCheckExists()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        var cm = new ConnectionStringManager(config);
        var ci = cm.Get<PostgresConnectionInfo>();
        services.AddHealthChecks().AddNpgSql(ci.ConnectionString, name: ci.Name);

        services.AddPostgresConnection(config);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

        Assert.Null(healthContributor);
    }

    [Fact]
    public void AddPosgreSqlConnection_AddsRelationalHealthContributor_WhenCommunityHealthCheckExistsAndForced()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        var cm = new ConnectionStringManager(config);
        var ci = cm.Get<PostgresConnectionInfo>();
        services.AddHealthChecks().AddNpgSql(ci.ConnectionString, name: ci.Name);

        services.AddPostgresConnection(config, addSteeltoeHealthChecks: true);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

        Assert.NotNull(healthContributor);
    }
}
