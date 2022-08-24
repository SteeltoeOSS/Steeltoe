// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using System.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Extensions.Configuration.CloudFoundry;
using Xunit;

namespace Steeltoe.Connector.MySql.Test;

/// <summary>
/// Tests for the extension method that adds both the DbConnection and the health check.
/// </summary>
public class MySqlProviderServiceCollectionExtensionsTest
{
    public MySqlProviderServiceCollectionExtensionsTest()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public void AddMySqlConnection_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot configurationRoot = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddMySqlConnection(configurationRoot));
        Assert.Contains(nameof(services), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddMySqlConnection(configurationRoot, "foobar"));
        Assert.Contains(nameof(services), ex2.Message);
    }

    [Fact]
    public void AddMySqlConnection_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddMySqlConnection(configuration));
        Assert.Contains(nameof(configuration), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddMySqlConnection(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message);
    }

    [Fact]
    public void AddMySqlConnection_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configurationRoot = null;
        const string serviceName = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddMySqlConnection(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message);
    }

    [Fact]
    public void AddMySqlConnection_NoVCAPs_AddsMySqlConnection()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        services.AddMySqlConnection(configurationRoot);

        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
    }

    [Fact]
    public void AddMySqlConnection_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddMySqlConnection(configurationRoot, "foobar"));
        Assert.Contains("foobar", ex.Message);
    }

    [Fact]
    public void AddMySqlConnection_MultipleMySqlServices_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.TwoServerVcap);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var ex = Assert.Throws<ConnectorException>(() => services.AddMySqlConnection(configurationRoot));
        Assert.Contains("Multiple", ex.Message);
    }

    [Fact]
    public void AddMySqlConnection_WithServiceName_AndVCaps_AddsMySqlConnection()
    {
        IServiceCollection services = new ServiceCollection();
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.TwoServerVcap);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddMySqlConnection(configurationRoot, "spring-cloud-broker-db");
        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
        string connString = service.ConnectionString;
        Assert.Contains("Password=7E1LxXnlH2hhlPVt", connString, StringComparison.InvariantCultureIgnoreCase);
        Assert.Contains("Server=192.168.0.90;Port=3306", connString, StringComparison.InvariantCultureIgnoreCase);
        Assert.Contains("Database=cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", connString, StringComparison.InvariantCultureIgnoreCase);
        Assert.Contains("User Id=Dd6O1BPXUHdrmzbP", connString, StringComparison.InvariantCultureIgnoreCase);
    }

    [Fact]
    public void AddMySqlConnection_WithVCAPs_AddsMySqlConnection()
    {
        IServiceCollection services = new ServiceCollection();
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.SingleServerVcap);
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddMySqlConnection(configurationRoot);

        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
        string connString = service.ConnectionString;
        Assert.Contains("cf_b4f8d2fa_a3ea_4e3a_a0e8_2cd040790355", connString);
        Assert.Contains("3306", connString);
        Assert.Contains("192.168.0.90", connString);
        Assert.Contains("Dd6O1BPXUHdrmzbP", connString);
        Assert.Contains("7E1LxXnlH2hhlPVt", connString);
    }

    [Fact]
    public void AddMySqlConnection_WithAzureBrokerVCAPs_AddsMySqlConnection()
    {
        IServiceCollection services = new ServiceCollection();
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.SingleServerAzureVcap);
        var appsettings = new Dictionary<string, string>();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        builder.AddInMemoryCollection(appsettings);
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddMySqlConnection(configurationRoot);

        var service = services.BuildServiceProvider().GetService<IDbConnection>();
        Assert.NotNull(service);
        string connString = service.ConnectionString;
        Assert.Contains("ub6oyk1kkh", connString); // database
        Assert.Contains("3306", connString); // port
        Assert.Contains("451200b4-c29d-4346-9a0a-70bc109bb6e9.mysql.database.azure.com", connString); // host
        Assert.Contains("wj7tsxai7i@451200b4-c29d-4346-9a0a-70bc109bb6e9", connString); // user
        Assert.Contains("10PUO82Uhqk8F2ii", connString); // password
    }

    [Fact]
    public void AddMySqlConnection_AddsRelationalHealthContributor()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddMySqlConnection(configurationRoot);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

        Assert.NotNull(healthContributor);
    }

    [Fact]
    public void AddMySqlConnection_DoesNotAddRelationalHealthContributor_WhenCommunityHealthExists()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var cm = new ConnectionStringManager(configurationRoot);
        Connection ci = cm.Get<MySqlConnectionInfo>();
        services.AddHealthChecks().AddMySql(ci.ConnectionString, ci.Name);

        services.AddMySqlConnection(configurationRoot);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

        Assert.Null(healthContributor);
    }

    [Fact]
    public void AddMySqlConnection_AddsRelationalHealthContributor_WhenCommunityHealthExistsAndForced()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var cm = new ConnectionStringManager(configurationRoot);
        Connection ci = cm.Get<MySqlConnectionInfo>();
        services.AddHealthChecks().AddMySql(ci.ConnectionString, ci.Name);

        services.AddMySqlConnection(configurationRoot, addSteeltoeHealthChecks: true);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

        Assert.NotNull(healthContributor);
    }
}
