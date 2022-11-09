// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Steeltoe.Common.HealthChecks;
using Steeltoe.Configuration.CloudFoundry;
using Steeltoe.Connector.Oracle;
using Xunit;

namespace Steeltoe.Connector.Test.Oracle;

public class OracleProviderServiceCollectionExtensionsTest
{
    [Fact]
    public void AddOracleConnection_ThrowsIfServiceCollectionNull()
    {
        const IServiceCollection services = null;
        const IConfigurationRoot configurationRoot = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddOracleConnection(configurationRoot));
        Assert.Contains(nameof(services), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddOracleConnection(configurationRoot, "foobar"));
        Assert.Contains(nameof(services), ex2.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddOracleConnection_ThrowsIfConfigurationNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configuration = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddOracleConnection(configuration));
        Assert.Contains(nameof(configuration), ex.Message, StringComparison.Ordinal);

        var ex2 = Assert.Throws<ArgumentNullException>(() => services.AddOracleConnection(configuration, "foobar"));
        Assert.Contains(nameof(configuration), ex2.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddOracleConnection_ThrowsIfServiceNameNull()
    {
        IServiceCollection services = new ServiceCollection();
        const IConfigurationRoot configurationRoot = null;
        const string serviceName = null;

        var ex = Assert.Throws<ArgumentNullException>(() => services.AddOracleConnection(configurationRoot, serviceName));
        Assert.Contains(nameof(serviceName), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddOracleConnection_AddsRelationalHealthContributor()
    {
        IServiceCollection services = new ServiceCollection();
        var builder = new ConfigurationBuilder();
        builder.AddCloudFoundry();
        IConfigurationRoot configurationRoot = builder.Build();

        services.AddOracleConnection(configurationRoot);
        var healthContributor = services.BuildServiceProvider().GetService<IHealthContributor>() as RelationalDbHealthContributor;

        Assert.NotNull(healthContributor);
    }
}
