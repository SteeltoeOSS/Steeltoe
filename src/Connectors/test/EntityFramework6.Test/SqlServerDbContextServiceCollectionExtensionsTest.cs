// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connector.SqlServer.EntityFramework6;
using Xunit;

namespace Steeltoe.Connector.EntityFramework6.Test;

public class SqlServerDbContextServiceCollectionExtensionsTest
{
    public SqlServerDbContextServiceCollectionExtensionsTest()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public void AddDbContext_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot configurationRoot = null;

        var ex = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, configurationRoot));
        Assert.Contains(nameof(services), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, configurationRoot, "foobar"));
        Assert.Contains(nameof(services), ex2.Message);
    }

    [Fact]
    public void AddDbContext_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, configuration));
        Assert.Contains(nameof(configuration), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message);
    }

    [Fact]
    public void AddDbContext_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configurationRoot = null;
        const string serviceName = null;

        var ex = Assert.Throws<ArgumentNullException>(() => SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message);
    }

    [Fact]
    public void AddDbContext_NoVCAPs_AddsDbContext()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, configurationRoot);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetService<GoodSqlServerDbContext>();
        var serviceHealth = serviceProvider.GetService<IHealthContributor>();
        Assert.NotNull(service);
        Assert.NotNull(serviceHealth);
        Assert.IsAssignableFrom<RelationalDbHealthContributor>(serviceHealth);
    }

    [Fact]
    public void AddDbContext_WithServiceName_NoVCAPs_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        var ex = Assert.Throws<ConnectorException>(() => SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, configurationRoot, "foobar"));
        Assert.Contains("foobar", ex.Message);
    }

    [Fact]
    public void AddDbContext_MultipleSqlServerServices_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", Steeltoe.TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.TwoServerVcap);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var ex = Assert.Throws<ConnectorException>(() => SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, configurationRoot));
        Assert.Contains("Multiple", ex.Message);
    }

    [Fact]
    public void AddDbContexts_WithVCAPs_AddsDbContexts()
    {
        IServiceCollection services = new ServiceCollection();
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", Steeltoe.TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", SqlServerTestHelpers.SingleServerVcap);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        SqlServerDbContextServiceCollectionExtensions.AddDbContext<GoodSqlServerDbContext>(services, configurationRoot);
        SqlServerDbContextServiceCollectionExtensions.AddDbContext<Good2SqlServerDbContext>(services, configurationRoot);

        ServiceProvider built = services.BuildServiceProvider();
        var service = built.GetService<GoodSqlServerDbContext>();
        Assert.NotNull(service);

        var service2 = built.GetService<Good2SqlServerDbContext>();
        Assert.NotNull(service2);
    }
}
