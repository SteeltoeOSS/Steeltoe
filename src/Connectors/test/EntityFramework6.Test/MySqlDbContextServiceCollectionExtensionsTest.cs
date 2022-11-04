// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connector.MySql.EntityFramework6;
using Xunit;

namespace Steeltoe.Connector.EntityFramework6.Test;

public class MySqlDbContextServiceCollectionExtensionsTest
{
    public MySqlDbContextServiceCollectionExtensionsTest()
    {
        Environment.SetEnvironmentVariable("VCAP_APPLICATION", null);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", null);
    }

    [Fact]
    public void AddDbContext_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot configurationRoot = null;

        var ex = Assert.Throws<ArgumentNullException>(() => MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodMySqlDbContext>(services, configurationRoot));
        Assert.Contains(nameof(services), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodMySqlDbContext>(services, configurationRoot, "foobar"));
        Assert.Contains(nameof(services), ex2.Message);
    }

    [Fact]
    public void AddDbContext_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodMySqlDbContext>(services, configuration));
        Assert.Contains(nameof(configuration), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodMySqlDbContext>(services, configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message);
    }

    [Fact]
    public void AddDbContext_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configurationRoot = null;
        const string serviceName = null;

        var ex = Assert.Throws<ArgumentNullException>(() => MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodMySqlDbContext>(services, configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message);
    }

    [Fact]
    public void AddDbContext_NoVCAPs_AddsDbContext()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot configurationRoot = new ConfigurationBuilder().Build();

        MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodMySqlDbContext>(services, configurationRoot);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetService<GoodMySqlDbContext>();
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

        var ex = Assert.Throws<ConnectorException>(() => MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodMySqlDbContext>(services, configurationRoot, "foobar"));
        Assert.Contains("foobar", ex.Message);
    }

    [Fact]
    public void AddDbContext_MultipleMySqlServices_ThrowsConnectorException()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", Steeltoe.TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.TwoServerVcap);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        var ex = Assert.Throws<ConnectorException>(() => MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodMySqlDbContext>(services, configurationRoot));
        Assert.Contains("Multiple", ex.Message);
    }

    [Fact]
    public void AddDbContexts_WithVCAPs_AddsDbContexts()
    {
        IServiceCollection services = new ServiceCollection();

        Environment.SetEnvironmentVariable("VCAP_APPLICATION", Steeltoe.TestHelpers.VcapApplication);
        Environment.SetEnvironmentVariable("VCAP_SERVICES", MySqlTestHelpers.SingleServerVcap);

        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        MySqlDbContextServiceCollectionExtensions.AddDbContext<GoodMySqlDbContext>(services, configurationRoot);
        MySqlDbContextServiceCollectionExtensions.AddDbContext<Good2MySqlDbContext>(services, configurationRoot);

        ServiceProvider built = services.BuildServiceProvider();
        var service = built.GetService<GoodMySqlDbContext>();
        Assert.NotNull(service);

        var service2 = built.GetService<Good2MySqlDbContext>();
        Assert.NotNull(service2);
    }
}
