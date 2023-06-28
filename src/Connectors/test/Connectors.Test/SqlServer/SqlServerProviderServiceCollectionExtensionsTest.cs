// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Common.TestResources;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connectors.SqlServer;
using Xunit;

namespace Steeltoe.Connectors.Test.SqlServer;

/// <summary>
/// Tests for the extension method that adds both the DbConnection and the health check.
/// </summary>
public class SqlServerProviderServiceCollectionExtensionsTest
{
    [Fact]
    public void AddSqlServerConnection_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot configurationRoot = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddSqlServerConnection(configurationRoot));
        Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddSqlServerConnection(configurationRoot, "foobar"));
        Assert.Contains(nameof(services), ex2.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSqlServerConnection_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddSqlServerConnection(configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddSqlServerConnection(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSqlServerConnection_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configurationRoot = null;
        const string serviceName = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddSqlServerConnection(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSqlServerConnection_NoVCAPs_AddsSqlServerConnection()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        services.AddSqlServerConnection(configurationRoot);

        var service = services.BuildServiceProvider().GetService<DbConnection>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddSqlServerConnection_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddSqlServerConnection(configurationRoot, "foobar"));
        Assert.Contains("foobar", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSqlServerConnection_MultipleSqlServerServices_ThrowsConnectorException()
    {
        // Arrange an environment where multiple sql server services have been provisioned
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", SqlServerTestHelpers.TwoServerVcap);

        IServiceCollection services = new ServiceCollection();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddSqlServerConnection(configurationRoot));
        Assert.Contains("Multiple", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSqlServerConnection_WithVCAPs_AddsSqlServerConnection()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVcap);

        IServiceCollection services = new ServiceCollection();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddSqlServerConnection(configurationRoot);

        var service = services.BuildServiceProvider().GetService<DbConnection>();
        Assert.NotNull(service);
        string connString = service.ConnectionString;
        Assert.Contains("de5aa3a747c134b3d8780f8cc80be519e", connString, StringComparison.Ordinal);
        Assert.Contains("1433", connString, StringComparison.Ordinal);
        Assert.Contains("192.168.0.80", connString, StringComparison.Ordinal);
        Assert.Contains("uf33b2b30783a4087948c30f6c3b0c90f", connString, StringComparison.Ordinal);
        Assert.Contains("Pefbb929c1e0945b5bab5b8f0d110c503", connString, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSqlServerConnection_WithUserVCAP_AddsSqlServerConnection()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVcapIgnoreName);

        IServiceCollection services = new ServiceCollection();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddSqlServerConnection(configurationRoot);

        var service = services.BuildServiceProvider().GetService<DbConnection>();
        Assert.NotNull(service);
        string connString = service.ConnectionString;
        Assert.Contains("Initial Catalog=testdb", connString, StringComparison.Ordinal);
        Assert.Contains("1433", connString, StringComparison.Ordinal);
        Assert.Contains("Data Source=ajaganathansqlserver", connString, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSqlServerConnection_WithAzureBrokerVCAPs_AddsSqlServerConnection()
    {
        using var appScope = new EnvironmentVariableScope("VCAP_APPLICATION", TestHelpers.VcapApplication);
        using var servicesScope = new EnvironmentVariableScope("VCAP_SERVICES", SqlServerTestHelpers.SingleServerAzureVcap);

        IServiceCollection services = new ServiceCollection();

        var appsettings = new Dictionary<string, string>();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        builder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddSqlServerConnection(configurationRoot);

        var service = services.BuildServiceProvider().GetService<DbConnection>();
        Assert.NotNull(service);
        string connString = service.ConnectionString;
        Assert.Contains("f1egl8ify4;", connString, StringComparison.Ordinal); // database
        Assert.Contains("fe049939-64f1-44f5-9f84-073ed5c82088.database.windows.net,1433", connString, StringComparison.Ordinal); // host:port
        Assert.Contains("rgmm5zlri4;", connString, StringComparison.Ordinal); // user
        Assert.Contains("737mAU1pj6HcBxzw;", connString, StringComparison.Ordinal); // password

        // other components of the url from the service broker should carry through to the connection string
        Assert.Contains("encrypt=true;", connString, StringComparison.Ordinal);
        Assert.Contains("trustServerCertificate=true", connString, StringComparison.Ordinal);
    }

    [Fact]
    public void AddSqlServerConnection_AddsRelationalHealthContributor()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddLogging();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddSqlServerConnection(configurationRoot);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDatabaseHealthContributor;

        Assert.NotNull(healthContributor);
    }

    [Fact]
    public void AddSqlServerConnection_DoesNotAddsRelationalHealthContributor_WhenCommunityHealthCheckExists()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var cm = new ConnectionStringManager(configurationRoot);
        Connection ci = cm.Get<SqlServerConnectionInfo>();
        services.AddHealthChecks().AddSqlServer(ci.ConnectionString, name: ci.Name);

        services.AddSqlServerConnection(configurationRoot);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDatabaseHealthContributor;

        Assert.Null(healthContributor);
    }

    [Fact]
    public void AddSqlServerConnection_AddsRelationalHealthContributor_WhenCommunityHealthCheckExistsAndForced()
    {
        IServiceCollection services = new ServiceCollection();
        services.AddLogging();

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var cm = new ConnectionStringManager(configurationRoot);
        Connection ci = cm.Get<SqlServerConnectionInfo>();
        services.AddHealthChecks().AddSqlServer(ci.ConnectionString, name: ci.Name);

        services.AddSqlServerConnection(configurationRoot, addSteeltoeHealthChecks: true);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDatabaseHealthContributor;

        Assert.NotNull(healthContributor);
    }
}
