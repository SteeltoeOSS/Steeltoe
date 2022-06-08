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

namespace Steeltoe.Connector.SqlServer.Test;

/// <summary>
/// Tests for the extension method that adds both the DbConnection and the health check
/// </summary>
public class SqlServerProviderServiceCollectionExtensionsTest
{
    public SqlServerProviderServiceCollectionExtensionsTest()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public void AddSqlServerConnection_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddSqlServerConnection(config));
        Assert.Contains(nameof(services), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddSqlServerConnection(config, "foobar"));
        Assert.Contains(nameof(services), ex2.Message);
    }

    [Fact]
    public void AddSqlServerConnection_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddSqlServerConnection(config));
        Assert.Contains(nameof(config), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddSqlServerConnection(config, "foobar"));
        Assert.Contains(nameof(config), ex2.Message);
    }

    [Fact]
    public void AddSqlServerConnection_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot config = null;
        const string serviceName = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddSqlServerConnection(config, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message);
    }

    [Fact]
    public void AddSqlServerConnection_NoVCAPs_AddsSqlServerConnection()
    {
        IServiceCollection services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        services.AddSqlServerConnection(config);

        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddSqlServerConnection_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddSqlServerConnection(config, "foobar"));
        Assert.Contains("foobar", ex.Message);
    }

    [Fact]
    public void AddSqlServerConnection_MultipleSqlServerServices_ThrowsConnectorException()
    {
        // Arrange an environment where multiple sql server services have been provisioned
        IServiceCollection services = new ServiceCollection();
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.TwoServerVCAP);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddSqlServerConnection(config));
        Assert.Contains("Multiple", ex.Message);
    }

    [Fact]
    public void AddSqlServerConnection_WithVCAPs_AddsSqlServerConnection()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVCAP);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        services.AddSqlServerConnection(config);

        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
        var connString = service.ConnectionString;
        Assert.Contains("de5aa3a747c134b3d8780f8cc80be519e", connString);
        Assert.Contains("1433", connString);
        Assert.Contains("192.168.0.80", connString);
        Assert.Contains("uf33b2b30783a4087948c30f6c3b0c90f", connString);
        Assert.Contains("Pefbb929c1e0945b5bab5b8f0d110c503", connString);
    }

    [Fact]
    public void AddSqlServerConnection_WithUserVCAP_AddsSqlServerConnection()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVCAPIgnoreName);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        services.AddSqlServerConnection(config);

        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
        var connString = service.ConnectionString;
        Assert.Contains("Initial Catalog=testdb", connString);
        Assert.Contains("1433", connString);
        Assert.Contains("Data Source=ajaganathansqlserver", connString);
    }

    [Fact]
    public void AddSqlServerConnection_WithAzureBrokerVCAPs_AddsSqlServerConnection()
    {
        IServiceCollection services = new ServiceCollection();
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VCAP_APPLICATION);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerAzureVCAP);
        var appsettings = new Dictionary<string, string>();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        builder.AddInMemoryCollection(appsettings);
        var config = builder.Build();

        services.AddSqlServerConnection(config);

        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
        var connString = service.ConnectionString;
        Assert.Contains("f1egl8ify4;", connString);                                                     // database
        Assert.Contains("fe049939-64f1-44f5-9f84-073ed5c82088.database.windows.net,1433", connString);  // host:port
        Assert.Contains("rgmm5zlri4;", connString);                                                     // user
        Assert.Contains("737mAU1pj6HcBxzw;", connString);                                               // password

        // other components of the url from the service broker should carry through to the connection string
        Assert.Contains("encrypt=true;", connString);
        Assert.Contains("trustServerCertificate=true", connString);
    }

    [Fact]
    public void AddSqlServerConnection_AddsRelationalHealthContributor()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        services.AddSqlServerConnection(config);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

        Assert.NotNull(healthContributor);
    }

    [Fact]
    public void AddSqlServerConnection_DoesntAddsRelationalHealthContributor_WhenCommunityHealthCheckExists()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        var cm = new ConnectionStringManager(config);
        var ci = cm.Get<SqlServerConnectionInfo>();
        services.AddHealthChecks().AddSqlServer(ci.ConnectionString, name: ci.Name);

        services.AddSqlServerConnection(config);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

        Assert.Null(healthContributor);
    }

    [Fact]
    public void AddSqlServerConnection_AddsRelationalHealthContributor_WhenCommunityHealthCheckExistsAndForced()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        var config = builder.Build();

        var cm = new ConnectionStringManager(config);
        var ci = cm.Get<SqlServerConnectionInfo>();
        services.AddHealthChecks().AddSqlServer(ci.ConnectionString, name: ci.Name);

        services.AddSqlServerConnection(config, addSteeltoeHealthChecks: true);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

        Assert.NotNull(healthContributor);
    }
}
