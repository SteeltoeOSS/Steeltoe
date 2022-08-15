// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Connector.EntityFramework6;
using Xunit;

namespace Steeltoe.Connector.Oracle.EntityFramework6.Test;

public class OracleDbContextServiceCollectionExtensionsTest
{
    [Fact]
    public void AddDbContext_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddDbContext<GoodOracleDbContext>(config));
        Assert.Contains(nameof(services), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddDbContext<GoodOracleDbContext>(config, "foobar"));
        Assert.Contains(nameof(services), ex2.Message);
    }

    [Fact]
    public void AddDbContext_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot config = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddDbContext<GoodOracleDbContext>(config));
        Assert.Contains(nameof(config), ex.Message);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddDbContext<GoodOracleDbContext>(config, "foobar"));
        Assert.Contains(nameof(config), ex2.Message);
    }

    [Fact]
    public void AddDbContext_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot config = null;
        const string serviceName = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddDbContext<GoodOracleDbContext>(config, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message);
    }

    [Fact]
    public void AddDbContext_NoVCAPs_AddsDbContext()
    {
        IServiceCollection services = new ServiceCollection();
        IConfigurationRoot config = new ConfigurationBuilder().Build();

        services.AddDbContext<GoodOracleDbContext>(config);

        ServiceProvider serviceProvider = services.BuildServiceProvider();
        var service = serviceProvider.GetService<GoodOracleDbContext>();
        var serviceHealth = serviceProvider.GetService<IHealthContributor>();
        Assert.NotNull(service);
        Assert.Null(serviceHealth);
    }
}
